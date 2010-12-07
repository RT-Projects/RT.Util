using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.Consoles;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="String"/> type.
    /// </summary>
    public static class StringExtensions
    {
        private const string _charsBase64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private static int[] _invBase64Url; // inverse base-64-url lookup table

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
            if (input == null)
                throw new ArgumentNullException("input");
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        /// <summary>
        /// Contains the set of ASCII characters allowed in a URL.
        /// </summary>
        private static byte[] _urlAllowedBytes
        {
            get
            {
                if (_urlAllowedBytesCache == null)
                    _urlAllowedBytesCache = Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$-_.!*'(),/:;@");
                return _urlAllowedBytesCache;
            }
        }
        private static byte[] _urlAllowedBytesCache = null;

        /// <summary>
        /// Escapes all necessary characters in the specified string so as to make it usable safely in a URL.
        /// </summary>
        /// <param name="input">The string to apply URL escaping to.</param>
        /// <returns>The specified string with the necessary URL escaping applied.</returns>
        /// <seealso cref="UrlUnescape(string)"/>
        public static string UrlEscape(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            byte[] utf8 = input.ToUtf8();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in utf8)
                if (_urlAllowedBytes.Contains(b))
                    sb.Append((char) b);
                else
                    sb.AppendFormat("%{0:X2}", b);
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
            if (input == null)
                throw new ArgumentNullException("input");

            if (Regex.IsMatch(input, @"%[^0-9a-fA-F]|%.[^0-9a-fA-F]|%.?\z", RegexOptions.Singleline))
                throw new ArgumentException("The input string is not in valid URL-escaped format.", "input");

            if (input.Length < 3)
                return input.Replace('+', ' ');

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
        /// Contains the set of characters disallowed in file names across all filesystems supported by our software.
        /// </summary>
        private static char[] _filenameDisallowedCharacters
        {
            get
            {
                if (_filenameDisallowedCharactersCache == null)
                    _filenameDisallowedCharactersCache = @"\/:?*""<>|{}".ToCharArray();
                return _filenameDisallowedCharactersCache;
            }
        }
        private static char[] _filenameDisallowedCharactersCache = null;

        /// <summary>
        /// Escapes all characters in this string which cannot form part of a valid filename on at least one
        /// supported filesystem. The escaping is fully reversible (via <see cref="FilenameCharactersUnescape"/>),
        /// but does not treat characters at specific positions differently (e.g. the "." at the end of the name is not escaped,
        /// even though it will disappear on a Win32 system).
        /// </summary>
        public static string FilenameCharactersEscape(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            var result = new StringBuilder(input.Length + input.Length / 2);
            foreach (char c in input)
            {
                if (_filenameDisallowedCharacters.Contains(c))
                {
                    result.Append('{');
                    foreach (var bt in Encoding.UTF8.GetBytes(c.ToString()))
                        result.AppendFormat("{0:X2}", bt);
                    result.Append('}');
                }
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>
        /// Reverses the transformation done by <see cref="FilenameCharactersEscape"/>. This routine will also
        /// work on filenames that cannot have been generated by the above escape procedure; any "invalid" escapes
        /// will be preserved as-is.
        /// </summary>
        public static string FilenameCharactersUnescape(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            var result = new StringBuilder(input.Length);
            byte[] decode = new byte[4];

            int offset = 0;
            while (offset < input.Length)
            {
                if (input[offset] == '{')
                {
                    int decodeCount = 0;
                    int startOffset = offset; // set to -1 if decoded successfully
                    offset++;
                    while (offset < input.Length)
                    {
                        char c = char.ToUpperInvariant(input[offset]);
                        if (c == '}')
                        {
                            offset++;
                            if (decodeCount > 0)
                            {
                                try
                                {
                                    result.Append(Encoding.UTF8.GetString(decode, 0, decodeCount));
                                    startOffset = -1; // successfully decoded this escape
                                }
                                catch (ArgumentException) { } // invalid escape
                            }
                            break;
                        }
                        else if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F')
                        {
                            offset++;
                            if (offset >= input.Length || decodeCount == 4)
                                break; // input ended abruptly or the escape is now too long to be valid
                            char c2 = char.ToUpperInvariant(input[offset]);
                            if (c2 >= '0' && c2 <= '9' || c2 >= 'A' && c2 <= 'F')
                            {
                                offset++;
                                decode[decodeCount] = (byte) ((c < 'A' ? c - '0' : c - '7') * 16 | (c2 < 'A' ? c2 - '0' : c2 - '7'));
                                decodeCount++;
                            }
                            else
                                break; // invalid second char
                        }
                        else
                        {
                            // invalid char encountered
                            break;
                        }
                    }
                    if (startOffset != -1)
                        result.Append(input, startOffset, offset - startOffset);
                }
                else
                {
                    result.Append(input[offset]);
                    offset++;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Converts the specified string to UTF-8.
        /// </summary>
        /// <param name="input">String to convert to UTF-8.</param>
        /// <returns>The specified string, converted to a byte-array containing the UTF-8 encoding of the string.</returns>
        public static byte[] ToUtf8(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Encoding.UTF8.GetBytes(input);
        }

        /// <summary>
        /// Converts the specified string to UTF-16.
        /// </summary>
        /// <param name="input">String to convert to UTF-16.</param>
        /// <returns>The specified string, converted to a byte-array containing the UTF-16 encoding of the string.</returns>
        public static byte[] ToUtf16(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Encoding.Unicode.GetBytes(input);
        }

        /// <summary>
        /// Converts the specified string to UTF-16 (Big Endian).
        /// </summary>
        /// <param name="input">String to convert to UTF-16 (Big Endian).</param>
        /// <returns>The specified string, converted to a byte-array containing the UTF-16 (Big Endian) encoding of the string.</returns>
        public static byte[] ToUtf16BE(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Encoding.BigEndianUnicode.GetBytes(input);
        }

        /// <summary>
        /// Converts the specified raw UTF-8 data to a string.
        /// </summary>
        /// <param name="input">Data to interpret as UTF-8 text.</param>
        /// <returns>A string containing the characters represented by the UTF-8-encoded input.</returns>
        public static string FromUtf8(this byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Encoding.UTF8.GetString(input);
        }

        /// <summary>
        /// Determines the length of the UTF-8 encoding of the specified string.
        /// </summary>
        /// <param name="input">String to determined UTF-8 length of.</param>
        /// <returns>The length of the string in bytes when encoded as UTF-8.</returns>
        public static int Utf8Length(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return Encoding.UTF8.GetByteCount(input);
        }

        /// <summary>
        /// Returns a JavaScript-compatible representation of the string in double-quotes with the appropriate characters escaped.
        /// </summary>
        /// <param name="input">String to escape.</param>
        /// <returns>JavaScript-compatible representation of the input string.</returns>
        public static string JsEscape(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return "\"" + input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("</", "<\"+\"/") + "\"";
        }

        /// <summary>
        /// Returns an SQL-compatible representation of the string in single-quotes with the appropriate characters escaped.
        /// </summary>
        /// <param name="input">String to escape.</param>
        /// <returns>SQL-compatible representation of the input string.</returns>
        public static string SqlEscape(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            return "'" + input.Replace("'", "''") + "'";
        }

        /// <summary>Encodes this byte array to base-64-url format, which is safe for use in URLs and
        /// does not contain the unnecessary padding when the number of bytes is not divisible by 3.</summary>
        /// <seealso cref="Base64UrlDecode"/>
        public static string Base64UrlEncode(this byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

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

        /// <summary>Decodes this string from base-64-url encoding, which is safe for use in URLs and does not
        /// contain the unnecessary padding whde the number of bytes is not divisible by 3, into a byte array.</summary>
        /// <seealso cref="Base64UrlEncode"/>
        public static byte[] Base64UrlDecode(this string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (input.Any(ch => !_charsBase64Url.Contains(ch)))
                throw new ArgumentException("The input string to Base64UrlDecode is not a valid base-64-url encoded string.");

            if (_invBase64Url == null)
            {
                // Initialise the base-64-url inverse lookup table
                _invBase64Url = new int[256];
                for (int j = 0; j < _invBase64Url.Length; j++)
                    _invBase64Url[j] = -1;
                for (int j = 0; j < _charsBase64Url.Length; j++)
                    _invBase64Url[(int) _charsBase64Url[j]] = j;
            }

            // See how many bytes are encoded at the end of the string
            int padding = input.Length % 4;
            if (padding == 1)
                throw new ArgumentException("The input string to Base64UrlDecode is not a valid base-64-url encoded string.");
            if (padding > 0)
                padding--;

            byte[] result = new byte[(input.Length / 4) * 3 + padding];
            int resultIndex = 0, inputIndex = 0;

            while (inputIndex < input.Length)
            {
                if (input.Length - inputIndex >= 4)
                {
                    // 00000011 11112222 22333333
                    uint v0 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v1 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v2 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v3 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    result[resultIndex++] = (byte) (v0 << 2 | v1 >> 4);
                    result[resultIndex++] = (byte) ((v1 & 15) << 4 | v2 >> 2);
                    result[resultIndex++] = (byte) ((v2 & 3) << 6 | v3);
                }
                else if (input.Length - inputIndex == 3)
                {
                    // 00000011 11112222 [22------]
                    uint v0 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v1 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v2 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    result[resultIndex++] = (byte) (v0 << 2 | v1 >> 4);
                    result[resultIndex++] = (byte) ((v1 & 15) << 4 | v2 >> 2);
                }
                else if (input.Length - inputIndex == 2)
                {
                    // 00000011 [1111----]
                    uint v0 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    uint v1 = checked((uint) _invBase64Url[input[inputIndex++]]);
                    result[resultIndex++] = (byte) (v0 << 2 | v1 >> 4);
                }
                else
                    throw new InternalErrorException("Internal error in Base64UrlDecode");
            }

            return result;
        }

        /// <summary>
        /// Escapes all characters in this string whose code is less than 32 using C/C#-compatible backslash escapes.
        /// </summary>
        /// <seealso cref="CLiteralUnescape"/>
        public static string CLiteralEscape(this string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

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
                    case '"': result.Append(@"\"""); break;
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
        /// <seealso cref="CLiteralEscape"/>
        public static string CLiteralUnescape(this string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

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
                        case '"': result.Append('"'); break;
                        case 'x':
                            // See how many characters are hex digits
                            var len = 0;
                            i++;
                            while (len <= 4 && i + len < value.Length && ((value[i + len] >= '0' && value[i + len] <= '9') || (value[i + len] >= 'a' && value[i + len] <= 'f') || (value[i + len] >= 'A' && value[i + len] <= 'F')))
                                len++;
                            if (len == 0)
                                throw new ArgumentException(@"Invalid hex escape sequence ""\x"" at position {0}".Fmt(i - 2), "value");
                            int code = int.Parse(value.Substring(i, len), NumberStyles.AllowHexSpecifier);
                            result.Append((char) code);
                            i += len - 1;
                            break;
                        default:
                            throw new ArgumentException("Unrecognised escape sequence at position {0}: \\{1}".Fmt(i - 1, c), "value");
                    }
                }

                i++;
            }

            return result.ToString();
        }

        /// <summary>Returns the specified collection, but with leading and trailing empty strings and nulls removed.</summary>
        public static IEnumerable<string> Trim(this IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            var arr = values.ToArray();
            var begin = 0;
            while (begin < arr.Length && string.IsNullOrEmpty(arr[begin]))
                begin++;
            if (begin == arr.Length)
                return new string[0];
            var end = arr.Length - 1;
            while (end >= 0 && string.IsNullOrEmpty(arr[end]))
                end--;
            return arr.Skip(begin).Take(end - begin + 1);
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
            if (formatString == null)
                throw new ArgumentNullException("formatString");

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
        /// <param name="hangingIndent">The number of spaces to add to each line except the first of each paragraph, thus creating a hanging indentation.</param>
        public static IEnumerable<string> WordWrap(this string text, int maxWidth, int hangingIndent = 0)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (maxWidth < 1)
                throw new ArgumentOutOfRangeException("maxWidth", maxWidth, "maxWidth cannot be less than 1");
            if (hangingIndent < 0)
                throw new ArgumentOutOfRangeException("hangingIndent", hangingIndent, "hangingIndent cannot be negative.");
            if (text == null || text == "")
                return new string[0];

            return wordWrap(text, maxWidth, hangingIndent);
        }

        private static IEnumerable<string> wordWrap(this string text, int maxWidth, int hangingIndent)
        {
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
                                // Leave part of the word on the current line if at least 2 chars fit
                                if (curLine.Length + space.Length + 2 <= maxWidth || curLine.Length == 0)
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
            if (input == null)
                throw new ArgumentNullException("input");
            string[] lines = Regex.Split(input, @"\r\n|\r|\n");
            return lines.JoinString("\r\n");
        }

        /// <summary>
        /// Determines whether the specified URL starts with the specified URL path.
        /// For example, the URL "/directory/file" starts with "/directory" but not with "/dir".
        /// </summary>
        public static bool UrlStartsWith(this string url, string path)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            return (url == path) || url.StartsWith(path + "/") || url.StartsWith(path + "?");
        }

        /// <summary>
        /// Same as <see cref="string.Substring(int)"/> but does not throw exceptions when the start index
        /// falls outside the boundaries of the string. Instead the result is truncated as appropriate.
        /// </summary>
        public static string SubstringSafe(this string source, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (startIndex >= source.Length)
                return "";
            else if (startIndex < 0)
                return source;
            else
                return source.Substring(startIndex);
        }

        /// <summary>
        /// Same as <see cref="string.Substring(int, int)"/> but does not throw exceptions when the start index
        ///  or length (or both) fall outside the boundaries of the string. Instead the result is truncated as appropriate.
        /// </summary>
        public static string SubstringSafe(this string source, int startIndex, int length)
        {
            if (source == null)
                throw new ArgumentNullException("source");
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

        /// <summary>
        /// Returns true if and only if this string is equal to the other string under the
        /// invariant-culture case-insensitive comparison.
        /// </summary>
        public static bool EqualsNoCase(this string strthis, string str)
        {
            if (strthis == null)
                throw new ArgumentNullException("strthis");
            return strthis.Equals(str, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true if and only if this string ends with the specified character.</summary>
        /// <seealso cref="StartsWith"/>
        public static bool EndsWith(this string str, char? ch)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (ch == null)
                return true;
            return str != null && str.Length > 0 && str[str.Length - 1] == ch.Value;
        }

        /// <summary>Returns true if and only if this string starts with the specified character.</summary>
        /// <seealso cref="EndsWith"/>
        public static bool StartsWith(this string str, char? ch)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (ch == null)
                return true;
            return str != null && str.Length > 0 && str[0] == ch.Value;
        }

        /// <summary>Colours the specified string in the specified console colour.</summary>
        /// <param name="str">The string to colour.</param>
        /// <param name="color">The colour to colour the string in.</param>
        /// <returns>A potentially colourful string.</returns>
        public static ConsoleColoredString Color(this string str, ConsoleColor color)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            return new ConsoleColoredString(str, color);
        }

        /// <summary>
        /// Reconstructs a byte array from its hexadecimal representation ("hexdump").
        /// </summary>
        public static byte[] FromHex(this string input)
        {
            if (input == null || (input.Length % 2) != 0)
                throw new ArgumentOutOfRangeException("The input string must be non-null and of even length.");
            byte[] result = new byte[input.Length / 2];
            var j = 0;
            for (int i = 0; i < result.Length; i++)
            {
                // Note: This series of 'if's is actually faster than ((input[j] & 7) + ((input[j] / 56) << 3) + input[j] / 58), although it gives the same result
                int upperNibble, lowerNibble;
                if (input[j] >= '0' && input[j] <= '9')
                    upperNibble = input[j] - '0';
                else if (input[j] >= 'a' && input[j] <= 'f')
                    upperNibble = input[j] - 'a' + 10;
                else if (input[j] >= 'A' && input[j] <= 'F')
                    upperNibble = input[j] - 'A' + 10;
                else
                    throw new InvalidOperationException("The character '{0}' is not a valid hexadecimal digit.".Fmt(input[j]));
                j++;
                if (input[j] >= '0' && input[j] <= '9')
                    lowerNibble = input[j] - '0';
                else if (input[j] >= 'a' && input[j] <= 'f')
                    lowerNibble = input[j] - 'a' + 10;
                else if (input[j] >= 'A' && input[j] <= 'F')
                    lowerNibble = input[j] - 'A' + 10;
                else
                    throw new InvalidOperationException("The character '{0}' is not a valid hexadecimal digit.".Fmt(input[j]));
                j++;
                result[i] = (byte) ((upperNibble << 4) + lowerNibble);
            }
            return result;
        }

        /// <summary>Inserts spaces at the beginning of every line contained within the specified string.</summary>
        /// <param name="str">String to add indentation to.</param>
        /// <param name="by">Number of spaces to add.</param>
        /// <param name="indentFirstLine">If true (default), all lines are indented; otherwise, all lines except the first.</param>
        /// <returns>The indented string.</returns>
        public static string Indent(this string str, int by, bool indentFirstLine = true)
        {
            if (indentFirstLine)
                return Regex.Replace(str, "^", new string(' ', by), RegexOptions.Multiline);
            return Regex.Replace(str, "(?<=\n)", new string(' ', by));
        }

        /// <summary>Splits a string into chunks of equal size. The last chunk may be smaller than chunkSize, but all chunks, if any, will contain at least 1 character.</summary>
        public static IEnumerable<string> Split(this string str, int chunkSize)
        {
            if (chunkSize <= 0) throw new ArgumentException("chunkSize must be greater than zero");
            for (int offset = 0; offset < str.Length; offset += chunkSize)
                yield return str.Substring(offset, Math.Min(chunkSize, str.Length - offset));
        }
    }
}
