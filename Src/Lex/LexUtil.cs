using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using System.Globalization;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex
{
    public static class LexUtil
    {
        public static bool IsDecimalDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsHexadecimalDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        #region Number lexing

        public static string LexDecimalInteger(LexReader reader)
        {
            string result = reader.ConsumeStringWhile(IsDecimalDigit);
            return (result.Length == 0) ? null : result;
        }

        public static string Lex0xHexInteger(LexReader reader)
        {
            if (!reader.ContinuesWith("0x") && !reader.ContinuesWith("0X"))
                return null;
            reader.Consume(2);
            string result = reader.ConsumeStringWhile(IsHexadecimalDigit);
            if (result.Length == 0)
                throw new LexException(reader.GetPosition(-2), "Hexadecimal integers starting with the \"0x\" prefix must have at least one hex digit.");
            return (result.Length == 0) ? null : result;
        }

        public static bool ParseFancyBaseInteger(LexReader reader)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region String lexing

        public static string LexStringLiteral(LexReader reader, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
        {
            return LexStringLiteral(reader, null, null, openingSequence, closingSequence, escapeClosingByDoubling);
        }

        public static string LexStringLiteral(LexReader reader, IDictionary<char, char> basicEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
        {
            return LexStringLiteral(reader, basicEscapes, null, openingSequence, closingSequence, escapeClosingByDoubling);
        }

        public static string LexStringLiteral(LexReader reader, IDictionary<char, char> basicEscapes, IDictionary<char, Func<LexReader, string>> advancedEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
        {
            var builder = new StringBuilder();
            if (!reader.ContinuesWith(openingSequence))
                return null;

            var startingPos = reader.GetPosition();
            string doubleClosingSequence = closingSequence + closingSequence;
            reader.Consume(openingSequence.Length);
            while (true)
            {
                if (reader.EndOfFile())
                    throw new LexException(reader.GetPosition(), "Unexpected end of string literal.").AddPosition(startingPos, "Start of string literal").Freeze();

                if (escapeClosingByDoubling && reader.ContinuesWith(doubleClosingSequence))
                {
                    reader.Consume(doubleClosingSequence.Length);
                    builder.Append(doubleClosingSequence);
                }
                else if (reader.ContinuesWith(closingSequence))
                {
                    reader.Consume(closingSequence.Length);
                    return builder.ToString();
                }
                else if ((basicEscapes != null || advancedEscapes != null) && reader.ContinuesWith("\\"))
                {
                    reader.Consume(1);
                    char escape = reader.ConsumeChar();
                    char replacement;
                    Func<LexReader, string> replacementFunc;
                    if (basicEscapes.TryGetValue(escape, out replacement))
                        builder.Append(replacement);
                    else if (advancedEscapes.TryGetValue(escape, out replacementFunc))
                        builder.Append(replacementFunc(reader));
                    else
                        throw new LexException(reader.GetPosition(-1), @"Unrecognized escape sequence: ""\{0}"".".Fmt(escape));
                    // could introduce support for other behaviours on unrecognized \x, such as no-escape (\x => \x) or as-is escape (\x => x)
                }
                else
                {
                    builder.Append(reader.ConsumeChar());
                }
            }
        }

        public static string LexCsharpUnicodeFixedLenCharEscape(LexReader reader, bool islong)
        {
            string numberOfChars = islong ? "eight" : "four";
            int numChars = islong ? 8 : 4;
            string sequence = reader.ConsumeStringWhile(IsHexadecimalDigit, numChars);
            if (sequence.Length < numChars)
                throw new LexException(reader.GetPosition(), "Unicode escape sequence is too short (requires {0} hex digits).".Fmt(numberOfChars));
            if (islong)
                return char.ConvertFromUtf32(int.Parse(sequence, NumberStyles.HexNumber));
            else
                return new string((char) int.Parse(sequence, NumberStyles.HexNumber), 1);
        }

        public static string LexCsharpUnicodeVariableLenCharEscape(LexReader reader)
        {
            string sequence = reader.ConsumeStringWhile(IsHexadecimalDigit, 4);
            if (sequence.Length == 0)
                throw new LexException(reader.GetPosition(), "Unicode escape sequence is too short (requires at least one hex digit).");
            return ((char) int.Parse(sequence, NumberStyles.HexNumber)).ToString();
        }

        private static IDictionary<char, char> _csharpStringEscapesBasic;
        public static IDictionary<char, char> CsharpStringEscapesBasic
        {
            get
            {
                if (_csharpStringEscapesBasic == null)
                {
                    _csharpStringEscapesBasic = new ReadOnlyDictionary<char, char>(new Dictionary<char, char>()
                        {
                            {'\'',  '\''},
                            {'\\',  '\\'},
                            {'"',  '"'},
                            {'0',  '\0'},
                            {'a',  '\a'},
                            {'b',  '\b'},
                            {'f',  '\f'},
                            {'n',  '\n'},
                            {'r',  '\r'},
                            {'t',  '\t'},
                            {'v',  '\v'},
                        });
                }
                return _csharpStringEscapesBasic;
            }
        }

        private static IDictionary<char, Func<LexReader, string>> _csharpStringEscapesAdvanced;
        public static IDictionary<char, Func<LexReader, string>> CsharpStringEscapesAdvanced
        {
            get
            {
                if (_csharpStringEscapesAdvanced == null)
                {
                    _csharpStringEscapesAdvanced = new ReadOnlyDictionary<char, Func<LexReader, string>>(new Dictionary<char, Func<LexReader, string>>()
                        {
                            {'u', reader => LexUtil.LexCsharpUnicodeFixedLenCharEscape(reader, false)},
                            {'U', reader => LexUtil.LexCsharpUnicodeFixedLenCharEscape(reader, true)},
                            {'x', LexUtil.LexCsharpUnicodeVariableLenCharEscape}
                        });
                }
                return _csharpStringEscapesAdvanced;
            }
        }

        #endregion
    }
}
