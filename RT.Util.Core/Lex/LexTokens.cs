using System.Text.RegularExpressions;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public abstract class Token(LexPosition start, LexPosition end)
{
    public LexPosition StartLocation { get; private set; } = start;
    public LexPosition EndLocation { get; private set; } = end;

    public abstract class Parser
    {
        /// <summary>
        ///     <para>
        ///         Parses the next token of a specific kind from the lex reader. Implementations must do one of the
        ///         following:</para>
        ///     <list type="bullet">
        ///         <item>return null if the reader does not appear to contain this kind of token at the current location -
        ///         while leaving the reader where it is</item>
        ///         <item>return a parsed token instance, advancing the reader to just after the parsed token</item>
        ///         <item>throw a <see cref="LexException"/> with a detailed description of the problem</item></list></summary>
        public abstract Token ParseToken(LexReader reader);
    }
}

public sealed class EndOfFileToken(LexPosition location) : Token(location, location)
{
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

public sealed class BuiltinToken(LexPosition start, LexPosition end, string @operator) : Token(start, end)
{
    public string Builtin { get; private set; } = @operator;

    public new sealed class Parser(IEnumerable<string> operators) : Token.Parser
    {
        private string[] _operators = operators.OrderByDescending(o => o.Length).ToArray();

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

public sealed class StringLiteralToken(LexPosition start, LexPosition end) : Token(start, end)
{
    public string Value { get; private set; }

    public new sealed class Parser(IDictionary<char, char> basicEscapes, IDictionary<char, Func<LexReader, string>> advancedEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling) : Token.Parser
    {
        public IDictionary<char, char> BasicEscapes { get; private set; } = basicEscapes;
        public IDictionary<char, Func<LexReader, string>> AdvancedEscapes { get; private set; } = advancedEscapes;
        public string OpeningSequence { get; private set; } = openingSequence;
        public string ClosingSequence { get; private set; } = closingSequence;
        public bool EscapeClosingByDoubling { get; private set; } = escapeClosingByDoubling;

        public Parser(string openingSequence, string closingSequence, bool escapeClosingByDoubling)
            : this(null, null, openingSequence, closingSequence, escapeClosingByDoubling)
        {
        }

        public Parser(IDictionary<char, char> basicEscapes, string openingSequence, string closingSequence, bool escapeClosingByDoubling)
            : this(basicEscapes, null, openingSequence, closingSequence, escapeClosingByDoubling)
        {
        }

        public override Token ParseToken(LexReader reader)
        {
            var start = reader.GetPosition();
            string literal = LexUtil.LexStringLiteral(reader, BasicEscapes, AdvancedEscapes, OpeningSequence, ClosingSequence, EscapeClosingByDoubling);
            return literal == null ? null : new StringLiteralToken(start, reader.GetPosition()) { Value = literal };
        }
    }
}

public sealed class IdentifierToken(LexPosition start, LexPosition end, string identifier) : Token(start, end)
{
    public string Identifier { get; private set; } = identifier;
}

public sealed class CommentToken(LexPosition start, LexPosition end) : Token(start, end)
{
}

public sealed class RegexTokenParser<TToken>(Regex regex, Func<LexPosition, LexPosition, string, TToken> init) : Token.Parser where TToken : Token
{
    public Regex Regex { get; private set; } = regex;
    public Func<LexPosition, LexPosition, string, TToken> Init { get; private set; } = init;

    public override Token ParseToken(LexReader reader)
    {
        var start = reader.GetPosition();
        string value = reader.ConsumeEntireMatch(Regex);
        return value == null ? null : Init(start, reader.GetPosition(), value);
    }
}
