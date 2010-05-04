using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex
{
    public abstract class Token
    {
        public LexPosition StartLocation { get; private set; }
        public LexPosition EndLocation { get; private set; }

        public Token(LexPosition start, LexPosition end)
        {
            StartLocation = start;
            EndLocation = end;
        }

        public abstract class Parser
        {
            /// <summary>
            /// Parses the next token of a specific kind from the lex reader. Implementations must do one of the following:
            /// <list type="bullet">
            ///   <item>return null if the reader does not appear to contain this kind of token at the current location - while leaving the reader where it is</item>
            ///   <item>return a parsed token instance, advancing the reader to just after the parsed token</item>
            ///   <item>throw a <see cref="LexException"/> with a detailed description of the problem</item>
            /// </list>
            /// </summary>
            public abstract Token ParseToken(LexReader reader);
        }
    }

    public sealed class EndOfFileToken : Token
    {
        public EndOfFileToken(LexPosition location) : base(location, location) { }

        public new sealed class Parser : Token.Parser
        {
            public override Token ParseToken(LexReader reader)
            {
                if (reader.EndOfFile())
                    return new EndOfFileToken(reader.GetPosition());
                else
                    return null;
            }
        }
    }

    public sealed class BuiltinToken : Token
    {
        public string Builtin { get; private set; }

        public BuiltinToken(LexPosition start, LexPosition end, string operator_)
            : base(start, end)
        {
            Builtin = operator_;
        }

        public new sealed class Parser : Token.Parser
        {
            private string[] _operators;

            public Parser(IEnumerable<string> operators)
            {
                _operators = operators.OrderByDescending(o => o.Length).ToArray();
            }

            public Parser(params string[] operators)
                : this((IEnumerable<string>) operators)
            {
            }

            public override Token ParseToken(LexReader reader)
            {
                foreach (var tokenstr in _operators)
                    if (reader.ContinuesWith(tokenstr))
                    {
                        var start = reader.GetPosition();
                        reader.Consume(tokenstr.Length);
                        return new BuiltinToken(start, reader.GetPosition(), tokenstr);
                    }
                return null;
            }
        }
    }

    public sealed class StringLiteralToken : Token
    {
        public string Value { get; private set; }

        public StringLiteralToken(LexPosition start, LexPosition end) : base(start, end) { }

        public new sealed class Parser : Token.Parser
        {
            public IDictionary<char, char> BasicEscapes { get; private set; }
            public IDictionary<char, Func<LexReader, string>> AdvancedEscapes { get; private set; }
            public string OpeningSequence { get; private set; }
            public string ClosingSequence { get; private set; }
            public bool EscapeClosingByDoubling { get; private set; }

            public Parser(string openingSequence, string closingSequence, bool escapeClosingByDoubling)
                : this(null, null, openingSequence, closingSequence, escapeClosingByDoubling)
            {
            }

            public Parser(IDictionary<char, char> basicEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
                : this(basicEscapes, null, openingSequence, closingSequence, escapeClosingByDoubling)
            {
            }

            public Parser(IDictionary<char, char> basicEscapes, IDictionary<char, Func<LexReader, string>> advancedEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
            {
                BasicEscapes = basicEscapes;
                AdvancedEscapes = advancedEscapes;
                OpeningSequence = openingSequence;
                ClosingSequence = closingSequence;
                EscapeClosingByDoubling = escapeClosingByDoubling;
            }

            public override Token ParseToken(LexReader reader)
            {
                var start = reader.GetPosition();
                string literal = LexUtil.LexStringLiteral(reader, BasicEscapes, AdvancedEscapes, OpeningSequence, ClosingSequence, EscapeClosingByDoubling);
                return literal == null ? null : new StringLiteralToken(start, reader.GetPosition()) { Value = literal };
            }
        }
    }

    public sealed class IdentifierToken : Token
    {
        public string Identifier { get; private set; }

        public IdentifierToken(LexPosition start, LexPosition end, string identifier)
            : base(start, end)
        {
            Identifier = identifier;
        }
    }

    public sealed class CommentToken : Token
    {
        public CommentToken(LexPosition start, LexPosition end) : base(start, end) { }
    }

    public sealed class RegexTokenParser<TToken> : Token.Parser where TToken : Token
    {
        public Regex Regex { get; private set; }
        public Func<LexPosition, LexPosition, string, TToken> Init { get; private set; }

        public RegexTokenParser(Regex regex, Func<LexPosition, LexPosition, string, TToken> init)
        {
            Regex = regex;
            Init = init;
        }

        public override Token ParseToken(LexReader reader)
        {
            var start = reader.GetPosition();
            string value = reader.ConsumeEntireMatch(Regex);
            return value == null ? null : Init(start, reader.GetPosition(), value);
        }
    }
}
