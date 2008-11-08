using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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
        /// Specifies a short string describing each log type. Defaults to:
        /// INFO, WARN, ERROR, DEBUG.
        /// </summary>
        public Dictionary<LogType, string> MsgTypeString = new Dictionary<LogType, string>();

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
            // Default type strings
            MsgTypeString[LogType.Info] = "INFO";
            MsgTypeString[LogType.Warning] = "WARN";
            MsgTypeString[LogType.Error] = "ERROR";
            MsgTypeString[LogType.Debug] = "DEBUG";

            // Default visibility levels
            VerbosityLimit[LogType.Info] = 1;
            VerbosityLimit[LogType.Warning] = 1;
            VerbosityLimit[LogType.Error] = 1;
            VerbosityLimit[LogType.Debug] = 0;
        }

        /// <summary>
        /// Takes a string which encodes verbosity limit configuration, parses it
        /// and sets the limits accordingly. On failure throws an ArgumentException
        /// with a fairly detailed description of the string format.
        /// 
        /// Examples of valid strings:
        ///     "" - use defaults
        ///     "3" - set all limits to 3
        ///     "2d0" - set all limits to 2, then set the debug limit to 0.
        ///     "i0w1e2d3" set info=0, warning=1, error=2, debug=3.
        ///     
        /// Intended use: configuring the logger via a command-line option.
        /// </summary>
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
        /// <paramref name="fmtText"/> which is the actual message.
        /// </summary>
        protected virtual void GetFormattedStrings(out string fmtInfo, out string fmtText, uint verbosity, LogType type, string message, object[] args)
        {
            string timestamp = (TimestampInUTC ? DateTime.Now.ToUniversalTime() : DateTime.Now).ToString(TimestampFormat);
            if (args.Length > 0)
                fmtText = string.Format(message, args);
            else
                fmtText = message;

            fmtInfo = string.Format(MessageFormat, timestamp, MsgTypeString[type], verbosity);
        }

        /// <summary>
        /// Appends an entry to the log. Derived classes implement this to put the
        /// log data where necessary. See also: <see cref="GetFormattedStrings"/> which
        /// is a helper method.
        /// </summary>
        /// <remarks>
        /// Note that the various specialised functions such as <see cref="Warn(string, object[])"/> simply
        /// call this method to do the work.
        /// </remarks>
        /// <param name="verbosity">Verbosity level of this message</param>
        /// <param name="type">Message type - info, warning, error or debug</param>
        /// <param name="message">The message itself</param>
        /// <param name="args">If present, will be string.Format'ted into the message.
        /// Otherwise "message" will be logged as-is.</param>
        public abstract void Log(uint verbosity, LogType type, string message, params object[] args);

        /// <summary>
        /// Returns true if a message of the specified verbosity and type will actually
        /// end up being logged.
        /// </summary>
        public virtual bool IsLogOn(uint verbosity, LogType type)
        {
            return VerbosityLimit[type] >= verbosity;
        }

        /// <summary>Appends an informational message to the log.</summary>
        public void Info(string message, params object[] args) { Log(1, LogType.Info, message, args); }
        /// <summary>Appends an informational message to the log.</summary>
        public void Info(uint verbosity, string message, params object[] args) { Log(verbosity, LogType.Info, message, args); }
        /// <summary>Appends a warning message to the log.</summary>
        public void Warn(string message, params object[] args) { Log(1, LogType.Warning, message, args); }
        /// <summary>Appends a warning message to the log.</summary>
        public void Warn(uint verbosity, string message, params object[] args) { Log(verbosity, LogType.Warning, message, args); }
        /// <summary>Appends an error message to the log.</summary>
        public void Error(string message, params object[] args) { Log(1, LogType.Error, message, args); }
        /// <summary>Appends an error message to the log.</summary>
        public void Error(uint verbosity, string message, params object[] args) { Log(verbosity, LogType.Error, message, args); }
        /// <summary>Appends a debug message to the log.</summary>
        public void Debug(string message, params object[] args) { Log(1, LogType.Debug, message, args); }
        /// <summary>Appends a debug message to the log.</summary>
        public void Debug(uint verbosity, string message, params object[] args) { Log(verbosity, LogType.Debug, message, args); }

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

            Log(verbosity, type, "<{0}>", exception.GetType().ToString());
            Log(verbosity, type, exception.StackTrace);
        }
    }

    /// <summary>
    /// Implements a logger which outputs messages to the console, word-wrapping
    /// long messages. Can use different colors for the different message types.
    /// </summary>
    [Serializable]
    public class ConsoleLogger : LoggerBase
    {
        /// <summary>
        /// Specifies text colors for each of the possible message types.
        /// </summary>
        public Dictionary<LogType, ConsoleColor> MsgTypeColor = new Dictionary<LogType, ConsoleColor>();

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

        public ConsoleLogger()
        {
            // Default colors
            MsgTypeColor[LogType.Info] = ConsoleColor.White;
            MsgTypeColor[LogType.Warning] = ConsoleColor.Yellow;
            MsgTypeColor[LogType.Error] = ConsoleColor.Red;
            MsgTypeColor[LogType.Debug] = ConsoleColor.Green;
        }

        public override void Log(uint verbosity, LogType type, string message, params object[] args)
        {
            lock (_lock_log)
            {
                if (VerbosityLimit[type] < verbosity)
                    return;

                string fmtInfo, fmtText;
                GetFormattedStrings(out fmtInfo, out fmtText, verbosity, type, message, args);

                TextWriter consoleStream = Console.Out;
                if (type == LogType.Error && ErrorsToStdErr)
                    consoleStream = Console.Error;

                Console.ForegroundColor = MsgTypeColor[type];

                if (!WordWrap)
                {
                    consoleStream.Write(fmtInfo);
                    consoleStream.WriteLine(fmtText);
                }
                else
                {
                    string indent = new string(' ', fmtInfo.Length);
                    bool first = true;
                    foreach (var line in fmtText.WordWrap(Console.WindowWidth - 1 - fmtInfo.Length))
                    {
                        consoleStream.Write(first ? fmtInfo : indent);
                        first = false;
                        consoleStream.WriteLine(line);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Implements a logger which puts messages into any <see cref="Stream"/> by
    /// creating a TextWriter wrapper around it.
    /// </summary>
    [Serializable]
    public class StreamLogger : LoggerBase
    {
        private Stream underlyingStream = null;
        private StreamWriter textStream;

        public StreamLogger()
        {
        }

        public StreamLogger(Stream underlyingStream)
        {
            Stream = underlyingStream;
        }

        /// <summary>
        /// Gets or sets the stream to which messages are logged.
        /// </summary>
        public Stream Stream
        {
            get { return underlyingStream; }
            set
            {
                underlyingStream = value;
                textStream = new StreamWriter(underlyingStream);
                textStream.AutoFlush = true;
            }
        }

        /// <summary>
        /// Gets the <see cref="StreamWriter"/> used by this StreamLogger for writing
        /// text. Intended use is to enable the caller write arbitrary text to the
        /// underlying stream.
        /// </summary>
        public StreamWriter StreamWriter
        {
            get { return textStream; }
        }

        public override void Log(uint verbosity, LogType type, string message, params object[] args)
        {
            lock (_lock_log)
            {
                if (VerbosityLimit[type] < verbosity || textStream == null)
                    return;

                string fmtInfo, fmtText;
                GetFormattedStrings(out fmtInfo, out fmtText, verbosity, type, message, args);

                textStream.Write(fmtInfo);
                textStream.WriteLine(fmtText);
            }
        }
    }

    /// <summary>
    /// Implements a logger which can log messages to several other loggers. The
    /// underlying loggers can be configured as necessary; their settings will be
    /// respected.
    /// </summary>
    [Serializable]
    public class MulticastLogger : LoggerBase
    {
        /// <summary>
        /// Add or remove the underlying loggers here. Every logger in this dictionary
        /// will be logged to.
        /// </summary>
        public readonly Dictionary<string, LoggerBase> Loggers = new Dictionary<string, LoggerBase>();

        public override void Log(uint verbosity, LogType type, string message, params object[] args)
        {
            lock (_lock_log)
            {
                foreach (LoggerBase logger in Loggers.Values)
                    logger.Log(verbosity, type, message, args);
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

        public override void ConfigureVerbosity(string settings)
        {
            throw new InvalidOperationException("The verbosity of the MulticastLogger cannot be configured. You should configure the verbosity of the underlying loggers instead.");
        }
    }
}
