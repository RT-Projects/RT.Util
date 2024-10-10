using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace RT.Util.Consoles;

/// <summary>Console-related utility functions.</summary>
public static class ConsoleUtil
{
    private static void initialiseConsoleInfo()
    {
        _consoleInfoInitialised = true;

        _stdOutState = ConsoleState.Unavailable;
        _stdErrState = ConsoleState.Unavailable;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hOut = WinAPI.GetStdHandle(WinAPI.STD_OUTPUT_HANDLE);
            if (hOut != WinAPI.INVALID_HANDLE_VALUE)
                _stdOutState = WinAPI.GetFileType(hOut) == WinAPI.FILE_TYPE_CHAR ? ConsoleState.Console : ConsoleState.Redirected;

            var hErr = WinAPI.GetStdHandle(WinAPI.STD_ERROR_HANDLE);
            if (hErr != WinAPI.INVALID_HANDLE_VALUE)
                _stdErrState = WinAPI.GetFileType(hErr) == WinAPI.FILE_TYPE_CHAR ? ConsoleState.Console : ConsoleState.Redirected;
        }
    }

    /// <summary>Represents the state of a console output stream.</summary>
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
    ///     Determines the state of the standard output stream. The first call determines the state and caches it; subsequent
    ///     calls return the cached value.</summary>
    public static ConsoleState StdOutState()
    {
        if (!_consoleInfoInitialised)
            initialiseConsoleInfo();
        return _stdOutState;
    }

    /// <summary>
    ///     Determines the state of the standard error stream. The first call determines the state and caches it; subsequent
    ///     calls return the cached value.</summary>
    public static ConsoleState StdErrState()
    {
        if (!_consoleInfoInitialised)
            initialiseConsoleInfo();
        return _stdErrState;
    }

    /// <summary>
    ///     Returns the maximum line width that console applications should use to correctly word-wrap their output. If the
    ///     output is redirected to a file, this will return an arbitrary but sensible value, otherwise the value reflects the
    ///     width of the console buffer.</summary>
    public static int WrapToWidth()
    {
        if (StdOutState() == ConsoleState.Redirected || StdErrState() == ConsoleState.Redirected)
            return 120;
        int width;
        try { width = Console.BufferWidth - 1; }
        catch { return 120; }
        return width <= 0 ? 120 : width;
    }

    /// <summary>
    ///     Outputs the specified message to the console window, treating newlines as paragraph breaks. All paragraphs are
    ///     word-wrapped to fit in the console buffer, or to a sensible width if redirected to a file. Each paragraph is
    ///     indented by the number of spaces at the start of the corresponding line.</summary>
    /// <param name="message">
    ///     The message to output.</param>
    /// <param name="hangingIndent">
    ///     Specifies a number of spaces by which the message is indented in all but the first line of each paragraph.</param>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    public static void WriteParagraphs(string message, int hangingIndent = 0, bool stdErr = false)
    {
        var output = stdErr ? Console.Error : Console.Out;

        // Special case: if message is empty, WordWrap would output nothing
        if (message.Length == 0)
        {
            output.WriteLine();
            return;
        }
        int width;
        try
        {
            width = WrapToWidth();
        }
        catch
        {
            output.WriteLine(message);
            return;
        }
        foreach (var line in message.WordWrap(width, hangingIndent))
            output.WriteLine(line);
    }

    /// <summary>
    ///     Outputs the specified coloured message, marked up using EggsML, to the console window, treating newlines as
    ///     paragraph breaks. All paragraphs are word-wrapped to fit in the console buffer, or to a sensible width if
    ///     redirected to a file. Each paragraph is indented by the number of spaces at the start of the corresponding line.</summary>
    /// <param name="message">
    ///     The message to output.</param>
    /// <param name="hangingIndent">
    ///     Specifies a number of spaces by which the message is indented in all but the first line of each paragraph.</param>
    /// <remarks>
    ///     See <see cref="EggsNode.ToConsoleColoredStringWordWrap"/> for the colour syntax.</remarks>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    public static void WriteParagraphs(EggsNode message, int hangingIndent = 0, bool stdErr = false)
    {
        int width;
        try
        {
            width = WrapToWidth();
        }
        catch
        {
            // Fall back to non-word-wrapping
            WriteLine(ConsoleColoredString.FromEggsNode(message), stdErr);
            return;
        }
        bool any = false;
        foreach (var line in message.ToConsoleColoredStringWordWrap(width, hangingIndent))
        {
            WriteLine(line, stdErr);
            any = true;
        }

        // Special case: if the input is empty, output an empty line
        if (!any)
            WriteLine("", stdErr);
    }

    /// <summary>
    ///     Outputs the specified message to the console window, treating newlines as paragraph breaks. All paragraphs are
    ///     word-wrapped to fit in the console buffer, or to a sensible width if redirected to a file. Each paragraph is
    ///     indented by the number of spaces at the start of the corresponding line.</summary>
    /// <param name="message">
    ///     The message to output.</param>
    /// <param name="hangingIndent">
    ///     Specifies a number of spaces by which the message is indented in all but the first line of each paragraph.</param>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    public static void WriteParagraphs(ConsoleColoredString message, int hangingIndent = 0, bool stdErr = false)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        int width;
        try
        {
            width = WrapToWidth();
        }
        catch
        {
            WriteLine(message, stdErr);
            return;
        }
        foreach (var line in message.WordWrap(width, hangingIndent))
            WriteLine(line, stdErr);
    }

    /// <summary>Writes the specified <see cref="ConsoleColoredString"/> to the console.</summary>
    public static void Write(ConsoleColoredString value, bool stdErr = false)
    {
        if (value != null)
            value.writeTo(stdErr ? Console.Error : Console.Out);
    }

    /// <summary>
    ///     Writes the specified <see cref="FormattableString"/> to the console.</summary>
    /// <param name="value">
    ///     Formattable string. You can use an interpolated string literal here.</param>
    /// <param name="foreground">
    ///     Default foreground color when an interpolated variable isn’t a <see cref="ConsoleColoredString"/> or <see
    ///     cref="ConsoleColoredChar"/> or a foreground color is unspecified.</param>
    /// <param name="background">
    ///     Default background color when an interpolated variable isn’t a <see cref="ConsoleColoredString"/> or <see
    ///     cref="ConsoleColoredChar"/> or a background color is unspecified.</param>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    public static void Write(FormattableString value, ConsoleColor? foreground = null, ConsoleColor? background = null, bool stdErr = false)
    {
        Write(value.Format.Color(foreground, background).Fmt(value.GetArguments()), stdErr);
    }

    /// <summary>
    ///     Writes the specified <see cref="FormattableString"/> to the console.</summary>
    /// <param name="value">
    ///     Formattable string. You can use an interpolated string literal here.</param>
    /// <param name="foreground">
    ///     Default foreground color when an interpolated variable isn’t a <see cref="ConsoleColoredString"/> or <see
    ///     cref="ConsoleColoredChar"/> or a foreground color is unspecified.</param>
    /// <param name="background">
    ///     Default background color when an interpolated variable isn’t a <see cref="ConsoleColoredString"/> or <see
    ///     cref="ConsoleColoredChar"/> or a background color is unspecified.</param>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    /// <param name="align">
    ///     Horizontal alignment of the string within the remaining space of the current line. If the string does not fit, it
    ///     will be printed as if left-aligned.</param>
    public static void WriteLine(FormattableString value, ConsoleColor? foreground = null, ConsoleColor? background = null, bool stdErr = false, HorizontalTextAlignment align = HorizontalTextAlignment.Left)
    {
        WriteLine(value.Format.Color(foreground, background).Fmt(value.GetArguments()), stdErr, align);
    }

    /// <summary>
    ///     Writes the specified <see cref="ConsoleColoredString"/> followed by a newline to the console.</summary>
    /// <param name="value">
    ///     The string to print to the console.</param>
    /// <param name="stdErr">
    ///     <c>true</c> to print to Standard Error instead of Standard Output.</param>
    /// <param name="align">
    ///     Horizontal alignment of the string within the remaining space of the current line. If the string does not fit, it
    ///     will be printed as if left-aligned.</param>
    public static void WriteLine(ConsoleColoredString value, bool stdErr = false, HorizontalTextAlignment align = HorizontalTextAlignment.Left)
    {
        var output = stdErr ? Console.Error : Console.Out;
        if (value != null)
        {
            var cursorLeft = 0;
            try { cursorLeft = Console.CursorLeft; }
            catch { }
            var width = WrapToWidth() - cursorLeft;
            if (align == HorizontalTextAlignment.Center && width > value.Length)
                output.Write(new string(' ', (width - value.Length) / 2));
            else if (align == HorizontalTextAlignment.Right && width > value.Length)
                output.Write(new string(' ', width - value.Length));
            value.writeTo(output);
        }
        output.WriteLine();
    }

    /// <summary>
    ///     Writes the specified or current stack trace to the console in pretty colors.</summary>
    /// <param name="stackTraceLines">
    ///     The stack trace. Each string in this collection is expected to be one line of the stack trace. If <c>null</c>,
    ///     defaults to the current stack trace.</param>
    public static void WriteStackTrace(IEnumerable<string> stackTraceLines = null)
    {
        if (stackTraceLines == null)
            stackTraceLines = Environment.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Skip(3);
        foreach (var traceLine in stackTraceLines)
        {
            var m = Regex.Match(traceLine, @"^\s*at ([\w\.]+\.)([\w`<>]+)\.([\w\[\],<>]+)(\(.*\))( in (.:\\.*\\)([^\\]+\.cs):line (\d+))?\s*$");
            if (m.Success)
                ConsoleUtil.WriteParagraphs("    - ".Color(ConsoleColor.DarkGreen) +
                    m.Groups[1].Value.Color(ConsoleColor.DarkGray) +
                    m.Groups[2].Value.Color(ConsoleColor.Cyan) + ".".Color(ConsoleColor.DarkGray) +
                    m.Groups[3].Value.Color(ConsoleColor.White) +
                    m.Groups[4].Value.Color(ConsoleColor.Green) +
                    (m.Groups[5].Length > 0 ?
                        " in ".Color(ConsoleColor.DarkGray) +
                        m.Groups[6].Value.Color(ConsoleColor.DarkYellow) + m.Groups[7].Value.Color(ConsoleColor.Yellow) + " line ".Color(ConsoleColor.DarkMagenta) +
                        m.Groups[8].Value.Color(ConsoleColor.Magenta)
                    : ""), 8
                );
            else
            {
                m = Regex.Match(traceLine, @"^\s*at (.*?)\s*$");
                if (m.Success)
                    ConsoleUtil.WriteParagraphs("    - ".Color(ConsoleColor.DarkGreen) + m.Groups[1].Value.Color(ConsoleColor.DarkGray), 8);
                else
                    ConsoleUtil.WriteParagraphs(traceLine, 8);
            }
        }
    }
}
