using RT.Util.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.KitchenSink.Lexing
{
    public static class Lexer
    {
        public static IEnumerable<Token> Lex(string source, params LexRule[] rules)
        {
            var uberRegex = rules.Select((rule, index) => "(?<r{0}>{1})".Fmt(index + 1, rule.RegexString)).JoinString("|") + "|(?<r0>.)";
            return Regex.Matches(source, uberRegex, RegexOptions.Compiled).Cast<Match>().Select(match =>
            {
                var ruleIndex = Enumerable.Range(1, rules.Length).FirstOrDefault(i => match.Groups["r" + i].Success);
                if (ruleIndex == 0)
                    throw new LexException(new SourceSpan(source, match.Groups["r0"].Index, match.Groups["r0"].Length), "Unexpected input.");
                var group = match.Groups["r" + ruleIndex];
                var token = rules[ruleIndex - 1].MatchSelector != null
                    ? rules[ruleIndex - 1].MatchSelector(match)
                    : rules[ruleIndex - 1].StringSelector(group.Value);
                if (token == null)
                    return null;
                token.OriginalSource = source;
                token.Index = group.Index;
                token.Length = group.Length;
                return token;
            }).Where(token => token != null);
        }

        public static IEnumerable<Token> Lex(string source, IEnumerable<LexRule> rules)
        {
            return Lex(source, rules.ToArray());
        }
    }
}
