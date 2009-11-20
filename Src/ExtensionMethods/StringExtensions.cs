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
        private const string _charsBase64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private static int[] _invBase64Url; // inverse base-64-url lookup table

        static StringExtensions()
        {
            // Initialise the base-64-url inverse lookup table
            _invBase64Url = new int[256];
            for (int i = 0; i < _invBase64Url.Length; i++)
                _invBase64Url[i] = -1;
            for (int i = 0; i < _charsBase64Url.Length; i++)
                _invBase64Url[(int) _charsBase64Url[i]] = i;
        }

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
        /// Converts the specified byte array to a string where each byte turns into an ASCII character. Bytes greater than 0x7F are replaced with question marks ('?').
        /// </summary>
        /// <param name="input">Byte array to convert to a string.</param>
        /// <returns>The specified byte array, converted to a string with non-ASCII characters replaced with question marks ('?').</returns>
        public static string FromAscii(this byte[] input)
        {
            return Encoding.ASCII.GetString(input);
        }

        /// <summary>
        /// Returns a JavaScript-compatible representation of the string in double-quotes with the appropriate characters escaped.
        /// </summary>
        /// <param name="input">String to escape.</param>
        /// <returns>JavaScript-compatible representation of the input string.</returns>
        public static string JsEscape(this string input)
        {
            return "\"" + input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("</", "<\"+\"/") + "\"";
        }

        /// <summary>
        /// Returns an SQL-compatible representation of the string in single-quotes with the appropriate characters escaped.
        /// </summary>
        /// <param name="input">String to escape.</param>
        /// <returns>SQL-compatible representation of the input string.</returns>
        public static string SqlEscape(this string input)
        {
            return "'" + input.Replace("'", "''") + "'";
        }

        /// <summary>
        /// Encodes this byte array to base-64-url format, which is safe for use in URLs and
        /// does not contain the unnecessary padding when the number of bytes is not divisible
        /// by 3.
        /// </summary>
        public static string Base64UrlEncode(this byte[] bytes)
        {
            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < bytes.Length)
            {
                if (bytes.Length - i >= 3)
                {
                    // 000000 001111 111122 222222
                    result.Append(_charsBase64Url[bytes[i] >> 2]);
                    result.Append(_charsBase64Url[(bytes[i] & 3) << 4 | bytes[i + 1] >> 4]);
                    result.Append(_charsBase64Url[(bytes[i + 1] & 15) << 2 | bytes[i + 2] >> 6]);
                    result.Append(_charsBase64Url[bytes[i + 2] & 63]);
                    i += 3;
                }
                else if (bytes.Length - i == 2)
                {
                    // 000000 001111 1111--
                    result.Append(_charsBase64Url[bytes[i] >> 2]);
                    result.Append(_charsBase64Url[(bytes[i] & 3) << 4 | bytes[i + 1] >> 4]);
                    result.Append(_charsBase64Url[(bytes[i + 1] & 15) << 2]);
                    i += 2;
                }
                else /* if (bytes.Length - i == 1) -- always true here given the while condition */
                {
                    // 000000 00----
                    result.Append(_charsBase64Url[bytes[i] >> 2]);
                    result.Append(_charsBase64Url[(bytes[i] & 3) << 4]);
                    i += 1;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Decodes this string from base-64-url format into a byte array. See
        /// <see cref="Base64UrlEncode"/> for more info on this format.
        /// </summary>
        public static byte[] Base64UrlDecode(this string input)
        {
            // See how many bytes are encoded at the end of the string
            int padding = input.Length % 4;
            if (padding == 1)
                throw new ArgumentException("The input string to Base64UrlDecode is not a valid base-64-url encoded string");
            if (padding > 0)
                padding--;

            byte[] result = new byte[(input.Length / 4) * 3 + padding];
            int ri = 0, ii = 0; // result index & input index

            while (ii < input.Length)
            {
                if (input.Length - ii >= 4)
                {
                    // 00000011 11112222 22333333
                    uint v0 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v1 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v2 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v3 = checked((uint) _invBase64Url[input[ii++]]);
                    result[ri++] = (byte) (v0 << 2 | v1 >> 4);
                    result[ri++] = (byte) ((v1 & 15) << 4 | v2 >> 2);
                    result[ri++] = (byte) ((v2 & 3) << 6 | v3);
                }
                else if (input.Length - ii == 3)
                {
                    // 00000011 11112222 [22------]
                    uint v0 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v1 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v2 = checked((uint) _invBase64Url[input[ii++]]);
                    result[ri++] = (byte) (v0 << 2 | v1 >> 4);
                    result[ri++] = (byte) ((v1 & 15) << 4 | v2 >> 2);
                }
                else if (input.Length - ii == 2)
                {
                    // 00000011 [1111----]
                    uint v0 = checked((uint) _invBase64Url[input[ii++]]);
                    uint v1 = checked((uint) _invBase64Url[input[ii++]]);
                    result[ri++] = (byte) (v0 << 2 | v1 >> 4);
                }
                else
                    throw new InternalError("Internal error in Base64UrlDecode");
            }

            return result;
        }

        /// <summary>
        /// Escapes all characters in this string whose code is less than 32 using C/C#-compatible backslash escapes.
        /// </summary>
        public static string CLiteralEscape(this string value)
        {
            var result = new StringBuilder(value.Length + value.Length / 2);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\0': result.Append(@"\0"); break;
                    case '\a': result.Append(@"\a"); break;
                    case '\b': result.Append(@"\b"); break;
                    case '\t': result.Append(@"\t"); break;
                    case '\n': result.Append(@"\n"); break;
                    case '\v': result.Append(@"\v"); break;
                    case '\f': result.Append(@"\f"); break;
                    case '\r': result.Append(@"\r"); break;
                    case '\\': result.Append(@"\\"); break;
                    default:
                        if (c >= ' ')
                            result.Append(c);
                        else
                            result.AppendFormat(@"\x{0:X2}", (int) c);
                        break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Reverses the escaping done by <see cref="CLiteralEscape"/>. Note that unescaping is not fully C/C#-compatible
        /// in the sense that not all strings that are valid string literals in C/C# can be correctly unescaped by this procedure.
        /// </summary>
        public static string CLiteralUnescape(this string value)
        {
            var result = new StringBuilder(value.Length);

            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c != '\\')
                    result.Append(c);
                else
                {
                    if (i + 1 >= value.Length)
                        throw new ArgumentException("String ends before the escape sequence at position {0} is complete".Fmt(i), "value");
                    i++;
                    c = value[i];
                    switch (c)
                    {
                        case '0': result.Append('\0'); break;
                        case 'a': result.Append('\a'); break;
                        case 'b': result.Append('\b'); break;
                        case 't': result.Append('\t'); break;
                        case 'n': result.Append('\n'); break;
                        case 'v': result.Append('\v'); break;
                        case 'f': result.Append('\f'); break;
                        case 'r': result.Append('\r'); break;
                        case '\\': result.Append('\\'); break;
                        case 'x':
                            if (i + 2 >= value.Length)
                                throw new ArgumentException("String ends before the escape sequence at position {0} is complete".Fmt(i - 1), "value");
                            int code;
                            if (!int.TryParse(value.Substring(i + 1, 2), NumberStyles.AllowHexSpecifier, null, out code))
                                throw new ArgumentException(@"Cannot parse hex escape sequence ""\x{0}"" at position {1}".Fmt(value.Substring(i + 1, 2), i - 1), "value");
                            result.Append((char) code);
                            i += 2;
                            break;
                        default:
                            throw new ArgumentException("Unrecognised escape sequence at position {0}: \\{1}".Fmt(i - 1, c), "value");
                    }
                }

                i++;
            }

            return result.ToString();
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
        public static string JoinString(this IEnumerable<string> values, string separator)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext())
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append(enumerator.Current);
            while (enumerator.MoveNext())
            {
                sb.Append(separator);
                sb.Append(enumerator.Current);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Joins all strings in the enumerable into a single string.
        /// <example>
        ///     <code>
        ///         var a = (new[] { 'Paris', 'London', 'Tokyo' }).Join();
        ///         // a contains "ParisLondonTokyo"
        ///     </code>
        /// </example>
        /// </summary>
        public static string JoinString(this IEnumerable<string> values)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var str in values)
                sb.Append(str);
            return sb.ToString();
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
        /// Formats the specified objects into the format string. The result is an enumerable collection
        /// which enumerates parts of the format string interspersed with the arguments as appropriate.
        /// </summary>
        public static IEnumerable<object> FmtEnumerable(this string formatString, params object[] args)
        {
            if (formatString == null) throw new ArgumentNullException("formatString");

            StringBuilder sb = new StringBuilder(formatString.Length);
            int i = 0;
            while (i < formatString.Length)
            {
                if (formatString[i] == '{')
                {
                    i++;
                    if (i >= formatString.Length)
                        throw new ArgumentException("Unexpected end of format string in the middle of a format specifier.");
                    if (formatString[i] == '{')
                    {
                        sb.Append('{');
                    }
                    else
                    {
                        // Only support single digit references
                        if (!(formatString[i] >= '0' && formatString[i] <= '9'))
                            throw new ArgumentException("Format specifier must contain a single digit after the opening curly bracket.");
                        int argNumber = (int) (formatString[i] - '0');
                        i++;
                        if (i >= formatString.Length)
                            throw new ArgumentException("Unexpected end of format string in the middle of a format specifier.");
                        if (formatString[i] != '}')
                            throw new ArgumentException("Format specifier did not end with a closing curly bracket immediately after the single digit group number.");
                        i++;
                        // Return the stuff we have!
                        if (argNumber >= args.Length)
                            throw new ArgumentException("Format specifier refers to argument index {{{0}}}, but only {1} argument(s) were supplied.".Fmt(argNumber, args.Length));
                        if (sb.Length > 0)
                            yield return sb.ToString();
                        yield return args[argNumber];
                        // Reset the buffer
                        sb = new StringBuilder(formatString.Length);
                    }
                }
                else if (formatString[i] == '}')
                {
                    i++;
                    if (i >= formatString.Length)
                        throw new ArgumentException("Unexpected end of format string with a single closing curly bracket.");
                    if (formatString[i] == '}')
                        sb.Append('}');
                    else
                        throw new ArgumentException("Unescaped closing curly bracket in format string which is not part of a valid format specifier.");
                }
                else
                {
                    sb.Append(formatString[i]);
                    i++;
                }
            }
            if (sb.Length > 0)
                yield return sb.ToString();
        }

        /// <summary>Word-wraps the current string to a specified width. Supports unix-style newlines and indented paragraphs.</summary>
        /// <remarks>
        /// <para>The supplied text will be split into "paragraphs" on the newline characters. Every paragraph will begin on a new line in the word-wrapped output, indented
        /// by the same number of spaces as in the input. All subsequent lines belonging to that paragraph will also be indented by the same amount.</para>
        /// <para>All multiple contiguous spaces will be replaced with a single space (except for the indentation).</para>
        /// </remarks>
        /// <param name="text">Text to be word-wrapped.</param>
        /// <param name="maxWidth">The maximum number of characters permitted on a single line, not counting the end-of-line terminator.</param>
        public static IEnumerable<string> WordWrap(this string text, int maxWidth)
        {
            return WordWrap(text, maxWidth, 0);
        }

        /// <summary>Word-wraps the current string to a specified width. Supports unix-style newlines and indented paragraphs.</summary>
        /// <remarks>
        /// <para>The supplied text will be split into "paragraphs" on the newline characters. Every paragraph will begin on a new line in the word-wrapped output, indented
        /// by the same number of spaces as in the input. All subsequent lines belonging to that paragraph will also be indented by the same amount.</para>
        /// <para>All multiple contiguous spaces will be replaced with a single space (except for the indentation).</para>
        /// </remarks>
        /// <param name="text">Text to be word-wrapped.</param>
        /// <param name="maxWidth">The maximum number of characters permitted on a single line, not counting the end-of-line terminator.</param>
        /// <param name="hangingIndent">The number of spaces to add to each line except the first of each paragraph, thus creating a hanging indentation.</param>
        public static IEnumerable<string> WordWrap(this string text, int maxWidth, int hangingIndent)
        {
            if (text == null || text == "")
                yield break;
            if (maxWidth < 1)
                throw new ArgumentOutOfRangeException("maxWidth", maxWidth, "maxWidth cannot be less than 1");
            if (hangingIndent < 0)
                throw new ArgumentOutOfRangeException("hangingIndent", hangingIndent, "hangingIndent cannot be negative.");

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
                    string indent = new string(' ', indentLen + hangingIndent);
                    string space = new string(' ', indentLen);

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

        /// <summary>Attempts to detect Unix-style and Mac-style line endings and converts them to Windows (\r\n).</summary>
        public static string UnifyLineEndings(this string input)
        {
            string[] lines = Regex.Split(input, @"\r\n|\r|\n");
            return lines.JoinString("\r\n");
        }

        /// <summary>
        /// Determines whether the specified URL starts with the specified URL path.
        /// For example, the URL "/directory/file" starts with "/directory" but not with "/dir".
        /// </summary>
        public static bool UrlStartsWith(this string url, string path)
        {
            return (url == path) || url.StartsWith(path + "/") || url.StartsWith(path + "?");
        }

        /// <summary>
        /// Same as <see cref="string.Substring(int)"/> but does not throw exceptions when the start index
        /// falls outside the boundaries of the string. Instead the result is truncated as appropriate.
        /// </summary>
        public static string SubstringSafe(this string source, int startIndex)
        {
            if (startIndex >= source.Length)
                return "";
            else if (startIndex < 0)
                return source;
            else
                return source.Substring(startIndex);
        }

        /// <summary>
        /// Same as <see cref="string.Substring(int, int)"/> but does not throw exceptions when the start index
        /// or length (or both) fall outside the boundaries of the string. Instead the result is truncated as appropriate.
        /// </summary>
        public static string SubstringSafe(this string source, int startIndex, int length)
        {
            if (startIndex < 0)
            {
                length += startIndex;
                startIndex = 0;
            }
            if (startIndex >= source.Length || length <= 0)
                return "";
            else if (startIndex + length > source.Length)
                return source.Substring(startIndex);
            else
                return source.Substring(startIndex, length);
        }
    }
}
