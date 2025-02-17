﻿using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

#if EXPORT_UTIL
namespace RT.Util.ExtensionMethods;
#else
namespace RT.Internal;
#endif

/// <summary>Provides extension methods on the <see cref="string"/> type.</summary>
#if EXPORT_UTIL
public
#endif
static class StringExtensions
{
    /// <summary>Contains the set of characters that are used in base64-url encoding.</summary>
    public const string CharsBase64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    private static int[] _invBase64Url; // inverse base-64-url lookup table

    /// <summary>
    ///     Concatenates the specified number of repetitions of the current string.</summary>
    /// <param name="input">
    ///     The string to be repeated.</param>
    /// <param name="numTimes">
    ///     The number of times to repeat the string.</param>
    /// <returns>
    ///     A concatenated string containing the original string the specified number of times.</returns>
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
    ///     Escapes all necessary characters in the specified string so as to make it usable safely in an HTML or XML context.</summary>
    /// <param name="input">
    ///     The string to apply HTML or XML escaping to.</param>
    /// <param name="leaveSingleQuotesAlone">
    ///     If <c>true</c>, does not escape single quotes (<c>'</c>, U+0027).</param>
    /// <param name="leaveDoubleQuotesAlone">
    ///     If <c>true</c>, does not escape single quotes (<c>"</c>, U+0022).</param>
    /// <returns>
    ///     The specified string with the necessary HTML or XML escaping applied.</returns>
    public static string HtmlEscape(this string input, bool leaveSingleQuotesAlone = false, bool leaveDoubleQuotesAlone = false)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        var result = input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        if (!leaveSingleQuotesAlone)
            result = result.Replace("'", "&#39;");
        if (!leaveDoubleQuotesAlone)
            result = result.Replace("\"", "&quot;");
        return result;
    }

    /// <summary>Contains the set of ASCII characters allowed in a URL.</summary>
    private static byte[] _urlAllowedBytes
    {
        get
        {
            if (_urlAllowedBytesCache == null)
                _urlAllowedBytesCache = Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$-_.!*(),/:;@");
            return _urlAllowedBytesCache;
        }
    }
    private static byte[] _urlAllowedBytesCache = null;

    /// <summary>
    ///     Escapes all necessary characters in the specified string so as to make it usable safely in a URL.</summary>
    /// <param name="input">
    ///     The string to apply URL escaping to.</param>
    /// <returns>
    ///     The specified string with the necessary URL escaping applied.</returns>
    /// <seealso cref="UrlUnescape(string)"/>
    public static string UrlEscape(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        byte[] utf8 = input.ToUtf8();
        var sb = new StringBuilder();
        foreach (byte b in utf8)
            if (_urlAllowedBytes.Contains(b))
                sb.Append((char) b);
            else
                sb.AppendFormat("%{0:X2}", b);
        return sb.ToString();
    }

    /// <summary>
    ///     Reverses the escaping performed by <see cref="UrlEscape"/> by decoding hexadecimal URL escape sequences into their
    ///     original characters.</summary>
    /// <param name="input">
    ///     String containing URL escape sequences to be decoded.</param>
    /// <returns>
    ///     The specified string with all URL escape sequences decoded.</returns>
    /// <seealso cref="UrlEscape(string)"/>
    public static string UrlUnescape(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.Length < 3)
            return input.Replace('+', ' ');

        // The result is never going to be longer than the input string
        byte[] inputBuffer = input.ToUtf8();
        byte[] buffer = new byte[inputBuffer.Length];

        var bufIx = 0;
        var inpIx = 0;
        while (inpIx < inputBuffer.Length)
        {
            if (inpIx <= inputBuffer.Length - 6 && inputBuffer[inpIx] == '%' && inputBuffer[inpIx + 1] == 'u' && int.TryParse(inputBuffer.Subarray(inpIx + 2, 4).FromUtf8(), NumberStyles.AllowHexSpecifier, null, out var i))
            {
                bufIx += Encoding.UTF8.GetBytes(char.ConvertFromUtf32(i), 0, 1, buffer, bufIx);
                inpIx += 6;
            }
            else if (inpIx <= inputBuffer.Length - 3 && inputBuffer[inpIx] == '%' && int.TryParse(inputBuffer.Subarray(inpIx + 1, 2).FromUtf8(), NumberStyles.AllowHexSpecifier, null, out i))
            {
                buffer[bufIx] = (byte) i;
                bufIx++;
                inpIx += 3;
            }
            else if (inputBuffer[inpIx] != '%')
            {
                buffer[bufIx] = inputBuffer[inpIx] == '+' ? (byte) ' ' : inputBuffer[inpIx];
                bufIx++;
                inpIx++;
            }
            else
                throw new ArgumentException("The input string is not in valid URL-escaped format.", nameof(input));
        }
        return Encoding.UTF8.GetString(buffer, 0, bufIx);
    }

    /// <summary>
    ///     Escapes all characters in this string which cannot form part of a valid filename on at least one supported
    ///     filesystem. The escaping is fully reversible (via <see cref="FilenameCharactersUnescape"/>), but does not treat
    ///     characters at specific positions differently (e.g. the "." at the end of the name is not escaped, even though it
    ///     will disappear on a Win32 system).</summary>
    public static string FilenameCharactersEscape(this string input, bool includeNonAscii = false)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var result = new StringBuilder(input.Length + input.Length / 2);
        foreach (char c in input)
        {
            var escape = false;
            switch (c)
            {
                case '\\':
                case '/':
                case '|':
                case ':':
                case '?':
                case '*':
                case '"':
                case '<':
                case '>':
                case '{':
                case '}':
                case (char) 0x7F:
                    escape = true;
                    break;
            }
            if (c <= 0x1F || (c >= 128 && includeNonAscii))
                escape = true;
            if (escape)
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
    ///     Reverses the transformation done by <see cref="FilenameCharactersEscape"/>. This routine will also work on
    ///     filenames that cannot have been generated by the above escape procedure; any "invalid" escapes will be preserved
    ///     as-is.</summary>
    public static string FilenameCharactersUnescape(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

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
    ///     Converts the specified string to UTF-8.</summary>
    /// <param name="input">
    ///     String to convert to UTF-8.</param>
    /// <returns>
    ///     The specified string, converted to a byte-array containing the UTF-8 encoding of the string.</returns>
    public static byte[] ToUtf8(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.UTF8.GetBytes(input);
    }

    /// <summary>
    ///     Converts the specified string to UTF-16.</summary>
    /// <param name="input">
    ///     String to convert to UTF-16.</param>
    /// <returns>
    ///     The specified string, converted to a byte-array containing the UTF-16 encoding of the string.</returns>
    public static byte[] ToUtf16(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.Unicode.GetBytes(input);
    }

    /// <summary>
    ///     Converts the specified string to UTF-16 (Big Endian).</summary>
    /// <param name="input">
    ///     String to convert to UTF-16 (Big Endian).</param>
    /// <returns>
    ///     The specified string, converted to a byte-array containing the UTF-16 (Big Endian) encoding of the string.</returns>
    public static byte[] ToUtf16BE(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.BigEndianUnicode.GetBytes(input);
    }

    /// <summary>
    ///     Converts the specified raw UTF-8 data to a string.</summary>
    /// <param name="input">
    ///     Data to interpret as UTF-8 text.</param>
    /// <param name="removeBom">
    ///     <c>true</c> to remove the first character if it is a UTF-8 BOM.</param>
    /// <returns>
    ///     A string containing the characters represented by the UTF-8-encoded input.</returns>
    public static string FromUtf8(this byte[] input, bool removeBom = false)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        var result = Encoding.UTF8.GetString(input);
        if (removeBom && result[0] == '\ufeff')
            return result.Substring(1);
        return result;
    }

    /// <summary>
    ///     Converts the specified raw UTF-16 (little-endian) data to a string.</summary>
    /// <param name="input">
    ///     Data to interpret as UTF-16 text.</param>
    /// <returns>
    ///     A string containing the characters represented by the UTF-16-encoded input.</returns>
    public static string FromUtf16(this byte[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.Unicode.GetString(input);
    }

    /// <summary>
    ///     Converts the specified raw UTF-16 (big-endian) data to a string.</summary>
    /// <param name="input">
    ///     Data to interpret as UTF-16BE text.</param>
    /// <returns>
    ///     A string containing the characters represented by the UTF-16BE-encoded input.</returns>
    public static string FromUtf16BE(this byte[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.BigEndianUnicode.GetString(input);
    }

    /// <summary>
    ///     Determines the length of the UTF-8 encoding of the specified string.</summary>
    /// <param name="input">
    ///     String to determined UTF-8 length of.</param>
    /// <returns>
    ///     The length of the string in bytes when encoded as UTF-8.</returns>
    public static int Utf8Length(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.UTF8.GetByteCount(input);
    }

    /// <summary>
    ///     Determines the length of the UTF-16 encoding of the specified string.</summary>
    /// <param name="input">
    ///     String to determined UTF-16 length of.</param>
    /// <returns>
    ///     The length of the string in bytes when encoded as UTF-16.</returns>
    public static int Utf16Length(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return Encoding.Unicode.GetByteCount(input);
    }

    /// <summary>
    ///     Returns a JavaScript- or JSON-compatible representation of the string with the appropriate characters escaped.
    ///     Returns "null" if the input is null.</summary>
    /// <param name="input">
    ///     String to escape.</param>
    /// <param name="quotes">
    ///     Specifies what type of quotes to put around the result, if any.</param>
    /// <returns>
    ///     JavaScript- or JSON-compatible representation of the input string, or the "null" keyword if the input is null.</returns>
    public static string JsEscapeNull(this string input, JsQuotes quotes = JsQuotes.Double)
    {
        return input == null ? "null" : input.JsEscape(quotes);
    }

    /// <summary>
    ///     Returns a JavaScript- or JSON-compatible representation of the string with the appropriate characters escaped.</summary>
    /// <param name="input">
    ///     String to escape.</param>
    /// <param name="quotes">
    ///     Specifies what type of quotes to put around the result, if any.</param>
    /// <returns>
    ///     JavaScript- or JSON-compatible representation of the input string.</returns>
    public static string JsEscape(this string input, JsQuotes quotes = JsQuotes.Double)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var sb = new StringBuilder();
        sb.AppendJsEscaped(input, quotes);
        return sb.ToString();
    }

    /// <summary>
    ///     Appends a JavaScript- or JSON-compatible representation of the string with the appropriate characters escaped into
    ///     the specified StringBuilder.</summary>
    /// <param name="sb">
    ///     The StringBuilder to add the result to.</param>
    /// <param name="input">
    ///     String to escape.</param>
    /// <param name="quotes">
    ///     Specifies what type of quotes to put around the result, if any.</param>
    public static void AppendJsEscaped(this StringBuilder sb, string input, JsQuotes quotes = JsQuotes.Double)
    {
        if (sb == null)
            throw new ArgumentNullException(nameof(sb));
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (quotes != JsQuotes.None)
            sb.Append(quotes == JsQuotes.Double ? '"' : '\'');
        foreach (var c in input)
        {
            switch (c)
            {
                case '"': if (quotes == JsQuotes.Single) sb.Append(c); else sb.Append("\\\""); break;
                case '\'': if (quotes == JsQuotes.Double) sb.Append(c); else if (quotes == JsQuotes.Single) sb.Append("\\'"); else sb.Append("\\u0027"); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c <= 31)
                    {
                        sb.Append("\\u");
                        sb.Append(((int) c).ToString("X4"));
                    }
                    else
                        sb.Append(c);
                    break;
            }
        }
        if (quotes != JsQuotes.None)
            sb.Append(quotes == JsQuotes.Double ? '"' : '\'');
    }

    /// <summary>
    ///     Returns an SQL-compatible representation of the string in single-quotes with the appropriate characters escaped.</summary>
    /// <param name="input">
    ///     String to escape.</param>
    /// <returns>
    ///     SQL-compatible representation of the input string.</returns>
    public static string SqlEscape(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        return "'" + input.Replace("'", "''") + "'";
    }

    /// <summary>
    ///     Encodes this byte array to base-64-url format, which is safe for use in URLs and does not contain the unnecessary
    ///     padding when the number of bytes is not divisible by 3.</summary>
    /// <seealso cref="Base64UrlDecode"/>
    public static string Base64UrlEncode(this byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        StringBuilder result = new StringBuilder();
        int i = 0;

        while (i < bytes.Length)
        {
            if (bytes.Length - i >= 3)
            {
                // 000000 001111 111122 222222
                result.Append(CharsBase64Url[bytes[i] >> 2]);
                result.Append(CharsBase64Url[(bytes[i] & 3) << 4 | bytes[i + 1] >> 4]);
                result.Append(CharsBase64Url[(bytes[i + 1] & 15) << 2 | bytes[i + 2] >> 6]);
                result.Append(CharsBase64Url[bytes[i + 2] & 63]);
                i += 3;
            }
            else if (bytes.Length - i == 2)
            {
                // 000000 001111 1111--
                result.Append(CharsBase64Url[bytes[i] >> 2]);
                result.Append(CharsBase64Url[(bytes[i] & 3) << 4 | bytes[i + 1] >> 4]);
                result.Append(CharsBase64Url[(bytes[i + 1] & 15) << 2]);
                i += 2;
            }
            else /* if (bytes.Length - i == 1) -- always true here given the while condition */
            {
                // 000000 00----
                result.Append(CharsBase64Url[bytes[i] >> 2]);
                result.Append(CharsBase64Url[(bytes[i] & 3) << 4]);
                i += 1;
            }
        }

        return result.ToString();
    }

    /// <summary>
    ///     Decodes this string from base-64-url encoding, which is safe for use in URLs and does not contain the unnecessary
    ///     padding when the number of bytes is not divisible by 3, into a byte array.</summary>
    /// <seealso cref="Base64UrlEncode"/>
    public static byte[] Base64UrlDecode(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (input.Any(ch => !CharsBase64Url.Contains(ch)))
            throw new ArgumentException("The input string to Base64UrlDecode is not a valid base-64-url encoded string.", nameof(input));

        if (_invBase64Url == null)
        {
            // Initialise the base-64-url inverse lookup table
            _invBase64Url = new int[256];
            for (int j = 0; j < _invBase64Url.Length; j++)
                _invBase64Url[j] = -1;
            for (int j = 0; j < CharsBase64Url.Length; j++)
                _invBase64Url[CharsBase64Url[j]] = j;
        }

        // See how many bytes are encoded at the end of the string
        int padding = input.Length % 4;
        if (padding == 1)
            throw new InvalidOperationException("The input string to Base64UrlDecode is not a valid base-64-url encoded string.");
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
                throw new Exception("Internal error in Base64UrlDecode");
        }

        return result;
    }

    /// <summary>
    ///     Escapes all characters in this string whose code is less than 32 or form invalid UTF-16 using C/C#-compatible
    ///     backslash escapes.</summary>
    /// <seealso cref="CLiteralUnescape"/>
    public static string CLiteralEscape(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

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
                    if (c >= 0xD800 && c < 0xDC00)
                    {
                        if (i == value.Length - 1) // string ends on a broken surrogate pair
                            result.AppendFormat(@"\u{0:X4}", (int) c);
                        else
                        {
                            char c2 = value[i + 1];
                            if (c2 >= 0xDC00 && c2 <= 0xDFFF)
                            {
                                // nothing wrong with this surrogate pair
                                i++;
                                result.Append(c);
                                result.Append(c2);
                            }
                            else // first half of a surrogate pair is not followed by a second half
                                result.AppendFormat(@"\u{0:X4}", (int) c);
                        }
                    }
                    else if (c >= 0xDC00 && c <= 0xDFFF) // the second half of a broken surrogate pair
                        result.AppendFormat(@"\u{0:X4}", (int) c);
                    else if (c >= ' ')
                        result.Append(c);
                    else // the character is in the 0..31 range
                        result.AppendFormat(@"\u{0:X4}", (int) c);
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    ///     Reverses the escaping done by <see cref="CLiteralEscape"/>. Note that unescaping is not fully C/C#-compatible in
    ///     the sense that not all strings that are valid string literals in C/C# can be correctly unescaped by this
    ///     procedure.</summary>
    /// <seealso cref="CLiteralEscape"/>
    public static string CLiteralUnescape(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

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
                    throw new ArgumentException($"String ends before the escape sequence at position {i} is complete", "value");
                i++;
                c = value[i];
                int code;
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
                            throw new ArgumentException($@"Invalid hex escape sequence ""\x"" at position {i - 2}", "value");
                        code = int.Parse(value.Substring(i, len), NumberStyles.AllowHexSpecifier);
                        result.Append((char) code);
                        i += len - 1;
                        break;
                    case 'u':
                        if (i + 4 >= value.Length)
                            throw new ArgumentException($@"Invalid hex escape sequence ""\u"" at position {i}", "value");
                        i++;
                        code = int.Parse(value.Substring(i, 4), NumberStyles.AllowHexSpecifier);
                        result.Append((char) code);
                        i += 3;
                        break;
                    default:
                        throw new ArgumentException($"Unrecognised escape sequence at position {i - 1}: \\{c}", "value");
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
            throw new ArgumentNullException(nameof(values));
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
    ///     Word-wraps the current string to a specified width. Supports UNIX-style newlines and indented paragraphs.</summary>
    /// <remarks>
    ///     <para>
    ///         The supplied text will be split into "paragraphs" at the newline characters. Every paragraph will begin on a
    ///         new line in the word-wrapped output, indented by the same number of spaces as in the input. All subsequent
    ///         lines belonging to that paragraph will also be indented by the same amount.</para>
    ///     <para>
    ///         All multiple contiguous spaces will be replaced with a single space (except for the indentation).</para></remarks>
    /// <param name="text">
    ///     Text to be word-wrapped.</param>
    /// <param name="maxWidth">
    ///     The maximum number of characters permitted on a single line, not counting the end-of-line terminator.</param>
    /// <param name="hangingIndent">
    ///     The number of spaces to add to each line except the first of each paragraph, thus creating a hanging indentation.</param>
    public static IEnumerable<string> WordWrap(this string text, int maxWidth, int hangingIndent = 0)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        if (maxWidth < 1)
            throw new ArgumentOutOfRangeException(nameof(maxWidth), maxWidth, "maxWidth cannot be less than 1");
        if (hangingIndent < 0)
            throw new ArgumentOutOfRangeException(nameof(hangingIndent), hangingIndent, "hangingIndent cannot be negative.");
        if (text == null || text.Length == 0)
            return Enumerable.Empty<string>();

        return wordWrap(
            text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None),
            maxWidth,
            hangingIndent,
            (txt, substrIndex) => txt.Substring(substrIndex).Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries),
            str => str.Length,
            txt =>
            {
                // Count the number of spaces at the start of the paragraph
                int indentLen = 0;
                while (indentLen < txt.Length && txt[indentLen] == ' ')
                    indentLen++;
                return indentLen;
            },
            num => new string(' ', num),
            () => new StringBuilder(),
            sb => sb.Length,
            (sb, str) => { sb.Append(str); },
            sb => sb.ToString(),
            (str, start, length) => length == null ? str.Substring(start) : str.Substring(start, length.Value),
            (str1, str2) => str1 + str2);
    }

    internal static IEnumerable<T> wordWrap<T, TBuilder>(IEnumerable<T> paragraphs, int maxWidth, int hangingIndent, Func<T, int, IEnumerable<T>> splitSubstringIntoWords,
        Func<T, int> getLength, Func<T, int> getIndent, Func<int, T> spaces, Func<TBuilder> getBuilder, Func<TBuilder, int> getTotalLength, Action<TBuilder, T> add,
        Func<TBuilder, T> getString, Func<T, int, int?, T> substring, Func<T, T, T> concat)
    {
        foreach (var paragraph in paragraphs)
        {
            var indentLen = getIndent(paragraph);
            var indent = spaces(indentLen + hangingIndent);
            var space = spaces(indentLen);
            var numSpaces = indentLen;
            var curLine = getBuilder();

            // Split into words
            foreach (var wordForeach in splitSubstringIntoWords(paragraph, indentLen))
            {
                var word = wordForeach;
                var curLineLength = getTotalLength(curLine);

                if (curLineLength + numSpaces + getLength(word) > maxWidth)
                {
                    // Need to wrap
                    if (getLength(word) > maxWidth)
                    {
                        // This is a very long word
                        // Leave part of the word on the current line if at least 2 chars fit
                        if (curLineLength + numSpaces + 2 <= maxWidth || getTotalLength(curLine) == 0)
                        {
                            int length = maxWidth - getTotalLength(curLine) - numSpaces;
                            add(curLine, space);
                            add(curLine, substring(word, 0, length));
                            word = substring(word, length, null);
                        }
                        // Commit the current line
                        yield return getString(curLine);

                        // Now append full lines' worth of text until we're left with less than a full line
                        while (indentLen + getLength(word) > maxWidth)
                        {
                            yield return concat(indent, substring(word, 0, maxWidth - indentLen));
                            word = substring(word, maxWidth - indentLen, null);
                        }

                        // Start a new line with whatever is left
                        curLine = getBuilder();
                        add(curLine, indent);
                        add(curLine, word);
                    }
                    else
                    {
                        // This word is not very long and it doesn't fit so just wrap it to the next line
                        yield return getString(curLine);

                        // Start a new line
                        curLine = getBuilder();
                        add(curLine, indent);
                        add(curLine, word);
                    }
                }
                else
                {
                    // No need to wrap yet
                    add(curLine, space);
                    add(curLine, word);
                }

                if (numSpaces != 1)
                {
                    space = spaces(1);
                    numSpaces = 1;
                }
            }

            yield return getString(curLine);
        }
    }

    /// <summary>Attempts to detect Unix-style and Mac-style line endings and converts them to Windows (\r\n).</summary>
    public static string UnifyLineEndings(this string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        string[] lines = Regex.Split(input, @"\r\n|\r|\n");
        return string.Join("\r\n", lines);
    }

    /// <summary>
    ///     Determines whether the specified URL starts with the specified URL path. For example, the URL "/directory/file"
    ///     starts with "/directory" but not with "/dir".</summary>
    public static bool UrlStartsWith(this string url, string path)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url));
        return (url == path) || url.StartsWith(path + "/") || url.StartsWith(path + "?");
    }

    /// <summary>
    ///     Same as <see cref="string.Substring(int)"/> but does not throw exceptions when the start index falls outside the
    ///     boundaries of the string. Instead the result is truncated as appropriate.</summary>
    public static string SubstringSafe(this string source, int startIndex)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (startIndex >= source.Length)
            return "";
        else if (startIndex < 0)
            return source;
        else
            return source.Substring(startIndex);
    }

    /// <summary>
    ///     Same as <see cref="string.Substring(int, int)"/> but does not throw exceptions when the start index or length (or
    ///     both) fall outside the boundaries of the string. Instead the result is truncated as appropriate.</summary>
    public static string SubstringSafe(this string source, int startIndex, int length)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
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
    ///     Same as <see cref="string.Substring(int)"/> but counting from the right instead of left. Equivalent to
    ///     <c>source.Reverse().Substring(startIndex).Reverse()</c>.</summary>
    public static string SubstringRight(this string source, int startIndex)
    {
        return source.Substring(0, source.Length - startIndex);
    }

    /// <summary>
    ///     Same as <see cref="string.Substring(int, int)"/> but counting from the right instead of left. Equivalent to
    ///     <c>source.Reverse().Substring(startIndex, length).Reverse()</c>.</summary>
    public static string SubstringRight(this string source, int startIndex, int length)
    {
        return source.Substring(source.Length - startIndex - length, length);
    }

    /// <summary>
    ///     Same as <see cref="SubstringSafe(string, int)"/> but counting from the right instead of left. Equivalent to
    ///     <c>source.Reverse().SubstringSafe(startIndex).Reverse()</c>.</summary>
    public static string SubstringRightSafe(this string source, int startIndex)
    {
        return source.SubstringSafe(0, source.Length - startIndex);
    }

    /// <summary>
    ///     Same as <see cref="SubstringSafe(string, int, int)"/> but counting from the right instead of left. Equivalent to
    ///     <c>source.Reverse().SubstringSafe(startIndex, length).Reverse()</c>.</summary>
    public static string SubstringRightSafe(this string source, int startIndex, int length)
    {
        return source.SubstringSafe(source.Length - startIndex - length, length);
    }

    /// <summary>Use <see cref="EqualsIgnoreCase"/>.</summary>
    [Obsolete("Use EqualsIgnoreCase"), EditorBrowsable(EditorBrowsableState.Never)]
    public static bool EqualsNoCase(this string strthis, string str) => EqualsIgnoreCase(strthis, str);
    /// <summary>Use <see cref="ContainsIgnoreCase"/>.</summary>
    [Obsolete("Use ContainsIgnoreCase"), EditorBrowsable(EditorBrowsableState.Never)]
    public static bool ContainsNoCase(this string strthis, string str) => ContainsIgnoreCase(strthis, str);

    /// <summary>
    ///     Determines whether this string is equal to the other string using the ordinal case-insensitive comparison (<see
    ///     cref="StringComparison.OrdinalIgnoreCase"/>).</summary>
    public static bool EqualsIgnoreCase(this string strthis, string str)
    {
        if (strthis == null)
            throw new ArgumentNullException(nameof(strthis));
        return strthis.Equals(str, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether this string contains the other string using the ordinal case-insensitive comparison (<see
    ///     cref="StringComparison.OrdinalIgnoreCase"/>).</summary>
    public static bool ContainsIgnoreCase(this string strthis, string str)
    {
        if (strthis == null)
            throw new ArgumentNullException(nameof(strthis));
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        return strthis.IndexOf(str, StringComparison.OrdinalIgnoreCase) != -1;
    }

    /// <summary>
    ///     Determines whether the beginning of this string instance matches the specified string using the ordinal
    ///     case-insensitive comparison (<see cref="StringComparison.OrdinalIgnoreCase"/>).</summary>
    public static bool StartsWithIgnoreCase(this string haystack, string value)
    {
        if (haystack == null)
            throw new ArgumentNullException(nameof(haystack));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return haystack.StartsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether the end of this string instance matches the specified string using the ordinal case-insensitive
    ///     comparison (<see cref="StringComparison.OrdinalIgnoreCase"/>).</summary>
    public static bool EndsWithIgnoreCase(this string haystack, string value)
    {
        if (haystack == null)
            throw new ArgumentNullException(nameof(haystack));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return haystack.EndsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns true if and only if this string ends with the specified character.</summary>
    /// <seealso cref="StartsWith"/>
    public static bool EndsWith(this string str, char? ch)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (ch == null)
            return true;
        return str != null && str.Length > 0 && str[str.Length - 1] == ch.Value;
    }

    /// <summary>
    ///     Returns true if and only if this string starts with the specified character.</summary>
    /// <seealso cref="EndsWith"/>
    public static bool StartsWith(this string str, char? ch)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (ch == null)
            return true;
        return str != null && str.Length > 0 && str[0] == ch.Value;
    }

    /// <summary>Reconstructs a byte array from its hexadecimal representation (“hexdump”).</summary>
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
                throw new InvalidOperationException($"The character '{input[j]}' is not a valid hexadecimal digit.");
            j++;
            if (input[j] >= '0' && input[j] <= '9')
                lowerNibble = input[j] - '0';
            else if (input[j] >= 'a' && input[j] <= 'f')
                lowerNibble = input[j] - 'a' + 10;
            else if (input[j] >= 'A' && input[j] <= 'F')
                lowerNibble = input[j] - 'A' + 10;
            else
                throw new InvalidOperationException($"The character '{input[j]}' is not a valid hexadecimal digit.");
            j++;
            result[i] = (byte) ((upperNibble << 4) + lowerNibble);
        }
        return result;
    }

    /// <summary>
    ///     Removes the overall indentation of the specified string while maintaining the relative indentation of each line.</summary>
    /// <param name="str">
    ///     String to remove indentation from.</param>
    /// <returns>
    ///     A string in which every line that isn’t all whitespace has had spaces removed from the beginning equal to the
    ///     least amount of spaces at the beginning of any line.</returns>
    public static string Unindent(this string str)
    {
        var least = Regex.Matches(str, @"^( *)(?![\r\n ]|\z)", RegexOptions.Multiline).Cast<Match>().MinOrDefault(m => m.Groups[1].Length, 0);
        return least == 0 ? str : Regex.Replace(str, "^" + new string(' ', least), "", RegexOptions.Multiline);
    }

    /// <summary>
    ///     Inserts spaces at the beginning of every line contained within the specified string.</summary>
    /// <param name="str">
    ///     String to add indentation to.</param>
    /// <param name="by">
    ///     Number of spaces to add.</param>
    /// <param name="indentFirstLine">
    ///     If true (default), all lines are indented; otherwise, all lines except the first.</param>
    /// <returns>
    ///     The indented string.</returns>
    public static string Indent(this string str, int by, bool indentFirstLine = true)
    {
        if (indentFirstLine)
            return Regex.Replace(str, "^", new string(' ', by), RegexOptions.Multiline);
        return Regex.Replace(str, "(?<=\n)", new string(' ', by));
    }

    /// <summary>
    ///     Removes spaces from the beginning of every line in such a way that the smallest indentation is reduced to zero.
    ///     Lines which contain only whitespace are not considered in the calculation and may therefore become empty.</summary>
    /// <param name="str">
    ///     The string to transform.</param>
    public static string RemoveCommonIndentation(this string str)
    {
        var minLen = Regex.Matches(str, @"^(?> *)(?!\r|$| )", RegexOptions.Multiline)
                    .Cast<Match>()
                    .Min(m => m.Length);
        return Regex.Replace(
            str,
            $"^(?> {{{minLen}}})|^(?> {{0,{minLen}}}(\r|$))",
            "",
            RegexOptions.Multiline);
    }

    /// <summary>
    ///     Splits a string into chunks of equal size. The last chunk may be smaller than <paramref name="chunkSize"/>, but
    ///     all chunks, if any, will contain at least 1 character.</summary>
    /// <param name="str">
    ///     String to split into chunks.</param>
    /// <param name="chunkSize">
    ///     Size of each chunk. Must be greater than zero.</param>
    /// <returns>
    ///     A lazy-evaluated collection containing the chunks from the string.</returns>
    public static IEnumerable<string> Split(this string str, int chunkSize)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (chunkSize <= 0)
            throw new ArgumentException("chunkSize must be greater than zero.", nameof(chunkSize));
        if (str.Length == 0)
            return Enumerable.Empty<string>();
        return splitIterator(str, chunkSize);
    }
    private static IEnumerable<string> splitIterator(this string str, int chunkSize)
    {
        for (int offset = 0; offset < str.Length; offset += chunkSize)
            yield return str.Substring(offset, Math.Min(chunkSize, str.Length - offset));
    }

    /// <summary>
    ///     Returns a new string in which all occurrences of <paramref name="oldValue"/> in the current instance, identified
    ///     using the specified string comparison, are replaced with <paramref name="newValue"/>.</summary>
    public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (oldValue == null)
            throw new ArgumentNullException(nameof(oldValue));
        if (oldValue.Length == 0)
            throw new ArgumentException("oldValue cannot be the empty string.", nameof(oldValue));
        if (newValue == null)
            throw new ArgumentNullException(nameof(newValue));
        var output = "";
        while (true)
        {
            var p = str.IndexOf(oldValue, comparison);
            if (p == -1)
                return output + str;
            output += str.Substring(0, p) + newValue;
            str = str.Substring(p + oldValue.Length);
        }
    }

    /// <summary>
    ///     Returns a string array that contains the substrings in this string that are delimited by elements of a specified
    ///     string array.</summary>
    /// <param name="str">
    ///     String to be split.</param>
    /// <param name="separator">
    ///     Strings that delimit the substrings in this string.</param>
    /// <returns>
    ///     An array whose elements contain the substrings in this string that are delimited by one or more strings in
    ///     separator. For more information, see the Remarks section.</returns>
    public static string[] Split(this string str, params string[] separator)
    {
        return str.Split(separator, StringSplitOptions.None);
    }

    /// <summary>
    ///     Returns a string array that contains the substrings in this string that are delimited by elements of a specified
    ///     string array. Empty items (zero-length strings) are filtered out.</summary>
    /// <param name="str">
    ///     String to be split.</param>
    /// <param name="separator">
    ///     Strings that delimit the substrings in this string.</param>
    /// <returns>
    ///     An array whose elements contain the substrings in this string that are delimited by one or more strings in
    ///     separator. For more information, see the Remarks section.</returns>
    public static string[] SplitNoEmpty(this string str, params string[] separator)
    {
        return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Determines whether the string contains only the characters 0-9.</summary>
    public static bool IsNumeric(this string str)
    {
        for (int i = 0; i < str.Length; i++)
            if (str[i] < '0' || str[i] > '9')
                return false;
        return true;
    }

    /// <summary>Adds the specified line to the end of the current string. Returns the line if the current string is null.</summary>
    public static string AddLine(this string str, string line)
    {
        if (str == null)
            return line;
        return str + Environment.NewLine + line;
    }

    /// <summary>Parses this string as an <c>int</c> using <see cref="int.Parse(string)"/>.</summary>
    public static int ParseInt(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => int.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as an <c>int</c> using <see cref="int.TryParse(string?, out int)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static int ParseInt(this string str, int default_, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => int.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as an <c>int</c> using <see cref="int.TryParse(string?, out int)"/>. Returns <c>null</c> if the
    ///     value cannot be parsed.</summary>
    public static int? ParseIntOrNull(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => int.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>uint</c> using <see cref="uint.Parse(string)"/>.</summary>
    public static uint ParseUInt(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => uint.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>uint</c> using <see cref="uint.TryParse(string?, out uint)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static uint ParseUInt(this string str, uint default_, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => uint.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>uint</c> using <see cref="uint.TryParse(string?, out uint)"/>. Returns <c>null</c> if
    ///     the value cannot be parsed.</summary>
    public static uint? ParseUIntOrNull(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => uint.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>long</c> using <see cref="long.Parse(string)"/>.</summary>
    public static long ParseLong(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => long.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>long</c> using <see cref="long.TryParse(string?, out long)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static long ParseLong(this string str, long default_, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => long.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>long</c> using <see cref="long.TryParse(string?, out long)"/>. Returns <c>null</c> if
    ///     the value cannot be parsed.</summary>
    public static long? ParseLongOrNull(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => long.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>ulong</c> using <see cref="ulong.Parse(string)"/>.</summary>
    public static ulong ParseULong(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => ulong.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>ulong</c> using <see cref="ulong.TryParse(string?, out ulong)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static ulong ParseULong(this string str, ulong default_, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => ulong.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>ulong</c> using <see cref="ulong.TryParse(string?, out ulong)"/>. Returns <c>null</c>
    ///     if the value cannot be parsed.</summary>
    public static ulong? ParseULongOrNull(this string str, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        => ulong.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>double</c> using <see cref="double.Parse(string)"/>.</summary>
    public static double ParseDouble(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => double.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>double</c> using <see cref="double.TryParse(string?, out double)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static double ParseDouble(this string str, double default_, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => double.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>double</c> using <see cref="double.TryParse(string?, out double)"/>. Returns
    ///     <c>null</c> if the value cannot be parsed.</summary>
    public static double? ParseDoubleOrNull(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => double.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>float</c> using <see cref="float.Parse(string)"/>.</summary>
    public static float ParseFloat(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => float.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>float</c> using <see cref="float.TryParse(string?, out float)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static float ParseFloat(this string str, float default_, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => float.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>float</c> using <see cref="float.TryParse(string?, out float)"/>. Returns <c>null</c>
    ///     if the value cannot be parsed.</summary>
    public static float? ParseFloatOrNull(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => float.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>Parses this string as a <c>decimal</c> using <see cref="decimal.Parse(string)"/>.</summary>
    public static decimal ParseDecimal(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => decimal.Parse(str, style, provider);

    /// <summary>
    ///     Parses this string as a <c>decimal</c> using <see cref="decimal.TryParse(string?, out decimal)"/>. Returns <paramref
    ///     name="default_"/> if the value cannot be parsed.</summary>
    public static decimal ParseDecimal(this string str, decimal default_, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => decimal.TryParse(str, style, provider, out var result) ? result : default_;

    /// <summary>
    ///     Parses this string as a <c>decimal</c> using <see cref="decimal.TryParse(string?, out decimal)"/>. Returns
    ///     <c>null</c> if the value cannot be parsed.</summary>
    public static decimal? ParseDecimalOrNull(this string str, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
        => decimal.TryParse(str, style, provider, out var result) ? result : null;

    /// <summary>
    ///     Determines whether the <paramref name="input"/> string matches the specified regular expression <paramref
    ///     name="pattern"/>.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="match">
    ///     Receives an object that contains information about the match.</param>
    /// <returns>
    ///     A boolean indicating whether a match was found or not.</returns>
    public static bool RegexMatch(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, out Match match)
    {
        match = Regex.Match(input, pattern);
        return match.Success;
    }

    /// <summary>
    ///     Determines whether the <paramref name="input"/> string matches the specified regular expression <paramref
    ///     name="pattern"/>.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="options">
    ///     A bitwise combination of the enumeration values that provide options for matching.</param>
    /// <param name="match">
    ///     Receives an object that contains information about the match.</param>
    /// <returns>
    ///     A boolean indicating whether a match was found or not.</returns>
    public static bool RegexMatch(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, RegexOptions options, out Match match)
    {
        match = Regex.Match(input, pattern, options);
        return match.Success;
    }

    /// <summary>
    ///     Determines whether the <paramref name="input"/> string matches the specified regular expression <paramref
    ///     name="pattern"/> using a culture-invariant case-insensitive match.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="match">
    ///     Receives an object that contains information about the match.</param>
    /// <returns>
    ///     A boolean indicating whether a match was found or not.</returns>
    public static bool RegexMatchIgnoreCase(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, out Match match)
    {
        match = Regex.Match(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success;
    }

    /// <summary>
    ///     Searches the specified <paramref name="input"/> string for all occurrences of a specified regular expression
    ///     <paramref name="pattern"/>.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="options">
    ///     A bitwise combination of the enumeration values that provide options for matching.</param>
    /// <returns>
    ///     A collection of the Match objects found by the search. If no matches are found, the method returns an empty
    ///     collection object.</returns>
    public static MatchCollection RegexMatches(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, RegexOptions options = 0) => Regex.Matches(input, pattern, options);

    /// <summary>
    ///     Searches the specified <paramref name="input"/> string for all occurrences of a specified regular expression
    ///     <paramref name="pattern"/>.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <returns>
    ///     A collection of the Match objects found by the search. If no matches are found, the method returns an empty
    ///     collection object.</returns>
    public static MatchCollection RegexMatchesIgnoreCase(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern) => Regex.Matches(input, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    ///     In a specified <paramref name="input"/> string, replaces all strings that match a specified regular expression
    ///     <paramref name="pattern"/> with a specified <paramref name="replacement"/> string.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="replacement">
    ///     The replacement string.</param>
    /// <param name="options">
    ///     A bitwise combination of the enumeration values that provide options for matching.</param>
    /// <returns>
    ///     A new string that is identical to the input string, except that the <paramref name="replacement"/> string takes
    ///     the place of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the method
    ///     returns the current instance unchanged.</returns>
    public static string RegexReplace(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, string replacement, RegexOptions options = 0) => Regex.Replace(input, pattern, replacement, options);

    /// <summary>
    ///     In a specified <paramref name="input"/> string, replaces all strings that match a specified regular expression
    ///     <paramref name="pattern"/> with a specified <paramref name="replacement"/> string. The pattern is matched using an
    ///     invariant-culture case-insensitive match.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="replacement">
    ///     The replacement string.</param>
    /// <returns>
    ///     A new string that is identical to the input string, except that the <paramref name="replacement"/> string takes
    ///     the place of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the method
    ///     returns the current instance unchanged.</returns>
    public static string RegexReplaceIgnoreCase(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, string replacement) => Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    ///     In a specified <paramref name="input"/> string, replaces all strings that match a specified regular expression
    ///     <paramref name="pattern"/> with a string returned by a match <paramref name="evaluator"/> delegate.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="evaluator">
    ///     A custom method that examines each match and returns either the original matched string or a replacement string.</param>
    /// <param name="options">
    ///     A bitwise combination of the enumeration values that provide options for matching.</param>
    /// <returns>
    ///     A new string that is identical to the input string, except that a replacement string takes the place of each
    ///     matched string. If <paramref name="pattern"/> is not matched in the current instance, the method returns the
    ///     current instance unchanged.</returns>
    public static string RegexReplace(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, MatchEvaluator evaluator, RegexOptions options = 0) => Regex.Replace(input, pattern, evaluator, options);

    /// <summary>
    ///     In a specified <paramref name="input"/> string, replaces all strings that match a specified regular expression
    ///     <paramref name="pattern"/> with a string returned by a match <paramref name="evaluator"/> delegate. The pattern is
    ///     matched using an invariant-culture case-insensitive match.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to match.</param>
    /// <param name="evaluator">
    ///     A custom method that examines each match and returns either the original matched string or a replacement string.</param>
    /// <returns>
    ///     A new string that is identical to the input string, except that a replacement string takes the place of each
    ///     matched string. If <paramref name="pattern"/> is not matched in the current instance, the method returns the
    ///     current instance unchanged.</returns>
    public static string RegexReplaceIgnoreCase(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, MatchEvaluator evaluator) => Regex.Replace(input, pattern, evaluator, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to split by.</param>
    /// <param name="options">
    ///     A bitwise combination of the enumeration values that provide options for matching.</param>
    /// <returns>
    ///     A new string array consisting of the pieces of the input string found between matches of the regular expression.</returns>
    public static string[] RegexSplit(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern, RegexOptions options = RegexOptions.None) => Regex.Split(input, pattern, options);

    /// <summary>
    ///     Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>. The pattern is
    ///     matched using an invariant-culture case-insensitive match.</summary>
    /// <param name="input">
    ///     The string to search for a match.</param>
    /// <param name="pattern">
    ///     The regular expression pattern to split by.</param>
    /// <returns>
    ///     A new string array consisting of the pieces of the input string found between matches of the regular expression.</returns>
    public static string[] RegexSplitIgnoreCase(this string input,
#if NET7_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.StringSyntax(System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.Regex)]
#endif
        string pattern) => Regex.Split(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}

/// <summary>Selects how the escaped JS string should be put into quotes.</summary>
#if EXPORT_UTIL
public
#endif
enum JsQuotes
{
    /// <summary>Put single quotes around the output. Single quotes are allowed in JavaScript only, but not in JSON.</summary>
    Single,
    /// <summary>Put double quotes around the output. Double quotes are allowed both in JavaScript and JSON.</summary>
    Double,
    /// <summary>Do not put any quotes around the output. The escaped output may be surrounded with either type of quotes.</summary>
    None
}
