using System.Collections.Generic;

namespace RT.KitchenSink.ParseCs
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
    
    public enum TokenType
    {
        Builtin,
        Identifier,
        StringLiteral,
        CharacterLiteral,
        NumberLiteral,
        CommentSlashSlash,
        CommentSlashStar,
        PreprocessorDirective,
        EndOfFile
    }

    public sealed class Token
    {
        private string _str;
        private TokenType _type;
        private int _index;
        public Token(string str, TokenType type, int index) { _str = str; _type = type; _index = index; }
        public string TokenStr { get { return _str; } }
        public TokenType Type { get { return _type; } }
        public int Index { get { return _index; } }

        public bool IsBuiltin(string name) { return _type == TokenType.Builtin && _str.Equals(name); }
        public bool IsIdentifier(string name) { return _type == TokenType.Identifier && _str.Equals(name); }

        public string Identifier()
        {
            return Identifier(@"Identifier expected.");
        }

        public string Identifier(string errorMessage)
        {
            if (_type != TokenType.Identifier)
                throw new ParseException(errorMessage, _index);
            return _str;
        }

        public void Assert(string token)
        {
            if (!_str.Equals(token))
                throw new ParseException(@"Assertion failed.", _index);
        }

        public override string ToString()
        {
            return _type.ToString() + (_str != null ? ": " + _str : "");
        }
    }

    public sealed class TokenJar
    {
        private IEnumerator<Token> _enumerator;
        private Token _endToken;

        public TokenJar(IEnumerable<Token> enumerable, Token endToken)
        {
            _enumerator = enumerable.GetEnumerator();
            _endToken = endToken;
        }

        private List<Token> _list;
        public Token this[int index]
        {
            get
            {
                if (_list == null)
                    _list = new List<Token>();
                while (_list.Count <= index)
                {
                    if (!_enumerator.MoveNext())
                        return _endToken;
                    _list.Add(_enumerator.Current);
                }
                return _list[index];
            }
        }
        public bool IndexExists(int index)
        {
            if (_list == null)
                _list = new List<Token>();
            if (_list.Count > index && _list[index].Type != TokenType.EndOfFile)
                return true;
            try
            {
                var token = this[index];
                return token.Type != TokenType.EndOfFile;
            }
            catch (ParseException) { return false; }
        }
        public bool Has(char c, int index)
        {
            return this[index].Type == TokenType.Builtin && this[index].TokenStr[0] == c;
        }
        public void Split(int index)
        {
            var oldToken = this[index];
            if (oldToken.TokenStr.Length < 2)
                return;
            _list.Insert(index + 1, new Token(oldToken.TokenStr.Substring(1), TokenType.Builtin, oldToken.Index + 1));
            _list[index] = new Token(oldToken.TokenStr.Substring(0, 1), TokenType.Builtin, oldToken.Index);
        }
    }
}
