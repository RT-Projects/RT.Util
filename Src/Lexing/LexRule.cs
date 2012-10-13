using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.KitchenSink.Lexing
{
    public sealed class LexRule
    {
        public string RegexString { get; private set; }
        public Func<string, Token> StringSelector { get; private set; }
        public Func<Match, Token> MatchSelector { get; private set; }
        public LexRule(string regex, Func<string, Token> stringSelector)
        {
            if (Regex.IsMatch(regex, @"\(\?<r\d+>"))
                throw new ArgumentException(@"The regular expression may not use a capturing group called ""r"" plus a number (e.g. ""r1""). Consider using any other name for the group.");
            if (stringSelector == null)
                throw new ArgumentNullException("groupSelector");

            StringSelector = stringSelector;
            RegexString = regex;
        }

        public LexRule(string regex, Func<Match, Token> matchSelector)
        {
            if (Regex.IsMatch(regex, @"\(\?<r\d+>"))
                throw new ArgumentException(@"The regular expression may not use a capturing group called ""r"" plus a number (e.g. ""r1""). Consider using any other name for the group.");
            if (matchSelector == null)
                throw new ArgumentNullException("matchSelector");

            MatchSelector = matchSelector;
            RegexString = regex;
        }
    }
}
