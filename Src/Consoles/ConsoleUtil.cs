using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Consoles
{
    /// <summary>
    /// Console-related utility functions.
    /// </summary>
    public static class ConsoleUtil
    {
        private static void initialiseConsoleInfo()
        {
            _consoleInfoInitialised = true;

            var hOut = WinAPI.GetStdHandle(WinAPI.STD_OUTPUT_HANDLE);
            if (hOut == WinAPI.INVALID_HANDLE_VALUE)
                _stdOutState = ConsoleState.Unavailable;
            else
                _stdOutState = WinAPI.GetFileType(hOut) == WinAPI.FILE_TYPE_CHAR ? ConsoleState.Console : ConsoleState.Redirected;

            var hErr = WinAPI.GetStdHandle(WinAPI.STD_ERROR_HANDLE);
            if (hErr == WinAPI.INVALID_HANDLE_VALUE)
                _stdErrState = ConsoleState.Unavailable;
            else
                _stdErrState = WinAPI.GetFileType(hErr) == WinAPI.FILE_TYPE_CHAR ? ConsoleState.Console : ConsoleState.Redirected;
        }

        /// <summary>
        /// Represents the state of a console output stream.
        /// </summary>
        public enum ConsoleState
        {
            /// <summary>This output stream is not available - eg when the program is not a console program.</summary>
            Unavailable,
            /// <summary>This output stream is printed on the console.</summary>
            Console,
            /// <summary>This output stream has been redirected - perhaps to a file or a pipe.</summary>
            Redirected,
        }

        private static bool _consoleInfoInitialised = false;
        private static ConsoleState _stdOutState;
        private static ConsoleState _stdErrState;

        /// <summary>
        /// Determines the state of the standard output stream. The first call determines the state
        /// and caches it; subsequent calls return the cached value.
        /// </summary>
        public static ConsoleState StdOutState()
        {
            if (!_consoleInfoInitialised)
                initialiseConsoleInfo();
            return _stdOutState;
        }

        /// <summary>
        /// Determines the state of the standard error stream. The first call determines the state
        /// and caches it; subsequent calls return the cached value.
        /// </summary>
        public static ConsoleState StdErrState()
        {
            if (!_consoleInfoInitialised)
                initialiseConsoleInfo();
            return _stdErrState;
        }

        /// <summary>
        /// Returns the maximum line width that all code wishing to correctly word-wrap its text output
        /// should use. If the output is redirected to a file this will return an arbitrary but sensible value,
        /// otherwise the value reflects the width of the console buffer.
        /// </summary>
        public static int WrapToWidth()
        {
            if (StdOutState() == ConsoleState.Redirected || StdErrState() == ConsoleState.Redirected)
                return 120;
            try { return Console.BufferWidth - 1; }
            catch { return 120; }
        }

        /// <summary>
        /// Outputs the specified message to the console window, treating newlines as paragraph breaks. All
        /// paragraphs are word-wrapped to fit in the console buffer, or to a sensible width if redirected to
        /// a file. Each paragraph is indented by the number of spaces at the start of the corresponding line.
        /// </summary>
        public static void WriteParagraphs(string message)
        {
            WriteParagraphs(message, 0);
        }

        /// <summary>
        /// Outputs the specified message to the console window, treating newlines as paragraph breaks. All
        /// paragraphs are word-wrapped to fit in the console buffer, or to a sensible width if redirected to
        /// a file. Each paragraph is indented by the number of spaces at the start of the corresponding line.
        /// </summary>
        /// <param name="message">The message to output.</param>
        /// <param name="hangingIndent">Specifies a number of spaces by which the message is indented in all but the first line of each paragraph.</param>
        public static void WriteParagraphs(string message, int hangingIndent)
        {
            try
            {
                int width = WrapToWidth();
                foreach (var line in message.WordWrap(width, hangingIndent))
                    Console.WriteLine(line);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Outputs the specified message to the console window, treating newlines as paragraph breaks. All
        /// paragraphs are word-wrapped to fit in the console buffer, or to a sensible width if redirected to
        /// a file. Each paragraph is indented by the number of spaces at the start of the corresponding line.
        /// </summary>
        public static void WriteParagraphs(ConsoleColoredString message)
        {
            WriteParagraphs(message, 0);
        }

        /// <summary>
        /// Outputs the specified message to the console window, treating newlines as paragraph breaks. All
        /// paragraphs are word-wrapped to fit in the console buffer, or to a sensible width if redirected to
        /// a file. Each paragraph is indented by the number of spaces at the start of the corresponding line.
        /// </summary>
        /// <param name="message">The message to output.</param>
        /// <param name="hangingIndent">Specifies a number of spaces by which the message is indented in all but the first line of each paragraph.</param>
        public static void WriteParagraphs(ConsoleColoredString message, int hangingIndent)
        {
            try
            {
                int width = WrapToWidth();
                foreach (var line in message.WordWrap(width, hangingIndent))
                    ConsoleUtil.WriteLine(line);
            }
            catch
            {
                ConsoleUtil.WriteLine(message);
            }
        }

        /// <summary>Writes the specified <see cref="ConsoleColoredString"/> to the console.</summary>
        public static void Write(ConsoleColoredString value)
        {
            value.writeToConsole();
        }

        /// <summary>Writes the specified <see cref="ConsoleColoredString"/> followed by a newline to the console.</summary>
        public static void WriteLine(ConsoleColoredString value)
        {
            value.writeToConsole();
            Console.WriteLine();
        }
    }
}
