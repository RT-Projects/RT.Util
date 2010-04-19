using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex
{
    public class TokenJar
    {
        private LexTokenizer _tok;

        public TokenJar(LexTokenizer tokenizer)
        {
            _tok = tokenizer;
        }

        public int CurIndex { get; private set; }

        public Token CurToken
        {
            get
            {
                if (!_tok.IndexInRange(CurIndex))
                    throw new ParseException("Expected token but found end of token stream.");
                return _tok[CurIndex];
            }
        }

        public bool CurTokenIs<TToken>(Func<TToken, bool> verify) where TToken : Token
        {
            var tok = CurToken as TToken;
            if (tok == null)
                return false;
            return verify(tok);
        }

        public Token ConsumeToken()
        {
            var tok = CurToken;
            CurIndex++;
            return tok;
        }

        public TToken ConsumeToken<TToken>() where TToken : Token
        {
            var tok = CurToken;
            if (tok is TToken)
            {
                CurIndex++;
                return (TToken) tok;
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

    //public class SwitchTokenCase<T>
    //{
    //    public string Description { get; private set; }
    //    public Func<T, bool> MatchTest { get; private set; }
    //    public Action<T> ActionOnMatch { get; private set; }

    //    public SwitchTokenCase(string description, Func<T, bool> matchTest, Action<T> actionOnMatch)
    //    {
    //        Description = description;
    //        MatchTest = matchTest;
    //        ActionOnMatch = actionOnMatch;
    //    }
    //}

    public class ParseException : Exception
    {
        public ParseException(string message)
            : base(message)
        {
        }
    }
}
