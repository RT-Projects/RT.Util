using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.Util
{
    /// <summary>
    /// Exposes a number of convenience methods to compensate for a couple of shortcomings of the built-in Regex class:
    /// namely, returning a non-generic collection of matches, and defaulting to "." not matching newlines.
    /// </summary>
    public static class Regex2
    {
        /// <summary>An easier-to-use version of <see cref="Regex.Matches(string, string)"/>.</summary>
        public static IEnumerable<Match> Matches(string input, string pattern)
        {
            return Regex.Matches(input, pattern, RegexOptions.Singleline).Cast<Match>();
        }

        /// <summary>An easier-to-use version of <see cref="Regex.Matches(string, string, RegexOptions)"/>.</summary>
        public static IEnumerable<Match> Matches(string input, string pattern, Regex2Options options)
        {
            return Regex.Matches(input, pattern, options.toStandardOpts()).Cast<Match>();
        }

        /// <summary>An easier-to-use version of <see cref="Regex.Match(string, string)"/>.</summary>
        public static Match Match(string input, string pattern)
        {
            return Regex.Match(input, pattern, RegexOptions.Singleline);
        }

        /// <summary>An easier-to-use version of <see cref="Regex.Match(string, string, RegexOptions)"/>.</summary>
        public static Match Match(string input, string pattern, Regex2Options options)
        {
            return Regex.Match(input, pattern, options.toStandardOpts());
        }

        private static RegexOptions toStandardOpts(this Regex2Options opts)
        {
            return (RegexOptions) ((int) opts ^ (int) RegexOptions.Singleline); // simply toggle the Singleline option
        }
    }

    /// <summary>Provides enumerated values to use to set regular expression options.</summary>
    [Flags]
    public enum Regex2Options
    {
        /// <summary>Specifies that no options are set.</summary>
        None = 0,
        /// <summary>Specifies case-insensitive matching.</summary>
        IgnoreCase = 1,
        /// <summary>Changes the meaning of ^ and $ so they match at the beginning
        /// and end, respectively, of any line, and not just the beginning and end of
        /// the entire string.</summary>
        NewlineMatchedAtHatAndDollar = 2,
        /// <summary>Changes the meaning of the dot (.) so it matches every character except \n.</summary>
        NewlineNotMatchedByDot = 16,
        /// <summary>Specifies that the only valid captures are explicitly named or numbered groups
        /// of the form (?<name>…). This allows unnamed parentheses to act as noncapturing
        /// groups without the syntactic clumsiness of the expression (?:…).</summary>
        ExplicitCapture = 4,
        /// <summary>Specifies that the regular expression is compiled to an assembly. This yields
        /// faster execution but increases startup time.</summary>
        Compiled = 8,
        /// <summary>Eliminates unescaped white space from the pattern and enables comments marked
        /// with #. However, the System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace
        /// value does not affect or eliminate white space in character classes.</summary>
        IgnorePatternWhitespace = 32,
        /// <summary>Specifies that the search will be from right to left instead of from left to right.</summary>
        RightToLeft = 64,
        /// <summary>Specifies that cultural differences in language is ignored.</summary>
        CultureInvariant = 512,
    }
}
