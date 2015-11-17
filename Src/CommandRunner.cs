using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using RT.Util.Consoles;
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
        private reader _stdoutReader, _stderrReader;
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
        ///     starting. See Remarks.</summary>
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
        ///     Overrides for the environment variables to be set for the command process. These overrides are in addition to
        ///     the variables inherited from the current process.</summary>
        public IDictionary<string, string> EnvironmentVariables
        {
            get { return _envVars; }
        }
        private Dictionary<string, string> _envVars = new Dictionary<string, string>();

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
        ///     with spaces. Each value must be a single command / executable / script / argument. Null values are allowed and
        ///     are skipped as if they weren't present. See Remarks.</summary>
        /// <remarks>
        ///     Example: <c>SetCommand(new[] { @"C:\Program Files\Foo\Foo.exe", "-f", @"C:\Some Path\file.txt" });</c></remarks>
        public void SetCommand(IEnumerable<string> args)
        {
            Command = ArgsToCommandLine(args);
        }

        /// <summary>
        ///     Sets the <see cref="Command"/> property by concatenating the command and any arguments while escaping values
        ///     with spaces. Each value must be a single command / executable / script / argument. Null values are allowed and
        ///     are skipped as if they weren't present. See Remarks.</summary>
        /// <remarks>
        ///     Example: <c>SetCommand(new[] { @"C:\Program Files\Foo\Foo.exe", "-f", @"C:\Some Path\file.txt" });</c></remarks>
        public void SetCommand(params string[] args)
        {
            Command = ArgsToCommandLine(args);
        }

        /// <summary>
        ///     Starts the command with all the settings as configured.</summary>
        /// <param name="stdin">
        ///     Provides a byte stream to be passed to the process’s standard input.</param>
        public void Start(byte[] stdin = null)
        {
            if (State != CommandRunnerState.NotStarted)
                throw new InvalidOperationException("This command has already been started, and cannot be started again.");
            State = CommandRunnerState.Started;

            _startInfo = new ProcessStartInfo();
            _startInfo.FileName = @"cmd.exe";
            _startInfo.Arguments = "/C " + EscapeCmdExeMetachars(Command);
            _startInfo.WorkingDirectory = WorkingDirectory;
            foreach (var kvp in EnvironmentVariables)
                _startInfo.EnvironmentVariables.Add(kvp.Key, kvp.Value);
            _startInfo.RedirectStandardInput = stdin != null;
            _startInfo.RedirectStandardOutput = true;
            _startInfo.RedirectStandardError = true;
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

            _stdoutReader = new reader(StdoutData, StdoutText, CaptureEntireStdout);
            _stderrReader = new reader(StderrData, StderrText, CaptureEntireStderr);

            _process = new Process();
            _process.EnableRaisingEvents = true;
            _process.StartInfo = _startInfo;
            _process.Exited += processExited;
            _process.Start();
            if (stdin != null)
            {
                _process.StandardInput.BaseStream.Write(stdin);
                _process.StandardInput.BaseStream.Close();
            }
            Thread.Sleep(50);
            _started.Set(); // the main purpose of _started is to make Pause reliable when executed immediately after Start.

            _stdoutReader.ReadBegin(_process.StandardOutput.BaseStream);
            _stderrReader.ReadBegin(_process.StandardError.BaseStream);
        }

        private void processExited(object sender, EventArgs e)
        {
            Ut.Assert(_process.HasExited);
            while (!_stdoutReader.Ended || !_stderrReader.Ended)
                Thread.Sleep(20);
            Ut.Assert(_stdoutReader.Ended);
            Ut.Assert(_stderrReader.Ended);
            if (_captureEntireStdout)
                _entireStdout = _stdoutReader.GetEntireOutput();
            if (_captureEntireStderr)
                _entireStderr = _stderrReader.GetEntireOutput();

            lock (_lock)
            {
                _pauseTimerDue = null;
                if (_pauseTimer != null)
                    _pauseTimer.Dispose();
            }
            _exitCode = _process.ExitCode;
            if (State != CommandRunnerState.Aborted)
                State = CommandRunnerState.Exited;

            _startInfo = null;
            _process.Exited -= processExited;
            _process = null;

            if (CommandEnded != null)
                CommandEnded();
            _ended.Set();
        }

        /// <summary>Starts the command with all the settings as configured. Does not return until the command exits.</summary>
        public void StartAndWait()
        {
            Start();
            EndedWaitHandle.WaitOne();
        }

        /// <summary>
        ///     Given a number of argument strings, constructs a single command line string with all the arguments escaped
        ///     correctly so that a process using standard Windows API for parsing the command line will receive exactly the
        ///     strings passed in here. See Remarks.</summary>
        /// <remarks>
        ///     The string is only valid for passing directly to a process. If the target process is invoked by passing the
        ///     process name + arguments to cmd.exe then further escaping is required, to counteract cmd.exe's interpretation
        ///     of additional special characters. See <see cref="EscapeCmdExeMetachars"/>.</remarks>
        public static string ArgsToCommandLine(IEnumerable<string> args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                if (arg == null)
                    continue;
                if (sb.Length != 0)
                    sb.Append(' ');
                // For details, see http://blogs.msdn.com/b/twistylittlepassagesallalike/archive/2011/04/23/everyone-quotes-arguments-the-wrong-way.aspx
                if (arg.Length != 0 && arg.IndexOfAny(_cmdChars) < 0)
                    sb.Append(arg);
                else
                {
                    sb.Append('"');
                    for (int c = 0; c < arg.Length; c++)
                    {
                        int backslashes = 0;
                        while (c < arg.Length && arg[c] == '\\')
                        {
                            c++;
                            backslashes++;
                        }
                        if (c == arg.Length)
                        {
                            sb.Append('\\', backslashes * 2);
                            break;
                        }
                        else if (arg[c] == '"')
                        {
                            sb.Append('\\', backslashes * 2 + 1);
                            sb.Append('"');
                        }
                        else
                        {
                            sb.Append('\\', backslashes);
                            sb.Append(arg[c]);
                        }
                    }
                    sb.Append('"');
                }
            }
            return sb.ToString();
        }
        private static readonly char[] _cmdChars = new[] { ' ', '"', '\n', '\t', '\v' };

        /// <summary>
        ///     Escapes all cmd.exe meta-characters by prefixing them with a ^. See <see cref="ArgsToCommandLine"/> for more
        ///     information.</summary>
        public static string EscapeCmdExeMetachars(string command)
        {
            var result = new StringBuilder();
            foreach (var ch in command)
            {
                switch (ch)
                {
                    case '(':
                    case ')':
                    case '%':
                    case '!':
                    case '^':
                    case '"':
                    case '<':
                    case '>':
                    case '&':
                    case '|':
                        result.Append('^');
                        break;
                }
                result.Append(ch);
            }
            return result.ToString();
        }

        private class reader
        {
            public bool Ended { get; private set; }

            private List<chunk> _chunks = new List<chunk>();
            private Decoder _decoder = Encoding.UTF8.GetDecoder();
            private Action<byte[]> _dataEvent;
            private Action<string> _textEvent;
            private bool _captureEntire;

            private class chunk
            {
                public byte[] Data = new byte[32768];
                public int Length = 0;
            }

            public reader(Action<byte[]> dataEvent, Action<string> textEvent, bool captureEntire)
            {
                _dataEvent = dataEvent;
                _textEvent = textEvent;
                _captureEntire = captureEntire;
            }

            public void ReadBegin(Stream stream)
            {
                if (_chunks.Count == 0)
                    _chunks.Add(new chunk());
                var chunk = _chunks[_chunks.Count - 1];
                if (chunk.Data.Length - chunk.Length < 1000)
                {
                    if (_captureEntire)
                    {
                        chunk = new chunk();
                        _chunks.Add(chunk);
                    }
                    else
                        chunk.Length = 0;
                }
                stream.BeginRead(chunk.Data, chunk.Length, chunk.Data.Length - chunk.Length, result => { readComplete(result, stream); }, null);
            }

            private void readComplete(IAsyncResult result, Stream stream)
            {
                int bytesRead = stream.EndRead(result);
                var chunk = _chunks[_chunks.Count - 1];
                chunk.Length += bytesRead;
                if (_dataEvent != null)
                {
                    var newBytes = new byte[bytesRead];
                    Array.Copy(chunk.Data, chunk.Length - bytesRead, newBytes, 0, bytesRead);
                    _dataEvent(newBytes);
                }
                if (_textEvent != null)
                {
                    var newChars = new char[bytesRead + 1]; // can have at most one char buffered up in the decoder, plus bytesRead new chars
                    int bytesUsed, charsUsed;
                    bool completed;
                    _decoder.Convert(chunk.Data, chunk.Length - bytesRead, bytesRead, newChars, 0, newChars.Length, false, out bytesUsed, out charsUsed, out completed);
                    Ut.Assert(completed); // it could still be halfway through a character; what this means is that newChars was large enough to accommodate everything
                    _textEvent(new string(newChars, 0, charsUsed));
                }
                if (bytesRead > 0) // continue reading
                    ReadBegin(stream);
                else
                {
                    Ended = true;
                    if (_textEvent != null)
                    {
                        var newChars = new char[1];
                        int bytesUsed, charsUsed;
                        bool completed;
                        _decoder.Convert(new byte[0], 0, 0, newChars, 0, newChars.Length, true, out bytesUsed, out charsUsed, out completed);
                        Ut.Assert(completed);
                        if (charsUsed > 0)
                            _textEvent(new string(newChars, 0, charsUsed));
                    }
                }
            }

            public byte[] GetEntireOutput()
            {
                Ut.Assert(_captureEntire);
                var result = new byte[_chunks.Sum(ch => ch.Length)];
                int offset = 0;
                foreach (var chunk in _chunks)
                {
                    Buffer.BlockCopy(chunk.Data, 0, result, offset, chunk.Length);
                    offset += chunk.Length;
                }
                return result;
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
            _pauseTimerDue = DateTime.UtcNow;
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

        /// <summary>
        ///     Executes the specified command using a fluid syntax. Use method chaining to configure any options, then invoke
        ///     <see cref="FluidCommandRunner.Go"/> (or one of its variants) to execute the command. See Remarks.</summary>
        /// <param name="args">
        ///     The command and its arguments, if any. Each value is automatically escaped as needed. See <see
        ///     cref="SetCommand(string[])"/> for further information. See <see cref="RunRaw"/> for an alternative way to
        ///     specify the command.</param>
        /// <remarks>
        ///     <para>
        ///         The default options are as follows: 0 is the only exit code indicating success; print the command output
        ///         to the console as-is.</para>
        ///     <para>
        ///         Example: <c>CommandRunner.Run(@"C:\Program Files\Foo\Foo.exe", "-f", @"C:\Some
        ///         Path\file.txt").SuccessExitCodes(0, 1).OutputNothing().Go();</c></para></remarks>
        public static FluidCommandRunner Run(params string[] args)
        {
            return new FluidCommandRunner(args);
        }

        /// <summary>
        ///     Executes the specified command using a fluid syntax. Use method chaining to configure any options, then invoke
        ///     <see cref="FluidCommandRunner.Go"/> (or one of its variants) to execute the command. See Remarks.</summary>
        /// <param name="command">
        ///     The command and its arguments. All arguments must be escaped as appropriate.</param>
        /// <remarks>
        ///     <para>
        ///         The default options are as follows: 0 is the only exit code indicating success; print the command output
        ///         to the console as-is.</para>
        ///     <para>
        ///         Example: <c>CommandRunner.RunRaw(@"C:\Program Files\Foo\Foo.exe -f ""C:\Some
        ///         Path\file.txt""").SuccessExitCodes(0, 1).Go();</c></para></remarks>
        public static FluidCommandRunner RunRaw(string command)
        {
            return new FluidCommandRunner(command);
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

    /// <summary>Implements method chaining for <see cref="CommandRunner.Run"/>.</summary>
    public class FluidCommandRunner
    {
        private CommandRunner _runner = new CommandRunner();
        private HashSet<int> _successExitCodes;
        private HashSet<int> _failExitCodes;
        private bool _printCommandOutput = true;
        private bool _printAugmented = false;
        private bool _printInvokeCount = false;
        private static int _invokeCount = 0;

        internal FluidCommandRunner(string[] args)
        {
            _runner.SetCommand(args);
        }

        internal FluidCommandRunner(string command)
        {
            _runner.Command = command;
        }

        /// <summary>Specifies which exit codes represent a successful invocation. All other codes are interpreted as failure.</summary>
        public FluidCommandRunner SuccessExitCodes(params int[] exitCodes)
        {
            if (_failExitCodes != null)
                throw new InvalidOperationException("Cannot use both SuccessExitCodes and FailExitCodes for the same command invocation.");
            if (_successExitCodes == null)
                _successExitCodes = new HashSet<int>();
            _successExitCodes.AddRange(exitCodes);
            return this;
        }

        /// <summary>
        ///     Specifies which exit codes represent a failed invocation. All other codes are interpreted as success. Mutually
        ///     exclusive with <see cref="SuccessExitCodes"/>.</summary>
        public FluidCommandRunner FailExitCodes(params int[] exitCodes)
        {
            if (_successExitCodes != null)
                throw new InvalidOperationException("Cannot use both SuccessExitCodes and FailExitCodes for the same command invocation.");
            if (_failExitCodes == null)
                _failExitCodes = new HashSet<int>();
            _failExitCodes.AddRange(exitCodes);
            return this;
        }

        private bool isSuccess(int code)
        {
            if (_failExitCodes != null)
                return !_failExitCodes.Contains(code);
            if (_successExitCodes != null)
                return _successExitCodes.Contains(code);
            return code == 0;
        }

        /// <summary>
        ///     Configures the runner to suppress all output. This invocation will print nothing to the console. If
        ///     unspecified, the command's stdout output is relayed to the console as-is, while stderr is relayed in red.</summary>
        public FluidCommandRunner OutputNothing()
        {
            _printCommandOutput = false;
            return this;
        }

        /// <summary>
        ///     Configures the runner to relay the command's output to the console, prefixing every line with a timestamp. The
        ///     entire command is printed before running it. When the command completes, its success/failure status is
        ///     printed, along with its run time and exit code.</summary>
        /// <param name="invokeCount">
        ///     If true, the prefix also includes a value that increments for every invocation, starting at 1.</param>
        public FluidCommandRunner OutputAugmented(bool invokeCount = false)
        {
            _printCommandOutput = true;
            _printAugmented = true;
            _printInvokeCount = invokeCount;
            return this;
        }

        /// <summary>
        ///     Invokes the command, blocking until the command finishes. If the command fails, throws a <see
        ///     cref="CommandRunnerFailedException"/>. See also <see cref="GoGetExitCode"/>.</summary>
        [DebuggerHidden]
        public void Go()
        {
            int result = GoGetExitCode();
            if (!isSuccess(result))
                throw new CommandRunnerFailedException(result);
        }

        /// <summary>
        ///     Invokes the command, blocking until the command finishes. On success, returns the raw output of the command.
        ///     If the command fails, throws a <see cref="CommandRunnerFailedException"/>. See Remarks.</summary>
        /// <remarks>
        ///     Output options such as <see cref="OutputAugmented"/> do not affect the data returned; they influence only how
        ///     the output is relayed to the console. This method ignores all stderr output.</remarks>
        [DebuggerHidden]
        public byte[] GoGetOutput()
        {
            _runner.CaptureEntireStdout = true;
            int result = GoGetExitCode();
            if (!isSuccess(result))
                throw new CommandRunnerFailedException(result);
            return _runner.EntireStdout;
        }

        /// <summary>
        ///     Invokes the command, blocking until the command finishes. On success, returns the raw output of the command,
        ///     interpreted as text in UTF-8. If the command fails, throws a <see cref="CommandRunnerFailedException"/>. See
        ///     Remarks.</summary>
        /// <remarks>
        ///     Output options such as <see cref="OutputAugmented"/> do not affect the data returned; they influence only how
        ///     the output is relayed to the console. This method ignores all stderr output.</remarks>
        [DebuggerHidden]
        public string GoGetOutputText()
        {
            return GoGetOutput().FromUtf8();
        }

        /// <summary>
        ///     Invokes the command, blocking until the command finishes. Returns the command's exit code. Does not throw if
        ///     the command failed.</summary>
        public int GoGetExitCode()
        {
            var invokeCount = Interlocked.Increment(ref _invokeCount) + 1;
            var startTime = DateTime.UtcNow;
            if (_printCommandOutput && _printAugmented)
            {
                var prefix = (_printInvokeCount ? "    Cmd {0} at {1:HH:mm}> " : "    {1:HH:mm}> ").Color(ConsoleColor.White);
                ConsoleUtil.WriteLine("Running command: ".Color(ConsoleColor.Yellow) + _runner.Command.Color(ConsoleColor.Cyan));
                ConsoleUtil.Write(ConsoleColoredString.Format(prefix, invokeCount, DateTime.UtcNow));
                _runner.StdoutText += txt => { ConsoleUtil.Write(ConsoleColoredString.Format(txt.Color(ConsoleColor.Gray).Replace("\n", "\n" + prefix), invokeCount, DateTime.UtcNow)); };
                _runner.StderrText += txt => { ConsoleUtil.Write(ConsoleColoredString.Format(txt.Color(ConsoleColor.Red).Replace("\n", "\n" + prefix), invokeCount, DateTime.UtcNow)); };
            }
            else if (_printCommandOutput)
            {
                _runner.StdoutText += txt => { Console.Write(txt); };
                _runner.StderrText += txt => { ConsoleUtil.Write(txt.Color(ConsoleColor.Red)); };
            }
            _runner.Start();
            _runner.EndedWaitHandle.WaitOne();
            if (_printCommandOutput && _printAugmented)
            {
                Console.WriteLine();
                var ranFor = "Ran for {0:#,0} seconds".Fmt((DateTime.UtcNow - startTime).TotalSeconds);
                if (isSuccess(_runner.ExitCode))
                    ConsoleUtil.WriteLine("Command succeeded. {0}\r\n\r\n".Fmt(ranFor).Color(ConsoleColor.Green));
                else
                    ConsoleUtil.WriteLine("Command failed with error code {0}. {1}\r\n\r\n".Fmt(_runner.ExitCode, ranFor).Color(ConsoleColor.Red));
            }
            return _runner.ExitCode;
        }
    }

    /// <summary>Indicates that a command returned an exit code indicating a failure.</summary>
    public class CommandRunnerFailedException : Exception
    {
        /// <summary>The exit code returned.</summary>
        public int ExitCode { get; private set; }
        /// <summary>Constructor.</summary>
        public CommandRunnerFailedException(int exitCode)
            : base("Command failed with exit code {0}".Fmt(exitCode))
        {
            ExitCode = exitCode;
        }
    }
}
