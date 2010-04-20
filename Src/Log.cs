using System;
using System.Collections.Generic;
using System.IO;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>Specifies the type of a log message logged using a subclass of <see cref="LoggerBase"/>.</summary>
    public enum LogType
    {
        /// <summary>Specifies an informational log message.</summary>
        Info,
        /// <summary>Specifies a warning message.</summary>
        Warning,
        /// <summary>Specifies an error message.</summary>
        Error,
        /// <summary>Specifies a debug message.</summary>
        Debug
    }

    /// <summary>
    /// Abstract base class for all loggers. Implements some common functionality.
    /// </summary>
    [Serializable]
    public abstract class LoggerBase
    {
        /// <summary>
        /// Holds the current verbosity limit for each of the log types. Only messages
        /// with same or lower verbosity will be printed. Defaults to level 1 for
        /// all messages except debug, which defaults to 0.
        /// 
        /// NOTE: applications <i>can</i> print a message with a verbosity level of 0 -
        /// such messages cannot be disabled.
        /// </summary>
        public Dictionary<LogType, uint> VerbosityLimit = new Dictionary<LogType, uint>();

        /// <summary>
        /// Holds a format string for printing the date/time at which a log entry
        /// was added. Defaults to a full ISO-formatted date/time string with
        /// milliseconds.
        /// </summary>
        public string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Holds a format string for the log information message. This will receive the
        /// following arguments: {0}: timestamp, {1}: message type string, {2}: message
        /// verbosity level. The actual message will be printed immediately after the
        /// log information message.
        /// </summary>
        public string MessageFormat = "{0} | {2}/{1,-5} | ";

        /// <summary>
        /// When printing multi-line (e.g. wrapped) messages, the indent text will consist
        /// of a number of spaces and end with this suffix.
        /// </summary>
        public string IndentFormatSuffix = "| ";

        /// <summary>
        /// If true, the timestamps will be printed in UTC. Otherwise in local time.
        /// Defaults to false.
        /// </summary>
        public bool TimestampInUTC = false;

        /// <summary>
        /// Used internally to make the Log operation atomic.
        /// </summary>
        protected object _lock_log = new object();

        /// <summary>
        /// Initialises some members to their default values.
        /// </summary>
        public LoggerBase()
        {
            // Default visibility levels
            VerbosityLimit[LogType.Info] = 1;
            VerbosityLimit[LogType.Warning] = 1;
            VerbosityLimit[LogType.Error] = 1;
            VerbosityLimit[LogType.Debug] = 0;
        }

        /// <summary>
        /// Specifies a short string describing each log type (INFO, WARN, ERROR, or DEBUG).
        /// </summary>
        public string GetMessageTypeString(LogType type)
        {
            switch (type)
            {
                case LogType.Info: return "INFO";
                case LogType.Warning: return "WARN";
                case LogType.Error: return "ERROR";
                case LogType.Debug: return "DEBUG";
            }
            return null;
        }

        /// <summary>
        /// Takes a string which encodes verbosity limit configuration, parses it
        /// and sets the limits accordingly. On failure throws an ArgumentException
        /// with a fairly detailed description of the string format.
        /// </summary>
        /// <remarks>
        /// <para>Examples of valid strings:</para>
        /// <list type="table">
        ///     <item><term>""</term><description>sets default values</description></item>
        ///     <item><term>"3"</term><description>sets all limits to 3</description></item>
        ///     <item><term>"2d0"</term><description>sets all limits to 2, then set the debug limit to 0</description></item>
        ///     <item><term>"i0w1e2d3"</term><description>sets info=0, warning=1, error=2, debug=3</description></item>
        /// </list>
        /// <para>Intended use: configuring the logger via a command-line option.</para>
        /// </remarks>
        public virtual void ConfigureVerbosity(string settings)
        {
            // 0 - set all types to this level
            // w1 - set warning log to this level
            // executed in the order in which they occur
            // e.g. 0w1e1 - disable all, enable warnings, enable errors
            string errorMessage = "Cannot parse log verbosity setting. Must consist of:\n  [0-9]  - set all types' verbosity\n  i[0-9] - set verbosity of info messages\n  w[0-9] - set verbosity of warning messages\n  e[0-9] - set verbosity of error messages\n  d[0-9] - set verbosity of debug messages\n\nE.g. 2d0i1 consists of 2, d0, i1";

            int pos = 0;
            while (pos < settings.Length)
            {
                if (char.IsDigit(settings[pos]))
                {
                    uint lvl = uint.Parse(settings[pos].ToString());
                    VerbosityLimit[LogType.Info] = lvl;
                    VerbosityLimit[LogType.Warning] = lvl;
                    VerbosityLimit[LogType.Error] = lvl;
                    VerbosityLimit[LogType.Debug] = lvl;
                    pos++;
                }
                else if (char.IsLetter(settings[pos]) && pos + 1 < settings.Length && char.IsDigit(settings[pos + 1]))
                {
                    uint lvl = uint.Parse(settings[pos + 1].ToString());
                    switch (settings[pos])
                    {
                        case 'i': VerbosityLimit[LogType.Info] = lvl; break;
                        case 'w': VerbosityLimit[LogType.Warning] = lvl; break;
                        case 'e': VerbosityLimit[LogType.Error] = lvl; break;
                        case 'd': VerbosityLimit[LogType.Debug] = lvl; break;
                        default:
                            throw new ArgumentException(errorMessage);
                    }
                    pos += 2;
                }
                else
                    throw new ArgumentException(errorMessage);
            }
        }

        /// <summary>
        /// Helps prepare a log message to the derived classes. Takes the parameters
        /// supplied by a call to one of the Log methods and generates two strings:
        /// the <paramref name="fmtInfo"/> which is the message header and the
        /// <paramref name="indent"/> which is the indent text.
        /// </summary>
        protected virtual void GetFormattedStrings(out string fmtInfo, out string indent, uint verbosity, LogType type)
        {
            string timestamp = (TimestampInUTC ? DateTime.Now.ToUniversalTime() : DateTime.Now).ToString(TimestampFormat);
            fmtInfo = string.Format(MessageFormat, timestamp, GetMessageTypeString(type), verbosity);
            indent = new string(' ', fmtInfo.Length - IndentFormatSuffix.Length) + IndentFormatSuffix;
        }

        /// <summary>
        /// Appends an entry to the log. Derived classes implement this to put the
        /// log data where necessary.
        /// </summary>
        /// <remarks>
        /// Note that the various specialised functions such as <see cref="Warn(string)"/> simply
        /// call this method to do the work.
        /// </remarks>
        /// <param name="verbosity">Verbosity level of this message.</param>
        /// <param name="type">Message type (info, warning, error or debug).</param>
        /// <param name="message">The message itself.</param>
        /// <seealso cref="GetFormattedStrings"/>
        public abstract void Log(uint verbosity, LogType type, string message);

        /// <summary>Creates a visual separation in the log, for example if a new section starts.</summary>
        public abstract void Separator();

        /// <summary>
        /// Returns true if a message of the specified verbosity and type will actually
        /// end up being logged.
        /// </summary>
        public virtual bool IsLogOn(uint verbosity, LogType type)
        {
            return VerbosityLimit[type] >= verbosity;
        }

        /// <summary>Appends an informational message to the log.</summary>
        public void Info(string message) { Log(1, LogType.Info, message); }
        /// <summary>Appends an informational message to the log.</summary>
        public void Info(uint verbosity, string message) { Log(verbosity, LogType.Info, message); }
        /// <summary>Appends a warning message to the log.</summary>
        public void Warn(string message) { Log(1, LogType.Warning, message); }
        /// <summary>Appends a warning message to the log.</summary>
        public void Warn(uint verbosity, string message) { Log(verbosity, LogType.Warning, message); }
        /// <summary>Appends an error message to the log.</summary>
        public void Error(string message) { Log(1, LogType.Error, message); }
        /// <summary>Appends an error message to the log.</summary>
        public void Error(uint verbosity, string message) { Log(verbosity, LogType.Error, message); }
        /// <summary>Appends a debug message to the log.</summary>
        public void Debug(string message) { Log(1, LogType.Debug, message); }
        /// <summary>Appends a debug message to the log.</summary>
        public void Debug(uint verbosity, string message) { Log(verbosity, LogType.Debug, message); }

        /// <summary>Determines whether an informational message would be visible (at default verbosity).</summary>
        public bool IsInfoOn() { return IsLogOn(1, LogType.Info); }
        /// <summary>Determines whether an informational message would be visible at specified verbosity.</summary>
        public bool IsInfoOn(uint verbosity) { return IsLogOn(verbosity, LogType.Info); }
        /// <summary>Determines whether a warning message would be visible (at default verbosity).</summary>
        public bool IsWarnOn() { return IsLogOn(1, LogType.Warning); }
        /// <summary>Determines whether a warning message would be visible at specified verbosity.</summary>
        public bool IsWarnOn(uint verbosity) { return IsLogOn(verbosity, LogType.Warning); }
        /// <summary>Determines whether an error message would be visible (at default verbosity).</summary>
        public bool IsErrorOn() { return IsLogOn(1, LogType.Error); }
        /// <summary>Determines whether an error message would be visible at specified verbosity.</summary>
        public bool IsErrorOn(uint verbosity) { return IsLogOn(verbosity, LogType.Error); }
        /// <summary>Determines whether a debug message would be visible (at default verbosity).</summary>
        public bool IsDebugOn() { return IsLogOn(1, LogType.Debug); }
        /// <summary>Determines whether a debug message would be visible at specified verbosity.</summary>
        public bool IsDebugOn(uint verbosity) { return IsLogOn(verbosity, LogType.Debug); }

        /// <summary>Logs an exception with a stack trace.</summary>
        public void Exception(Exception exception) { Exception(exception, 1, LogType.Error); }
        /// <summary>Logs an exception with a stack trace.</summary>
        public void Exception(Exception exception, LogType type) { Exception(exception, 1, type); }

        /// <summary>
        /// Logs an exception with a stack trace at the specified verbosity and message type.
        /// Any InnerExceptions are also logged as appropriate.
        /// </summary>
        public void Exception(Exception exception, uint verbosity, LogType type)
        {
            if (!IsLogOn(verbosity, type))
                return;

            if (exception.InnerException != null)
                Exception(exception.InnerException, verbosity, type);

            Log(verbosity, type, "<{0}>: {1}".Fmt(exception.GetType().ToString(), exception.Message));
            Log(verbosity, type, exception.StackTrace);
        }
    }

    /// <summary>
    /// Implements a logger which doesn't do anything with the log messages. Use this as the
    /// default logger where no logging is wanted by default, to avoid checks for null in every log message.
    /// </summary>
    public sealed class NullLogger : LoggerBase
    {
        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Log(uint verbosity, LogType type, string message)
        {
        }
        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Separator()
        {
        }
    }

    /// <summary>
    /// Implements a logger which outputs messages to the console, word-wrapping
    /// long messages. Can use different colors for the different message types.
    /// </summary>
    [Serializable]
    public sealed class ConsoleLogger : LoggerBase
    {
        /// <summary>
        /// Set this to false to disable the word-wrapping of messages to the
        /// width of the console window.
        /// </summary>
        public bool WordWrap = true;

        /// <summary>
        /// Set this to false to ensure that all messages are printed to StdOut
        /// (aka Console.Out). By default error messages will be printed to
        /// StdErr instead (aka Console.Error).
        /// </summary>
        public bool ErrorsToStdErr = true;

        /// <summary>
        /// Constructs a new console logger.
        /// </summary>
        public ConsoleLogger()
        {
        }

        /// <summary>
        /// Gets a text color for each of the possible message types.
        /// </summary>
        public ConsoleColor GetMessageTypeColor(LogType type)
        {
            switch (type)
            {
                case LogType.Info: return ConsoleColor.White;
                case LogType.Warning: return ConsoleColor.Yellow;
                case LogType.Error: return ConsoleColor.Red;
                case LogType.Debug: return ConsoleColor.Green;
                default: return ConsoleColor.Gray;
            }
        }

        /// <summary>Logs a message to the console.</summary>
        public override void Log(uint verbosity, LogType type, string message)
        {
            lock (_lock_log)
            {
                if (VerbosityLimit[type] < verbosity)
                    return;

                string fmtInfo, indent;
                GetFormattedStrings(out fmtInfo, out indent, verbosity, type);

                TextWriter consoleStream = Console.Out;
                if (type == LogType.Error && ErrorsToStdErr)
                    consoleStream = Console.Error;

                var prevCol = Console.ForegroundColor;
                Console.ForegroundColor = GetMessageTypeColor(type);

                int wrapWidth = WordWrap ? ConsoleUtil.WrapToWidth() : int.MaxValue;
                bool first = true;
                foreach (var line in message.WordWrap(wrapWidth - fmtInfo.Length))
                {
                    consoleStream.Write(first ? fmtInfo : indent);
                    first = false;
                    consoleStream.WriteLine(line);
                }
                if (first)
                    consoleStream.WriteLine(fmtInfo); // don't completely skip blank messages

                Console.ForegroundColor = prevCol;
            }
        }

        /// <summary>Creates a visual separation in the log, for example if a new section starts.</summary>
        public override void Separator()
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine(new string('-', ConsoleUtil.WrapToWidth()));
            Console.Out.WriteLine();
        }
    }

    /// <summary>
    /// Implements a logger which puts messages into any <see cref="Stream"/> by creating a TextWriter wrapper around it.
    /// Use this logger only if the stream will remain open for the duration of the execution.
    /// </summary>
    [Serializable]
    public sealed class StreamLogger : LoggerBase
    {
        private Stream _underlyingStream = null;
        private StreamWriter _streamWriter;

        /// <summary>Creates a new instance.</summary>
        public StreamLogger()
        {
        }

        /// <summary>Creates a new instance.</summary>
        public StreamLogger(Stream underlyingStream)
        {
            Stream = underlyingStream;
        }

        /// <summary>
        /// Gets or sets the stream to which messages are logged.
        /// </summary>
        public Stream Stream
        {
            get { return _underlyingStream; }
            set
            {
                _underlyingStream = value;
                _streamWriter = new StreamWriter(_underlyingStream);
                _streamWriter.AutoFlush = true;
            }
        }

        /// <summary>
        /// Gets the <see cref="StreamWriter"/> used by this StreamLogger for writing
        /// text. Intended use is to enable the caller write arbitrary text to the
        /// underlying stream.
        /// </summary>
        public StreamWriter StreamWriter
        {
            get { return _streamWriter; }
        }

        /// <summary>Logs a message to the underlying stream.</summary>
        public override void Log(uint verbosity, LogType type, string message)
        {
            lock (_lock_log)
            {
                if (VerbosityLimit[type] < verbosity || _streamWriter == null)
                    return;

                string fmtInfo, indent;
                GetFormattedStrings(out fmtInfo, out indent, verbosity, type);
                _streamWriter.Write(fmtInfo);
                _streamWriter.WriteLine(message);
            }
        }

        /// <summary>Creates a visual separation in the log, for example if a new section starts.</summary>
        public override void Separator()
        {
            lock (_lock_log)
            {
                _streamWriter.WriteLine();
                _streamWriter.WriteLine(new string('-', 120));
                _streamWriter.WriteLine();
            }
        }
    }

    /// <summary>
    /// Implements a logger which appends messages to a file by opening and closing the file each time.
    /// This is in contrast to <see cref="StreamLogger"/>, which keeps the stream open.
    /// </summary>
    [Serializable]
    public sealed class FileAppendLogger : LoggerBase
    {
        /// <summary>Creates a new instance.</summary>
        public FileAppendLogger() { Filename = null; }

        /// <summary>Creates a new instance.</summary>
        public FileAppendLogger(string filename) { Filename = filename; }

        /// <summary>
        /// Gets or sets the path to the file to which messages are logged.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>Logs a message to the underlying stream.</summary>
        public override void Log(uint verbosity, LogType type, string message)
        {
            if (VerbosityLimit[type] < verbosity || Filename == null)
                return;

            lock (_lock_log)
            {
                string fmtInfo, indent;
                GetFormattedStrings(out fmtInfo, out indent, verbosity, type);

                using (var f = File.AppendText(Filename))
                {
                    f.Write(fmtInfo);
                    f.WriteLine(message);
                }
            }
        }

        /// <summary>Creates a visual separation in the log, for example if a new section starts.</summary>
        public override void Separator()
        {
            lock (_lock_log)
            {
                using (var f = File.AppendText(Filename))
                {
                    f.WriteLine();
                    f.WriteLine(new string('-', 120));
                    f.WriteLine();
                }
            }
        }
    }

    /// <summary>
    /// Implements a logger which can log messages to several other loggers. The
    /// underlying loggers can be configured as necessary; their settings will be
    /// respected.
    /// </summary>
    [Serializable]
    public sealed class MulticastLogger : LoggerBase
    {
        /// <summary>
        /// Add or remove the underlying loggers here. Every logger in this dictionary
        /// will be logged to.
        /// </summary>
        public readonly Dictionary<string, LoggerBase> Loggers = new Dictionary<string, LoggerBase>();

        /// <summary>Logs a message to the underlying loggers.</summary>
        public override void Log(uint verbosity, LogType type, string message)
        {
            lock (_lock_log)
            {
                foreach (LoggerBase logger in Loggers.Values)
                    logger.Log(verbosity, type, message);
            }
        }

        /// <summary>Creates a visual separation in the log, for example if a new section starts.</summary>
        public override void Separator()
        {
            lock (_lock_log)
            {
                foreach (LoggerBase logger in Loggers.Values)
                    logger.Separator();
            }
        }

        /// <summary>
        /// Returns false if logging a message with the specified settings would not
        /// actually result in any logger producing any output. When this is false it
        /// is safe for a program to skip logging a message with these settings.
        /// </summary>
        public override bool IsLogOn(uint verbosity, LogType type)
        {
            foreach (LoggerBase logger in Loggers.Values)
                if (logger.VerbosityLimit[type] >= verbosity)
                    return true;

            return false;
        }

        /// <summary>Configures the verbosity of every underlying logger. See <see cref="LoggerBase.ConfigureVerbosity"/> for more info.</summary>
        public override void ConfigureVerbosity(string settings)
        {
            foreach (var logger in Loggers.Values)
                logger.ConfigureVerbosity(settings);
        }
    }
}
