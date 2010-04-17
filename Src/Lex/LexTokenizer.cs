using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.KitchenSink.Collections;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex
{
    public class LexTokenizer : LazyToCollection<Token>
    {
        public LexTokenizer(LexReader reader, IEnumerable<Token.Parser> tokenParsers, ICollection<Type> ignoreTokens, Type stopToken, bool swallowWhitespace)
            : base(makeEnumerable(reader, tokenParsers, ignoreTokens, stopToken, swallowWhitespace))
        {
            if (!IndexInRange(0))
                throw new Exception("There are no tokens in the token stream.");
        }

        private static IEnumerable<Token> makeEnumerable(LexReader reader, IEnumerable<Token.Parser> tokenParsers, ICollection<Type> ignoreTokens, Type stopToken, bool swallowWhitespace)
        {
            while (true)
            {
                if (swallowWhitespace)
                    reader.ConsumeAnyWhitespace();
                var token = tokenParsers.Select(p => p.ParseToken(reader)).FirstOrDefault(t => t != null);
                if (token == null)
                    throw new LexException(reader.GetPosition(), "Unrecognized sequence of characters.");
                var type = token.GetType();
                if (type == stopToken)
                    yield break;
                else if (!ignoreTokens.Contains(type))
                    yield return token;
            }
        }
    }
}
