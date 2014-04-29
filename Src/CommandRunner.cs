using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    ///     Provides features to execute cmd.exe commands in a hidden window and retrieve their output. This class is geared
    ///     heavily towards executing console programs, batch files and console scripts, but can also execute built-in
    ///     commands and non-console programs. Because the command goes through cmd.exe, the PATH, PATHEXT and file
    ///     associations are all automatically taken care of.</summary>
    /// <remarks>
    ///     <para>
    ///         When the command completes, whether by exiting naturally or by being aborted, the <see cref="State"/> changes
    ///         first. Then the <see cref="CommandEnded"/> event fires. Finally, the <see cref="EndedWaitHandle"/> is
    ///         signalled. By the time the <see cref="State"/> has changed to <see cref="CommandRunnerState.Exited"/>, it is
    ///         guaranteed that all the output has been processed and the <see cref="ExitCode"/> can be retrieved.</para>
    ///     <para>
    ///         This class is thread-safe: all public members may be used from any thread.</para></remarks>
    public class CommandRunner
    {
        private Process _process;
        private ProcessStartInfo _startInfo;
        private string _tempStdout, _tempStderr;
        private Stream _streamStdout, _streamStderr;
        private Decoder _utf8Stdout, _utf8Stderr;
        private Thread _thread;
        private ManualResetEventSlim _started = new ManualResetEventSlim();
        private ManualResetEventSlim _ended = new ManualResetEventSlim();
        private Timer _pauseTimer;
        private DateTime? _pauseTimerDue = null;
        private object _lock = new object();

        /// <summary>
        ///     Raised once the command ends (that is, exits naturally or is aborted) and all the clean-up has completed. See
        ///     Remarks.</summary>
        /// <remarks>
        ///     When this event occurs, the <see cref="CommandRunner"/> is guaranteed to be in either the <see
        ///     cref="CommandRunnerState.Exited"/> or <see cref="CommandRunnerState.Aborted"/> state. The <see
        ///     cref="EndedWaitHandle"/> will not have fired yet, and will only fire once this event handler is completed.</remarks>
        public event Action CommandEnded;
        /// <summary>
        ///     Raised whenever the command execution resumes after it was paused. This includes manual calls to <see
        ///     cref="ResumePaused"/>.</summary>
        public event Action CommandResumed;
        /// <summary>
        ///     Raised whenever the command has produced new output on stdout (but no more often than about once in 50ms).</summary>
        public event Action<byte[]> StdoutData;
        /// <summary>
        ///     Raised whenever the command has produced new output on stderr (but no more often than about once in 50ms).</summary>
        public event Action<byte[]> StderrData;
        /// <summary>
        ///     Raised whenever the command has produced new text on stdout. For ASCII outputs, this is identical to <see
        ///     cref="StdoutData"/>, however utf8-encoded text is guaranteed to correctly handle the possibility that part of
        ///     a character's encoding has not been output yet. Other encodings are not supported.</summary>
        public event Action<string> StdoutText;
        /// <summary>Same as <see cref="StdoutText"/> but for stderr.</summary>
        public event Action<string> StderrText;

        private bool _captureEntireStdout = false;
        private bool _captureEntireStderr = false;
        private byte[] _entireStdout;
        private byte[] _entireStderr;

        /// <summary>
        ///     Specifies whether the entire stdout output should be captured. This can consume large amounts of memory, and
        ///     so defaults to <c>false</c>.</summary>
        public bool CaptureEntireStdout
        {
            get { return _captureEntireStdout; }
            set
            {
                if (State != CommandRunnerState.NotStarted) // this condition is a bit too restrictive for the current implementation, but allows for removing the temporary files in future
                    throw new InvalidOperationException("This property cannot be changed once the runner has started.");
                _captureEntireStdout = value;
            }
        }

        /// <summary>
        ///     Specifies whether the entire stderr output should be captured. This can consume large amounts of memory, and
        ///     so defaults to <c>false</c>.</summary>
        public bool CaptureEntireStderr
        {
            get { return _captureEntireStderr; }
            set
            {
                if (State != CommandRunnerState.NotStarted)
                    throw new InvalidOperationException("This property cannot be changed once the runner has started.");
                _captureEntireStderr = value;
            }
        }

        /// <summary>
        ///     Gets the entire stdout output produced by the program. This property can only be accessed once the command has
        ///     ended (exited or aborted), and only if <see cref="CaptureEntireStdout"/> is <c>true</c>. You may modify the
        ///     returned array, but subsequent invocations will then return the modified array. The relative interleaving of
        ///     stdout and stderr is not preserved in this property.</summary>
        public byte[] EntireStdout
        {
            get
            {
                if (!CaptureEntireStdout)
                    throw new InvalidOperationException("This property can only be read if CaptureEntireStdout is true");
                if (State != CommandRunnerState.Exited && State != CommandRunnerState.Aborted)
                    throw new InvalidOperationException("This property can only be read once the command has exited or was aborted.");
                return _entireStdout;
            }
        }

        /// <summary>
        ///     Gets the entire stderr output produced by the program. This property can only be accessed once the command has
        ///     ended (exited or aborted), and only if <see cref="CaptureEntireStderr"/> is <c>true</c>. You may modify the
        ///     returned array, but subsequent invocations will then return the modified array. The relative interleaving of
        ///     stdout and stderr is not preserved in this property.</summary>
        public byte[] EntireStderr
        {
            get
            {
                if (!CaptureEntireStderr)
                    throw new InvalidOperationException("This property can only be read if CaptureEntireStderr is true");
                if (State != CommandRunnerState.Exited && State != CommandRunnerState.Aborted)
                    throw new InvalidOperationException("This property can only be read once the command has exited or was aborted.");
                return _entireStderr;
            }
        }

        /// <summary>
        ///     Exposes a waitable flag indicating whether the command has ended, either by exiting naturally or by being
        ///     aborted. See also Remarks on <see cref="CommandRunner"/>.</summary>
        /// <remarks>
        ///     Calling Set or Reset will result in corruption and undefined behaviour. This should be a read-only waitable
        ///     flag, but there is no built-in class</remarks>
        public WaitHandle EndedWaitHandle { get { return _ended.WaitHandle; } }

        /// <summary>
        ///     Indicates the current state of the runner. Some properties are only readable and/or writable in specific
        ///     states. Note that this property is not guaranteed to reflect whether the actual command is running or not, or
        ///     whether a call to <see cref="Abort"/> resulted in the command of being terminated; rather, this is the logical
        ///     state of the <see cref="CommandRunner"/> itself.</summary>
        public CommandRunnerState State { get; private set; }

        /// <summary>
        ///     Gets the exit code returned by the command. If the command has not started or exited yet, or has been aborted,
        ///     this method will throw an <see cref="InvalidOperationException"/>.</summary>
        public int ExitCode
        {
            get
            {
                if (State != CommandRunnerState.Exited)
                    throw new InvalidOperationException("This property can only be read if the command has exited and was not aborted.");
                return _exitCode;
            }
        }
        private int _exitCode;

        /// <summary>
        ///     The command to be executed, as a single string. Any command supported by cmd.exe is permitted. See also <see
        ///     cref="SetCommand(string[])"/>, which simplifies running commands with spaces and/or arguments. Once the
        ///     command has been started, this property becomes read-only and indicates the value in effect at the time of
        ///     starting.</summary>
        public string Command
        {
            get { return _command; }
            set { onlyBeforeStarted(); _command = value; }
        }
        private string _command = null;

        /// <summary>
        ///     The working directory to be used when starting the command. Once the command has been started, this property
        ///     becomes read-only and indicates the value in effect at the time of starting.</summary>
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { onlyBeforeStarted(); _workingDirectory = value; }
        }
        private string _workingDirectory = null;

        /// <summary>
        ///     Credentials and parameters used to run the command as a different user. Null to run as the same user as the
        ///     current process. Once the command has been started, this property becomes read-only and indicates the value in
        ///     effect at the time of starting.</summary>
        public RunAsUserParams RunAsUser
        {
            get { return _runAsUser; }
            set { onlyBeforeStarted(); _runAsUser = value; }
        }
        private RunAsUserParams _runAsUser = null;

        private void onlyBeforeStarted()
        {
            if (State != CommandRunnerState.NotStarted)
                throw new InvalidOperationException("This property cannot be modified after the command has been started.");
        }

        /// <summary>
        ///     Sets the <see cref="Command"/> property by concatenating the command and any arguments while escaping values
        ///     with spaces. Each value must be a single command / executable / script / argument. See Remarks.</summary>
        /// <remarks>
        ///     Example: <c>SetCommand(new[] { @"C:\Program Files\Foo\Foo.exe", "-f", @"C:\Some Path\file.txt" });</c></remarks>
        public void SetCommand(IEnumerable<string> args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (sb.Length != 0)
                    sb.Append(' ');
                sb.Append(arg.Contains(" ") ? "\"" + arg + "\"" : arg);
            }
            Command = sb.ToString();
        }

        /// <summary>
        ///     Sets the <see cref="Command"/> property by concatenating the command and any arguments while escaping values
        ///     with spaces. Each value must be a single command / executable / script / argument. See Remarks.</summary>
        /// <remarks>
        ///     Example: <c>SetCommand(@"C:\Program Files\Foo\Foo.exe", "-f", @"C:\Some Path\file.txt");</c></remarks>
        public void SetCommand(params string[] args)
        {
            SetCommand((IEnumerable<string>) args);
        }

        /// <summary>Starts the command with all the settings as configured.</summary>
        public void Start()
        {
            if (State != CommandRunnerState.NotStarted)
                throw new InvalidOperationException("This command has already been started, and cannot be started again.");
            State = CommandRunnerState.Started;

            _tempStdout = Path.GetTempFileName();
            _tempStderr = Path.GetTempFileName();

            _startInfo = new ProcessStartInfo();
            _startInfo.FileName = @"cmd.exe";
            _startInfo.Arguments = "/C " + Command + @" >{0} 2>{1}".Fmt(_tempStdout, _tempStderr);
            _startInfo.WorkingDirectory = WorkingDirectory;
            _startInfo.RedirectStandardInput = false;
            _startInfo.RedirectStandardOutput = false;
            _startInfo.RedirectStandardError = false;
            _startInfo.CreateNoWindow = true;
            _startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            _startInfo.UseShellExecute = false;

            if (RunAsUser != null)
            {
                _startInfo.LoadUserProfile = RunAsUser.LoadProfile;
                _startInfo.Domain = RunAsUser.Domain;
                _startInfo.UserName = RunAsUser.Username;
                _startInfo.Password = RunAsUser.Password;
            }

            _thread = new Thread(thread);
            _thread.Start();
        }

        private void thread()
        {
            // There's no indication that Process members are thread-safe, so use it on this thread exclusively.
            _process = new Process();
            _process.EnableRaisingEvents = true;
            _process.StartInfo = _startInfo;
            _process.Start();
            _utf8Stdout = Encoding.UTF8.GetDecoder();
            _utf8Stderr = Encoding.UTF8.GetDecoder();
            Thread.Sleep(50);
            _started.Set(); // the main purpose of _started is to make Pause reliable when executed immediately after Start.

            while (!_process.HasExited)
            {
                checkOutputs();
                Thread.Sleep(50);
            }
            checkOutputs();

            if (_captureEntireStdout)
                _entireStdout = File.ReadAllBytes(_tempStdout);
            if (_captureEntireStderr)
                _entireStderr = File.ReadAllBytes(_tempStderr);

            lock (_lock)
            {
                _pauseTimerDue = null;
                if (_pauseTimer != null)
                    _pauseTimer.Dispose();
            }
            _exitCode = _process.ExitCode;
            if (State != CommandRunnerState.Aborted)
                State = CommandRunnerState.Exited;

            if (_streamStdout != null)
                _streamStdout.Dispose();
            if (_streamStderr != null)
                _streamStderr.Dispose();
            Ut.OnExceptionRetryThenIgnore(() => { File.Delete(_tempStdout); }, delayMs: 1000);
            Ut.OnExceptionRetryThenIgnore(() => { File.Delete(_tempStderr); }, delayMs: 1000);
            _startInfo = null;
            _tempStdout = _tempStderr = null;
            _streamStdout = _streamStderr = null;
            _utf8Stdout = _utf8Stderr = null;
            _process = null;
            _thread = null;

            if (CommandEnded != null)
                CommandEnded();
            _ended.Set();
        }

        private void checkOutputs()
        {
            checkOutput(_tempStdout, ref _streamStdout, _utf8Stdout, StdoutData, StdoutText);
            checkOutput(_tempStderr, ref _streamStderr, _utf8Stderr, StderrData, StderrText);
        }

        private void checkOutput(string filename, ref Stream stream, Decoder utf8, Action<byte[]> dataEvent, Action<string> textEvent)
        {
            if (dataEvent == null && textEvent == null)
                return;

            if (stream == null)
            {
                try { stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                catch { }
            }

            if (stream != null)
            {
                var newBytes = stream.ReadAllBytes();
                if (newBytes.Length > 0)
                {
                    if (dataEvent != null)
                        dataEvent(newBytes);
                    if (textEvent != null)
                    {
                        var count = utf8.GetCharCount(newBytes, 0, newBytes.Length);
                        char[] buffer = new char[count];
                        int charsObtained = utf8.GetChars(newBytes, 0, newBytes.Length, buffer, 0);
                        if (charsObtained > 0)
                            textEvent(new string(buffer));
                    }
                }
            }
        }

        /// <summary>
        ///     Aborts the command by killing the process and all its children, if any. Throws if the command has not been
        ///     started yet. Does nothing if the command has already ended. See Remarks.</summary>
        /// <remarks>
        ///     It is theoretically possible that this method will fail to terminate a child, if called at exactly the wrong
        ///     time while a child process is being spawned. To avoid this possibility, pause the process tree first by
        ///     calling <see cref="Pause"/> (but see its remarks too).</remarks>
        public void Abort()
        {
            if (State == CommandRunnerState.NotStarted)
                throw new InvalidOperationException("Cannot abort the command because it has not been started yet.");
            if (State != CommandRunnerState.Started)
                return;
            State = CommandRunnerState.Aborted;
            _process.KillWithChildren();
        }

        /// <summary>
        ///     Gets the time at which the command will wake up again, <c>DateTime.MaxValue</c> if the command is paused
        ///     indefinitely, or null if the command is not paused.</summary>
        public DateTime? PausedUntil { get { return _pauseTimerDue; } }

        /// <summary>
        ///     Pauses the command for the specified duration by suspending every thread in every process in the process tree.
        ///     Throws if the command hasn't been started or has been aborted. Does nothing if the command has exited. If
        ///     called when already paused, will make sure the command is paused for at least the specified duration, but will
        ///     not shorten the resume timer. See Remarks.</summary>
        /// <param name="duration">
        ///     Pause duration. Zero and negative intervals do not pause the command at all. Use <c>TimeSpan.MaxValue</c> or
        ///     <c>null</c> for an indefinite pause.</param>
        /// <remarks>
        ///     It is theoretically possible for this to fail to suspend all threads in all processes, if a new thread is
        ///     created while the threads are being suspended. The current code does not address this scenario, but this can
        ///     be fixed, if necessary.</remarks>
        public void Pause(TimeSpan? duration = null)
        {
            if (State == CommandRunnerState.Exited)
                return;
            if (State == CommandRunnerState.NotStarted || State == CommandRunnerState.Aborted)
                throw new InvalidOperationException("Cannot pause a command that has not been started yet or has been aborted.");
            var durationNN = duration ?? TimeSpan.MaxValue; // because MaxValue is not a legal default
            if (durationNN <= TimeSpan.Zero)
                return;

            lock (_lock)
            {
                bool wasPaused = _pauseTimerDue != null;
                if (durationNN == TimeSpan.MaxValue)
                {
                    _pauseTimerDue = DateTime.MaxValue;
                    if (_pauseTimer != null)
                    {
                        _pauseTimer.Dispose();
                        _pauseTimer = null;
                    }
                }
                else
                {
                    var newDue = DateTime.UtcNow + durationNN;
                    if (_pauseTimerDue == null || newDue > _pauseTimerDue)
                        _pauseTimerDue = newDue;
                    if (_pauseTimer == null)
                        _pauseTimer = new Timer(resume, null, durationNN, TimeSpan.FromMilliseconds(-1));
                    // else the timer will fire and, if necessary, reschedule itself to fire again later.
                }
                if (!wasPaused)
                    pause();
            }
        }

        private void pause()
        {
            _started.Wait(); // ensure that the process is not yet in the start-up phase, immediately after the call to Start().
            foreach (var childProcess in _process.ChildProcessIds(true).Select(pid => Process.GetProcessById(pid)).Concat(_process))
            {
                foreach (ProcessThread thr in childProcess.Threads)
                {
                    IntPtr pThread = WinAPI.OpenThread(WinAPI.ThreadAccess.SUSPEND_RESUME, false, (uint) thr.Id);
                    if (pThread == IntPtr.Zero)
                        continue;
                    WinAPI.SuspendThread(pThread);
                    WinAPI.CloseHandle(pThread);
                }
            }
        }

        /// <summary>
        ///     Resumes the command after it has been paused. If the command was paused with a timeout, the timer is cleared.
        ///     Throws if the command has not been started yet or has been aborted. Does nothing if the command is not paused.</summary>
        public void ResumePaused()
        {
            if (State == CommandRunnerState.NotStarted || State == CommandRunnerState.Aborted)
                throw new InvalidOperationException("Cannot pause a command that has not been started yet or has been aborted.");
            resume();
        }

        private void resume(object _ = null)
        {
            lock (_lock)
            {
                if (_pauseTimerDue == null || _process == null || State != CommandRunnerState.Started)
                    return;

                // The due time may have changed; wait more if necessary
                var now = DateTime.UtcNow;
                if (now < _pauseTimerDue)
                {
                    _pauseTimer.Change(_pauseTimerDue.Value - now, TimeSpan.FromMilliseconds(-1));
                    return;
                }

                _pauseTimerDue = null;
                _pauseTimer.Dispose();
                _pauseTimer = null;

                foreach (var childProcess in _process.ChildProcessIds(true).Select(pid => Process.GetProcessById(pid)).Concat(_process))
                {
                    foreach (ProcessThread thr in childProcess.Threads)
                    {
                        IntPtr pThread = WinAPI.OpenThread(WinAPI.ThreadAccess.SUSPEND_RESUME, false, (uint) thr.Id);
                        if (pThread == IntPtr.Zero)
                            continue;
                        WinAPI.ResumeThread(pThread);
                        WinAPI.CloseHandle(pThread);
                    }
                }
            }

            if (CommandResumed != null)
                CommandResumed();
        }
    }

    /// <summary>Represents one of the possible states a <see cref="CommandRunner"/> can have.</summary>
    public enum CommandRunnerState
    {
        /// <summary>The command has not been started yet.</summary>
        NotStarted,
        /// <summary>
        ///     The command has started and has neither been aborted yet nor has exited on its own. This state does not
        ///     guarantee that the process in which the command is executed has already started, nor that it has not yet
        ///     terminated.</summary>
        Started,
        /// <summary>The command has exited on its own, without the runner terminating it.</summary>
        Exited,
        /// <summary>
        ///     The command has been terminated by the runner. This state does not guarantee that the process in which the
        ///     command is executed has already been killed.</summary>
        Aborted,
    }

    /// <summary>Specifies credentials and settings required to run a process as a different user.</summary>
    public class RunAsUserParams
    {
        /// <summary>Gets the name of the user account under which to run the process.</summary>
        public string Username { get; private set; }
        /// <summary>Gets the password for the <see cref="Username"/> account.</summary>
        public SecureString Password { get; private set; }
        /// <summary>Gets a value indicating whether the user profile is to be loaded, which can be time-consuming.</summary>
        public bool LoadProfile { get; private set; }
        /// <summary>Specifies the ActiveDirectory domain of the required user account.</summary>
        public string Domain { get; private set; }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="username">
        ///     The name of the user account under which to run the process.</param>
        /// <param name="password">
        ///     The password for the <paramref name="username"/> account.</param>
        /// <param name="loadProfile">
        ///     Specifies whether the target process will access the HKCU registry area. Loading the user profile can be
        ///     time-consuming, so it is optional.</param>
        /// <param name="domain">
        ///     ActiveDirectory domain of the required user account.</param>
        public RunAsUserParams(string username, SecureString password, bool loadProfile = false, string domain = null)
        {
            if (username == null)
                throw new ArgumentNullException("username");
            Username = username;
            Password = password;
            LoadProfile = loadProfile;
            Domain = domain;
        }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="username">
        ///     The name of the user account under which to run the process.</param>
        /// <param name="password">
        ///     The password for the <paramref name="username"/> account.</param>
        /// <param name="loadProfile">
        ///     Specifies whether the target process will access the HKCU registry area. Loading the user profile can be
        ///     time-consuming, so it is optional.</param>
        /// <param name="domain">
        ///     ActiveDirectory domain of the required user account.</param>
        public RunAsUserParams(string username, string password, bool loadProfile = false, string domain = null)
        {
            if (username == null)
                throw new ArgumentNullException("username");
            Username = username;
            Password = new SecureString();
            foreach (var c in password)
                Password.AppendChar(c);
            Password.MakeReadOnly();
            LoadProfile = loadProfile;
            Domain = domain;
        }
    }
}
