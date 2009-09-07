using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util
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
        /// Returns the width of the console as far as word wrapping is concerned. This is the true width
        /// of the console if it is available and neither stdout nor stderr are redirected. In all other
        /// cases returns int.MaxValue.
        /// </summary>
        public static int WrapWidth()
        {
            if (StdOutState() == ConsoleState.Redirected || StdErrState() == ConsoleState.Redirected)
                return int.MaxValue;
            try { return Console.WindowWidth; }
            catch { return int.MaxValue; }
        }

        /// <summary>
        /// Outputs the specified message to the console window, word-wrapping to the console window's width if possible.
        /// </summary>
        public static void WriteLine(string message)
        {
            try
            {
                int width = Console.BufferWidth;
                foreach (var line in message.WordWrap(width - 1))
                    Console.WriteLine(line);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }
    }
}
