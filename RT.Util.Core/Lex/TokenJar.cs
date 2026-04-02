using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public sealed class TokenJar(LexTokenizer tokenizer)
{
    public int CurIndex { get; private set; }

    public Token CurToken
    {
        get
        {
            if (!tokenizer.IndexInRange(CurIndex))
                throw new ParseException("Expected token but found end of token stream.");
            return tokenizer[CurIndex];
        }
    }

    public bool CurTokenIs<TToken>(Func<TToken, bool> verify) where TToken : Token => CurToken is TToken tok && verify(tok);

    public Token ConsumeToken()
    {
        var tok = CurToken;
        CurIndex++;
        return tok;
    }

    public TToken ConsumeToken<TToken>() where TToken : Token
    {
        var tok = CurToken;
        if (tok is TToken token)
        {
            CurIndex++;
            return token;
        }
        else
            throw new ParseException("Expected a {0}, found a {1}".Fmt(typeof(TToken).Name, tok.GetType().Name));
    }

    public TToken ConsumeToken<TToken>(Func<TToken, string> verify) where TToken : Token
    {
        var tok = ConsumeToken<TToken>();
        string error = verify(tok);
        if (error == null)
            return tok;
        else
            throw new ParseException(error);
    }
}

public sealed class ParseException(string message) : Exception(message)
{
}
