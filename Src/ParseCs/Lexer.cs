using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;

namespace RT.KitchenSink.ParseCs
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

    public static class Lexer
    {
        public static string[] Keywords = new[] {
            "abstract", "as",
            "base", "bool", "break", "byte",
            "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double",
            "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach",
            "goto",
            "if", "implicit", "in", "int", "interface", "internal", "is",
            "lock", "long",
            "namespace", "new", "null",
            "object", "operator", "out", "override",
            "params", "private", "protected", "public",
            "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile",
            "while"
        };

        public static string[] CharacterTokens = new[] {
            // Three characters
            "<<=", ">>=",
            // Two characters
            "!=", "%=", "^=", "&=", "&&", "*=", "--", "-=", "->", "==", "=>", "+=", "++", "::", "|=", "||", "<<", "<=", ">>", ">=", "/=", "??",
            // One character
            "~", "!", "%", "^", "&", "*", "(", ")", "-", "=", "+", "[", "{", "]", "}", ";", ":", "|", ",", "<", ".", ">", "/", "?"
        };

        [Flags]
        public enum LexOptions { IgnoreComments = 1 };

        public static List<string> PreprocessorDirectives = new List<string>();

        public static TokenJar Lex(string data, LexOptions opt)
        {
            var endToken = new Token(null, TokenType.EndOfFile, data.Length);
            return new TokenJar(preprocess(lex(data)).Where(t => ((t.Type != TokenType.CommentSlashSlash && t.Type != TokenType.CommentSlashStar) || (opt & LexOptions.IgnoreComments) == 0)), endToken);
        }

        public static TokenJar Lex(string data)
        {
            var endToken = new Token(null, TokenType.EndOfFile, data.Length);
            return new TokenJar(preprocess(lex(data)), endToken);
        }

        private static IEnumerable<Token> lex(string data)
        {
            int index = 0;
            int dataLength = data.Length;
            while (index < dataLength && char.IsWhiteSpace(data, index))
                index += char.IsSurrogate(data, index) ? 2 : 1;

            while (index < dataLength)
            {
                // Keywords
                foreach (var kw in Keywords)
                {
                    for (int i = 0; i < kw.Length; i++)
                        if (index + i < data.Length && kw[i] != data[index + i])
                            goto ContinueForeachKw;

                    if (index + kw.Length >= dataLength || data[index + kw.Length] == '_' || char.IsLetterOrDigit(data, index + kw.Length))
                        continue;

                    yield return new Token(kw, TokenType.Builtin, index);
                    index += kw.Length;
                    goto ContinueDo;

                ContinueForeachKw: ;
                }

                // Identifiers
                if (char.IsLetter(data, index) || data[index] == '_' || (data[index] == '@' && index + 1 < dataLength && (data[index + 1] == '_' || char.IsLetter(data, index + 1))))
                {
                    int tokenIndex = index;
                    if (data[index] == '@')
                        index++;
                    int origIndex = index;
                    index += char.IsSurrogate(data, index) ? 2 : 1;
                    while (index < dataLength && (char.IsLetterOrDigit(data, index) || data[index] == '_'))
                        index += char.IsSurrogate(data, index) ? 2 : 1;
                    yield return new Token(data.Substring(origIndex, index - origIndex), TokenType.Identifier, tokenIndex);
                    goto ContinueDo;
                }

                // String literals (verbatim)
                if (data[index] == '@' && index + 1 < dataLength && data[index + 1] == '"')
                {
                    var tokenIndex = index;
                    index += 2;
                    var str = new StringBuilder();
                    while (index < dataLength)
                    {
                        if (data[index] == '"' && index + 1 < dataLength && data[index + 1] == '"')
                        {
                            str.Append('"');
                            index += 2;
                        }
                        else if (data[index] == '"')
                        {
                            yield return new Token(str.ToString(), TokenType.StringLiteral, tokenIndex);
                            index++;
                            goto ContinueDo;
                        }
                        else
                        {
                            str.Append(data[index]);
                            index++;
                        }
                    }
                    throw new LexException("Unterminated string literal.", index);
                }

                // String literals and character literals (non-verbatim)
                if (data[index] == '"' || data[index] == '\'')
                {
                    var tokenIndex = index;
                    bool isChar = data[index] == '\'';
                    index++;
                    var str = new StringBuilder();
                    while (index < dataLength)
                    {
                        bool more = index + 1 < dataLength;
                        char ch = data[index];
                        if (ch == '\\' && more)
                        {
                            switch (data[index + 1])
                            {
                                case '\'': str.Append('\''); index += 2; break;
                                case '"': str.Append('"'); index += 2; break;
                                case '\\': str.Append('\\'); index += 2; break;
                                case '0': str.Append('\0'); index += 2; break;
                                case 'a': str.Append('\a'); index += 2; break;
                                case 'b': str.Append('\b'); index += 2; break;
                                case 'f': str.Append('\f'); index += 2; break;
                                case 'n': str.Append('\n'); index += 2; break;
                                case 'r': str.Append('\r'); index += 2; break;
                                case 't': str.Append('\t'); index += 2; break;
                                case 'v': str.Append('\v'); index += 2; break;

                                // Fixed-length Unicode character escape sequence
                                case 'u':
                                case 'U':
                                    bool islong = data[index + 1] == 'U';
                                    string numberOfChars = islong ? "eight" : "four";
                                    int numChars = islong ? 8 : 4;
                                    index += 2;
                                    if (index + numChars > dataLength)
                                        throw new LexException("Unicode escape sequence too short (requires {0} hex digits).".Fmt(numberOfChars), index);
                                    for (int i = 0; i < numChars; i++)
                                        if (!((data[index + i] >= '0' && data[index + i] <= '9') || (data[index + i] >= 'a' && data[index + i] <= 'f') || (data[index + i] >= 'A' && data[index + i] <= 'F')))
                                            throw new LexException("Unicode escape sequence too short (requires {0} hex digits).".Fmt(numberOfChars), index);
                                    if (islong)
                                        str.Append(char.ConvertFromUtf32(int.Parse(data.Substring(index, 8), NumberStyles.HexNumber)));
                                    else
                                        str.Append((char) int.Parse(data.Substring(index, 4), NumberStyles.HexNumber));
                                    index += numChars;
                                    break;

                                // Variable-length Unicode character escape sequence
                                case 'x':
                                    index += 2;
                                    int origIndex = index;
                                    if (index + 1 >= dataLength || !((data[index + 1] >= '0' && data[index + 1] <= '9') || (data[index + 1] >= 'a' && data[index + 1] <= 'f') || (data[index + 1] >= 'A' && data[index + 1] <= 'F')))
                                        throw new LexException("Unicode escape sequence too short (requires at least one hex digit).", index);
                                    index++;
                                    while (index < dataLength && index - origIndex < 4 && ((data[index] >= '0' && data[index] <= '9') || (data[index] >= 'a' && data[index] <= 'f') || (data[index] >= 'A' && data[index] <= 'F')))
                                        index++;
                                    str.Append((char) int.Parse(data.Substring(origIndex, index - origIndex), NumberStyles.HexNumber));
                                    break;

                                default:
                                    throw new LexException("Unrecognized escape sequence: \\{0}".Fmt(data[index + 1]), index);
                            }
                        }
                        else if (!isChar && ch == '"')
                        {
                            yield return new Token(str.ToString(), TokenType.StringLiteral, tokenIndex);
                            index++;
                            goto ContinueDo;
                        }
                        else if (isChar && ch == '\'')
                        {
                            if (str.Length < 1)
                                throw new LexException("Empty character literal.", index);
                            if (str.Length > 1)
                                throw new LexException("Too many characters in character literal.", index);
                            yield return new Token(str.ToString(), TokenType.CharacterLiteral, tokenIndex);
                            index++;
                            goto ContinueDo;
                        }
                        else if (ch == '\n' || ch == '\r')
                            throw new LexException("Unterminated string literal.", index);
                        else
                        {
                            str.Append(ch);
                            index++;
                        }
                    }
                    throw new LexException("Unterminated string literal.", index);
                }

                // Hexadecimal integer literals
                if (data[index] == '0' && index + 1 < data.Length && (data[index + 1] == 'x' || data[index + 1] == 'X'))
                {
                    int origIndex = index;
                    index += 2;
                    while (index < data.Length && ((data[index] >= '0' && data[index] <= '9') || (data[index] >= 'a' && data[index] <= 'f') || (data[index] >= 'A' && data[index] <= 'F')))
                        index++;
                    if (index == origIndex + 2)
                        throw new LexException("Invalid number.", index);
                    if (data[index] == 'u' || data[index] == 'U' || data[index] == 'l' || data[index] == 'L')
                    {
                        char first = data[index];
                        index++;
                        if ((data[index] == 'u' || data[index] == 'U' || data[index] == 'l' || data[index] == 'L') && char.ToUpper(data[index]) != char.ToUpper(first))
                            index++;
                    }
                    yield return new Token(data.Substring(origIndex, index - origIndex), TokenType.NumberLiteral, origIndex);
                    goto ContinueDo;
                }

                // Number literals (floating point or integer)
                if ((data[index] >= '0' && data[index] <= '9') || (data[index] == '.' && index + 1 < data.Length && data[index + 1] >= '0' && data[index + 1] <= '9'))
                {
                    int origIndex = index;
                    bool haveDot = false;
                    bool haveE = false;
                    while (index < data.Length)
                    {
                        if (!haveDot && !haveE && data[index] == '.')
                        {
                            haveDot = true;
                            index++;
                        }
                        else if (!haveE && (data[index] == 'e' || data[index] == 'E'))
                        {
                            haveE = true;
                            index++;
                        }
                        else if (data[index] >= '0' && data[index] <= '9')
                            index++;
                        else
                            break;
                    }
                    // The number cannot end with a '.', but you can have member access expressions after numbers, so make sure the '.' becomes a separate token.
                    if (haveDot && data[index - 1] == '.')
                        index--;
                    if (index < data.Length && ((!haveDot && !haveE && (data[index] == 'u' || data[index] == 'U' || data[index] == 'l' || data[index] == 'L'))
                            || (data[index] == 'm' || data[index] == 'M' || data[index] == 'd' || data[index] == 'D' || data[index] == 'f' || data[index] == 'F')))
                    {
                        char first = data[index];
                        index++;
                        if (index < data.Length && ((char.ToUpper(first) == 'U' && char.ToUpper(data[index]) == 'L') || (char.ToUpper(first) == 'L' && char.ToUpper(data[index]) == 'U')))
                            index++;
                    }
                    yield return new Token(data.Substring(origIndex, index - origIndex), TokenType.NumberLiteral, origIndex);
                    goto ContinueDo;
                }

                // Comments
                if (data[index] == '/' && index + 1 < data.Length && (data[index + 1] == '/' || data[index + 1] == '*'))
                {
                    int origIndex = index;
                    if (data[index + 1] == '/')
                    {
                        int pos = data.IndexOf('\n', index + 2);
                        yield return new Token(pos < 0 ? data.Substring(origIndex) : data.Substring(origIndex, pos - origIndex), TokenType.CommentSlashSlash, origIndex);
                        index = pos < 0 ? data.Length : pos + 1;
                    }
                    else
                    {
                        int pos = data.IndexOf("*/", index + 2);
                        if (pos == -1)
                            throw new LexException("Unterminated comment.", index);
                        yield return new Token(data.Substring(origIndex, pos + 2 - origIndex), TokenType.CommentSlashStar, origIndex);
                        index = pos + 2;
                    }
                    goto ContinueDo;
                }

                // Tokens which consist of punctuation characters
                foreach (var ch in CharacterTokens)
                {
                    if (index + ch.Length > data.Length)
                        continue;

                    for (int i = 0; i < ch.Length; i++)
                        if (ch[i] != data[index + i])
                            goto ContinueForeachCh;

                    yield return new Token(ch, TokenType.Builtin, index);
                    index += ch.Length;
                    goto ContinueDo;

                ContinueForeachCh: ;
                }

                if (data[index] == '#' && beginningOfLine(data, index))
                {
                    int i = index;
                    while (i < data.Length && data[i] != '\n' && data[i] != '\r' && data[i] != '\u2028' && data[i] != '\u2029')
                        i++;
                    yield return new Token(data.Substring(index, i - index), TokenType.PreprocessorDirective, index);
                    index = i;
                    goto ContinueDo;
                }

                throw new LexException(@"Unrecognized token", index);

            ContinueDo:
                while (index < dataLength && char.IsWhiteSpace(data, index))
                    index += char.IsSurrogate(data, index) ? 2 : 1;
            }
        }

        private static bool beginningOfLine(string data, int index)
        {
            var i = index - 1;
            while (true)
            {
                if (i == -1 || data[i] == '\n' || data[i] == '\r' || data[i] == '\u2028' || data[i] == '\u2029')
                    return true;
                if (!char.IsWhiteSpace(data, i))
                    return false;
                i--;
            }
        }

        private static IEnumerable<Token> preprocess(IEnumerable<Token> tok)
        {
            return tok.Where(t => t.Type != TokenType.PreprocessorDirective);

            /*
            var openIfs = new Stack<bool>();
            var openRegions = new Stack<string>();
            int lastIndex = 0;

            foreach (var t in tok)
            {
                if (t.Type == TokenType.PreprocessorDirective)
                {
                    var cmd = t.TokenStr.Trim();
                    Match m;
                    if ((m = Regex.Match(cmd, @"^#if\s*!\s*")).Success)
                        openIfs.Push(openIfs.Peek() && !PreprocessorDirectives.Contains(cmd.Substring(m.Length)));
                    else if ((m = Regex.Match(cmd, @"^#if\s+")).Success)
                        openIfs.Push(openIfs.Peek() && PreprocessorDirectives.Contains(cmd.Substring(m.Length)));
                    else if (cmd == "#else")
                    {
                        var f = openIfs.Pop();
                        openIfs.Push(openIfs.Peek() && !f);
                    }
                    else if (cmd == "#endif")
                        openIfs.Pop();
                    else if ((m = Regex.Match(cmd, @"^#region\s+")).Success)
                        openRegions.Push(cmd.Substring(m.Length));
                    else if (cmd == "#endregion" || (cmd.StartsWith("#endregion") && char.IsWhiteSpace(cmd, "#endregion".Length)))
                        openRegions.Pop();
                    else if (cmd == "#pragma" || (cmd.StartsWith("#pragma") && char.IsWhiteSpace(cmd, "#pragma".Length))
                         || (cmd.StartsWith("#warning") && char.IsWhiteSpace(cmd, "#warning".Length))
                         || (cmd.StartsWith("#error") && char.IsWhiteSpace(cmd, "#error".Length)))
                    {
                        // ignore these
                    }
                    else if ((m = Regex.Match(cmd, @"^#define\s+")).Success)
                    {
                        if (openIfs.Peek())
                            PreprocessorDirectives.Add(cmd.Substring(m.Length));
                    }
                    else
                        throw new LexException(@"Unknown preprocessor directive: " + cmd, t.Index);
                }
                else if (openIfs.Peek())
                {
                    yield return t;
                }
                lastIndex = t.Index;
            }

            if (openIfs.Count > 1)
                throw new LexException(@"Unterminated #if directive.", lastIndex);
            */
        }
    }

    public sealed class LexException : Exception
    {
        private int _index;
        public LexException(string message, int index) : this(message, index, null) { }
        public LexException(string message, int index, Exception inner) : base(message, inner) { _index = index; }
        public int Index { get { return _index; } }
    }
}
