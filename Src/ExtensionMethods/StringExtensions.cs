using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="String"/> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Concatenates the specified number of repetitions of the current string.
        /// </summary>
        /// <param name="input">The string to be repeated.</param>
        /// <param name="numTimes">The number of times to repeat the string.</param>
        /// <returns>A concatenated string containing the original string the specified number of times.</returns>
        public static string Repeat(this string input, int numTimes)
        {
            if (numTimes == 0) return "";
            if (numTimes == 1) return input;
            if (numTimes == 2) return input + input;
            var sb = new StringBuilder();
            for (int i = 0; i < numTimes; i++)
                sb.Append(input);
            return sb.ToString();
        }

        /// <summary>
        /// Escapes all necessary characters in the specified string so as to make it usable safely in an HTML or XML context.
        /// </summary>
        /// <param name="input">The string to apply HTML or XML escaping to.</param>
        /// <returns>The specified string with the necessary HTML or XML escaping applied.</returns>
        public static string HtmlEscape(this string input)
        {
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        /// <summary>
        /// Returns the set of characters allowed in a URL.
        /// </summary>
        public static string UrlAllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$-_.!*'(),/:;@";

        /// <summary>
        /// Escapes all necessary characters in the specified string so as to make it usable safely in a URL.
        /// </summary>
        /// <param name="input">The string to apply URL escaping to.</param>
        /// <returns>The specified string with the necessary URL escaping applied.</returns>
        /// <seealso cref="UrlUnescape(string)"/>
        public static string UrlEscape(this string input)
        {
            byte[] utf8 = input.ToUtf8();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in utf8)
                if (UrlAllowedCharacters.Contains((char) b))
                    sb.Append((char) b);
                else
                    sb.Append(string.Format("%{0:X2}", b));
            return sb.ToString();
        }

        /// <summary>
        /// Reverses the escaping performed by <see cref="UrlEscape"/> by decoding hexadecimal URL escape sequences into their original characters.
        /// </summary>
        /// <param name="input">String containing URL escape sequences to be decoded.</param>
        /// <returns>The specified string with all URL escape sequences decoded.</returns>
        /// /// <seealso cref="UrlEscape(string)"/>
        public static string UrlUnescape(this string input)
        {
            if (input.Length < 3)
                return input;

            int bufferSize = input.Length;
            for (int i = 0; i < input.Length; i++)
                if (input[i] == '%') { bufferSize -= 2; }

            byte[] buffer = new byte[bufferSize];

            bufferSize = 0;
            int j = 0;
            while (j < input.Length)
            {
                if (input[j] == '%')
                {
                    try
                    {
                        buffer[bufferSize] = byte.Parse("" + input[j + 1] + input[j + 2], NumberStyles.HexNumber);
                        bufferSize++;
                    }
                    catch (Exception) { }
                    j += 3;
                }
                else
                {
                    buffer[bufferSize] = input[j] == '+' ? (byte) ' ' : (byte) input[j];
                    bufferSize++;
                    j++;
                }
            }
            return Encoding.UTF8.GetString(buffer, 0, bufferSize);
        }

        /// <summary>
        /// Converts the specified string to UTF-8.
        /// </summary>
        /// <param name="input">String to convert to UTF-8.</param>
        /// <returns>The specified string, converted to a byte-array containing the UTF-8 encoding of the string.</returns>
        public static byte[] ToUtf8(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        /// <summary>
        /// Converts the specified raw UTF-8 data to a string.
        /// </summary>
        /// <param name="input">Data to interpret as UTF-8 text.</param>
        /// <returns>A string containing the characters represented by the UTF-8-encoded input.</returns>
        public static string FromUtf8(this byte[] input)
        {
            return Encoding.UTF8.GetString(input);
        }

        /// <summary>
        /// Determines the length of the UTF-8 encoding of the specified string.
        /// </summary>
        /// <param name="input">String to determined UTF-8 length of.</param>
        /// <returns>The length of the string in bytes when encoded as UTF-8.</returns>
        public static int Utf8Length(this string input)
        {
            return Encoding.UTF8.GetByteCount(input);
        }

        /// <summary>
        /// Converts the specified string to a byte array. Non-ASCII characters are replaced with question marks ('?').
        /// </summary>
        /// <param name="input">String to convert to a byte array.</param>
        /// <returns>The specified string, converted to a byte-array with non-ASCII characters replaced with question marks ('?').</returns>
        public static byte[] ToAscii(this string input)
        {
            return Encoding.ASCII.GetBytes(input);
        }

        /// <summary>
        /// Returns a JavaScript-compatible representation of the string in double-quotes with the appropriate characters escaped.
        /// </summary>
        /// <param name="input">String to escape.</param>
        /// <returns>JavaScript-compatible representation of the input string.</returns>
        public static string JsEscape(this string input)
        {
            return "\"" + input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        /// <summary>
        /// Joins all strings in <see pref="values"/> using the string as the separator.
        /// <example>
        ///     <code>
        ///         var a = ", ".Join(new[] { 'Paris', 'London', 'Tokyo' });
        ///         // a contains "Paris, London, Tokyo"
        ///     </code>
        /// </example>
        /// </summary>
        public static string Join(this string separator, IEnumerable<string> values)
        {
            return separator.Join(values.GetEnumerator());
        }

        /// <summary>
        /// Joins all strings in <see pref="values"/> using the string as the separator.
        /// <example>
        ///     <code>
        ///         var a = ", ".Join(new[] { 'Paris', 'London', 'Tokyo' });
        ///         // a contains "Paris, London, Tokyo"
        ///     </code>
        /// </example>
        /// </summary>
        public static string Join(this string separator, IEnumerator<string> values)
        {
            if (!values.MoveNext()) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(values.Current);
            while (values.MoveNext())
            {
                sb.Append(separator);
                sb.Append(values.Current);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Joins all strings in the enumerable using the specified string as the separator.
        /// <example>
        ///     <code>
        ///         var a = (new[] { 'Paris', 'London', 'Tokyo' }).Join(", ");
        ///         // a contains "Paris, London, Tokyo"
        ///     </code>
        /// </example>
        /// </summary>
        public static string Join(this IEnumerable<string> values, string separator)
        {
            return separator.Join(values);
        }

        /// <summary>
        /// Formats a string using <see cref="string.Format(string, object[])"/>.
        /// </summary>
        public static string Fmt(this string formatString, params object[] args)
        {
            return string.Format(formatString, args);
        }

        /// <summary>
        /// Formats a string using <see cref="string.Format(string, object)"/>.
        /// </summary>
        public static string Fmt(this string formatString, object arg0)
        {
            return string.Format(formatString, arg0);
        }

        /// <summary>
        /// Formats a string using <see cref="string.Format(string, object, object)"/>.
        /// </summary>
        public static string Fmt(this string formatString, object arg0, object arg1)
        {
            return string.Format(formatString, arg0, arg1);
        }

        /// <summary>
        /// Formats a string using <see cref="string.Format(string, object, object, object)"/>.
        /// </summary>
        public static string Fmt(this string formatString, object arg0, object arg1, object arg2)
        {
            return string.Format(formatString, arg0, arg1, arg2);
        }

        /// <summary>
        /// Word-wraps the current string to a specified width. Supports unix-style
        /// newlines and indented paragraphs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The supplied text will be split into "paragraphs" on the newline characters.
        /// Every paragraph will begin on a new line in the word-wrapped output, indented
        /// by the same number of spaces as in the input. All subsequent lines belonging
        /// to that paragraph will also be indented by the same amount.</para>
        /// <para>
        /// All multiple contiguous spaces will be replaced with a single space
        /// (except for the indentation).</para>
        /// </remarks>
        /// <param name="text">Text to be word-wrapped.</param>
        /// <param name="maxWidth">The maximum number of characters permitted
        /// on a single line, not counting the end-of-line terminator.</param>
        public static IEnumerable<string> WordWrap(this string text, int maxWidth)
        {
            if (text == null || text == "")
                yield break;
            if (maxWidth < 1)
                throw new ArgumentOutOfRangeException("maxWidth cannot be less than 1");

            Regex regexSplitOnWindowsNewline = new Regex(@"\r\n", RegexOptions.Compiled);
            Regex regexSplitOnUnixMacNewline = new Regex(@"[\r\n]", RegexOptions.Compiled);
            Regex regexKillDoubleSpaces = new Regex(@"  +", RegexOptions.Compiled);

            // Split into "paragraphs"
            foreach (string para in regexSplitOnWindowsNewline.Split(text))
            {
                foreach (string paragraph in regexSplitOnUnixMacNewline.Split(para))
                {
                    // Count the number of spaces at the start of the paragraph
                    int indentLen = 0;
                    while (indentLen < paragraph.Length && paragraph[indentLen] == ' ')
                        indentLen++;

                    // Get a list of words
                    string[] words = regexKillDoubleSpaces.Replace(paragraph.Substring(indentLen), " ").Split(' ');

                    StringBuilder curLine = new StringBuilder();
                    string indent = new string(' ', indentLen);
                    string space = indent;

                    for (int i = 0; i < words.Length; i++)
                    {
                        string word = words[i];

                        if (curLine.Length + space.Length + word.Length > maxWidth)
                        {
                            // Need to wrap
                            if (word.Length > maxWidth)
                            {
                                // This is a very long word
                                // Leave part of the word on the current line but only if at least 2 chars fit
                                if (curLine.Length + space.Length + 2 <= maxWidth)
                                {
                                    int length = maxWidth - curLine.Length - space.Length;
                                    curLine.Append(space);
                                    curLine.Append(word.Substring(0, length));
                                    word = word.Substring(length);
                                }
                                // Commit the current line
                                yield return curLine.ToString();

                                // Now append full lines' worth of text until we're left with less than a full line
                                while (indent.Length + word.Length > maxWidth)
                                {
                                    yield return indent + word.Substring(0, maxWidth - indent.Length);
                                    word = word.Substring(maxWidth - indent.Length);
                                }

                                // Start a new line with whatever is left
                                curLine = new StringBuilder();
                                curLine.Append(indent);
                                curLine.Append(word);
                            }
                            else
                            {
                                // This word is not very long and it doesn't fit so just wrap it to the next line
                                yield return curLine.ToString();

                                // Start a new line
                                curLine = new StringBuilder();
                                curLine.Append(indent);
                                curLine.Append(word);
                            }
                        }
                        else
                        {
                            // No need to wrap yet
                            curLine.Append(space);
                            curLine.Append(word);
                        }

                        space = " ";
                    }

                    yield return curLine.ToString();
                }
            }
        }
    }
}
