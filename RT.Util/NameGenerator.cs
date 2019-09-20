
namespace RT.Util
{
    /// <summary>Generates a sequence of short names starting with “a” through “z”, followed by two-letter names etc.</summary>
    public sealed class NameGenerator
    {
        /// <summary>Constructor.</summary>
        public NameGenerator() { _curname = ""; }

        /// <summary>Generates the next name in the sequence.</summary>
        public string NextName()
        {
            _curname = nextName(_curname);
            return _curname;
        }

        private string _curname;

        private static string nextName(string prev)
        {
            if (string.IsNullOrEmpty(prev))
                return "a";
            if (prev.Length == 1)
            {
                if (prev[0] == 'z')
                    return "aa";
                else
                    return ((char) (prev[0] + 1)).ToString();
            }
            else
            {
                if (prev[prev.Length - 1] == 'z')
                    return prev.Substring(0, prev.Length - 1) + "0";
                else if (prev[prev.Length - 1] == '9')
                    return nextName(prev.Substring(0, prev.Length - 1)) + "a";
                else
                    return prev.Substring(0, prev.Length - 1) + (char) (prev[prev.Length - 1] + 1);
            }
        }
    }
}
