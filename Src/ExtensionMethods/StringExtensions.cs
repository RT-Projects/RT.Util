using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace RT.Util.ExtensionMethods
{
    public static class StringExtensions
    {
        /// <summary>
        /// Escapes all necessary characters in the specified string so as to make it usable safely in an HTML or XML context.
        /// </summary>
        /// <param name="Input">The string to apply HTML or XML escaping to.</param>
        /// <returns>The specified string with the necessary HTML or XML escaping applied.</returns>
        public static string HTMLEscape(this string Input)
        {
            return Input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        /// <summary>
        /// Escapes all necessary characters in the specified string so as to make it usable safely in a URL.
        /// </summary>
        /// <param name="Input">The string to apply URL escaping to.</param>
        /// <returns>The specified string with the necessary URL escaping applied.</returns>
        public static string URLEscape(this string Input)
        {
            byte[] UTF8 = Input.ToUTF8();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in UTF8)
                sb.Append((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9')
                    || (b == '$') || (b == '-') || (b == '_') || (b == '.') || (b == '!') || (b == '/')
                    || (b == '*') || (b == '\'') || (b == '(') || (b == ')') || (b == ',')
                    ? ((char) b).ToString() : string.Format("%{0:X2}", b));
            return sb.ToString();
        }

        /// <summary>
        /// Reverses the escaping performed by <see cref="URLEscape"/>() by decoding hexadecimal URL escape sequences into their original characters.
        /// </summary>
        /// <param name="Input">String containing URL escape sequences to be decoded.</param>
        /// <returns>The specified string with all URL escape sequences decoded.</returns>
        public static string URLUnescape(this string Input)
        {
            if (Input.Length < 3)
                return Input;
            int BufferSize = 0;
            int i = 0;
            while (i < Input.Length)
            {
                BufferSize++;
                if (Input[i] == '%') { i += 2; }
                i++;
            }
            byte[] Buffer = new byte[BufferSize];
            BufferSize = 0;
            i = 0;
            while (i < Input.Length)
            {
                if (Input[i] == '%' && i < Input.Length - 2)
                {
                    try
                    {
                        Buffer[BufferSize] = byte.Parse("" + Input[i + 1] + Input[i + 2], NumberStyles.HexNumber);
                        BufferSize++;
                    }
                    catch (Exception) { }
                    i += 3;
                }
                else
                {
                    Buffer[BufferSize] = Input[i] == '+' ? (byte) ' ' : (byte) Input[i];
                    BufferSize++;
                    i++;
                }
            }
            return Encoding.UTF8.GetString(Buffer, 0, BufferSize);
        }

        /// <summary>
        /// Converts the specified string to UTF-8.
        /// </summary>
        /// <param name="Input">String to convert to UTF-8.</param>
        /// <returns>The specified string, converted to a byte-array containing the UTF-8 encoding of the string.</returns>
        public static byte[] ToUTF8(this string Input)
        {
            return Encoding.UTF8.GetBytes(Input);
        }

        /// <summary>
        /// Determines the length of the UTF-8 encoding of the specified string.
        /// </summary>
        /// <param name="Input">String to determined UTF-8 length of.</param>
        /// <returns>The length of the string in bytes when encoded as UTF-8.</returns>
        public static int UTF8Length(this string Input)
        {
            return Encoding.UTF8.GetByteCount(Input);
        }

        /// <summary>
        /// Converts the specified string to a byte array. Non-ASCII characters are replaced with question marks ('?').
        /// </summary>
        /// <param name="Input">String to convert to a byte array.</param>
        /// <returns>The specified string, converted to a byte-array with non-ASCII characters replaced with question marks ('?').</returns>
        public static byte[] ToASCII(this string Input)
        {
            return Encoding.ASCII.GetBytes(Input);
        }
    }
}
