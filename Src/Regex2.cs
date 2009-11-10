using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.KitchenSink
{
    public class Regex2
    {
        public static List<Match> Matches(string input, string pattern)
        {
            return new List<Match>(Regex.Matches(input, pattern, RegexOptions.Singleline).Cast<Match>());
        }

        public static List<Match> Matches(string input, string pattern, RegexOptions options)
        {
            return new List<Match>(Regex.Matches(input, pattern, options | RegexOptions.Singleline).Cast<Match>());
        }

        public static Match Match(string input, string pattern)
        {
            return Regex.Match(input, pattern, RegexOptions.Singleline);
        }

        public static Match Match(string input, string pattern, RegexOptions options)
        {
            return Regex.Match(input, pattern, options | RegexOptions.Singleline);
        }
    }
}
