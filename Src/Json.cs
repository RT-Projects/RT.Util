using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace RT.Util.Json
{
    /// <summary>Represents a JSON parsing exception.</summary>
    public class JsonParseException : Exception
    {
        private JsonParserState _state;

        internal JsonParseException(JsonParserState ps, string message)
            : base(message)
        {
            _state = ps.Clone();
        }

        /// <summary>Gets the line number at which the parse error occurred.</summary>
        public int Line { get { return _state.OffsetConverter.GetLine(_state.Pos); } }
        /// <summary>Gets the column number at which the parse error occurred.</summary>
        public int Column { get { return _state.OffsetConverter.GetColumn(_state.Pos); } }
        /// <summary>A snippet of the JSON string at which the parse error occurred.</summary>
        public string Snippet { get { return _state.Snippet; } }
    }

    /// <summary>Represents one of the JSON symbols for parsing purposes.</summary>
    internal enum Sym { DictStart, ListStart, StringStart, Number, Letter, Other, EOF }

    /// <summary>Keeps track of the JSON parser state.</summary>
    internal class JsonParserState
    {
        public string Json;
        public int Pos;

        private OffsetToLineCol _offsetConverter;
        public OffsetToLineCol OffsetConverter { get { if (_offsetConverter == null) _offsetConverter = new OffsetToLineCol(Json); return _offsetConverter; } }

        private JsonParserState() { }

        public JsonParserState(string json)
        {
            Json = json;
            Pos = 0;
            ConsumeWhitespace();
        }

        public JsonParserState Clone()
        {
            var result = new JsonParserState();
            result.Json = Json;
            result.Pos = Pos;
            result._offsetConverter = _offsetConverter;
            return result;
        }

        public void ConsumeWhitespace()
        {
            while (Pos < Json.Length && " \t\r\n".Contains(Json[Pos]))
                Pos++;
        }

        public char Cur { get { return Json[Pos]; } }

        public Sym CurSym
        {
            get
            {
                if (Pos >= Json.Length) return Sym.EOF;
                char cur = Cur;
                if (cur == '{') return Sym.DictStart;
                else if (cur == '[') return Sym.ListStart;
                else if (cur == '"' || cur == '\'') return Sym.StringStart;
                else if (cur >= '0' && cur <= '9' || cur == '.' || cur == '-') return Sym.Number;
                else if (cur >= 'A' && cur <= 'Z' || cur >= 'a' && cur <= 'z') return Sym.Letter;
                else return Sym.Other;
            }
        }

        public string Snippet
        {
            get
            {
                int line, col;
                OffsetConverter.GetLineAndColumn(Pos, out line, out col);
                return "[" + line + "," + col + "] " + Json.SubstringSafe(Pos, 15);
            }
        }

        public override string ToString()
        {
            return Snippet;
        }
    }

    /// <summary>Represents a JSON value.</summary>
    public abstract class JsonValue
    {
        internal static JsonValue ParseValue(JsonParserState ps)
        {
            switch (ps.CurSym)
            {
                case Sym.DictStart: return new JsonDict(ps);
                case Sym.ListStart: return new JsonList(ps);
                case Sym.StringStart: return new JsonString(ps);
                case Sym.Number: return new JsonNumber(ps);
                case Sym.Letter:
                    string word = Regex.Match(ps.Json.Substring(ps.Pos), @"^\w+").Captures[0].Value;
                    if (word == "true" || word == "false") return new JsonBool(ps);
                    else if (word == "null") return new JsonNull(ps);
                    else throw new JsonParseException(ps, "unknown keyword: \"{0}\"".Fmt(word));
                case Sym.Other: throw new JsonParseException(ps, "unexpected character");
                case Sym.EOF: throw new JsonParseException(ps, "unexpected end of input");
            }
            throw new JsonParseException(ps, "internal error");
        }

        /// <summary>Treats the value as a <see cref="JsonDict"/>, and gets the value with the specified key.</summary>
        public JsonValue this[string key]
        {
            get
            {
                if (this is JsonDict) return ((JsonDict) this).Value[key];
                else throw new Exception("This JSON value is not a dict");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonList"/>, and gets the value with the specified key.</summary>
        public JsonValue this[int index]
        {
            get
            {
                if (this is JsonList) return ((JsonList) this).Value[index];
                else throw new Exception("This value is not a list");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonDict"/>, and gets the underlying dictionary of values.</summary>
        public Dictionary<string, JsonValue> AsDict
        {
            get
            {
                if (this is JsonDict) return ((JsonDict) this).Value;
                else throw new Exception("This JSON value is not a dict");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonList"/>, and gets the underlying list of values.</summary>
        public List<JsonValue> AsList
        {
            get
            {
                if (this is JsonList) return ((JsonList) this).Value;
                else throw new Exception("This JSON value is not a list");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonBool"/>, and gets the underlying boolean value.</summary>
        public bool AsBool
        {
            get
            {
                if (this is JsonBool) return ((JsonBool) this).Value;
                else throw new Exception("This JSON value is not a bool");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonNumber"/>, and gets the underlying numeric value.</summary>
        public decimal AsNumber
        {
            get
            {
                if (this is JsonNumber) return ((JsonNumber) this).Value;
                else throw new Exception("This JSON value is not a number");
            }
        }

        /// <summary>Treats the value as a <see cref="JsonString"/>, and gets the underlying string value.</summary>
        public string AsString
        {
            get
            {
                if (this is JsonString) return ((JsonString) this).Value;
                else throw new Exception("This JSON value is not a string");
            }
        }
    }

    /// <summary>Represents a JSON "null" value.</summary>
    public class JsonNull : JsonValue
    {
        internal JsonNull(JsonParserState ps)
        {
            if (Regex.IsMatch(ps.Json.Substring(ps.Pos), @"^null\b"))
            {
                ps.Pos += 4;
                ps.ConsumeWhitespace();
            }
            else
                throw new JsonParseException(ps, "not a 'null'");
        }
    }

    /// <summary>Represents a JSON boolean value.</summary>
    public class JsonBool : JsonValue
    {
        private bool _value;
        /// <summary>Gets the underlying bool value.</summary>
        public bool Value { get { return _value; } }

        internal JsonBool(JsonParserState ps)
        {
            if (Regex.IsMatch(ps.Json.Substring(ps.Pos), @"^true\b"))
            {
                _value = true;
                ps.Pos += 4;
                ps.ConsumeWhitespace();
            }
            else if (Regex.IsMatch(ps.Json.Substring(ps.Pos), @"^false\b"))
            {
                _value = false;
                ps.Pos += 5;
                ps.ConsumeWhitespace();
            }
            else
                throw new JsonParseException(ps, "not a bool");
        }
    }

    /// <summary>Represents a JSON numeric value.</summary>
    public class JsonNumber : JsonValue
    {
        private decimal _value;
        /// <summary>Gets the underlying decimal value.</summary>
        public decimal Value { get { return _value; } }

        internal JsonNumber(JsonParserState ps)
        {
            StringBuilder sb = new StringBuilder();
            while (ps.CurSym == Sym.Number || ps.Cur == 'e')
            {
                sb.Append(ps.Cur);
                ps.Pos++;
            }
            if (!decimal.TryParse(sb.ToString(), out _value))
            {
                double d;
                if (!double.TryParse(sb.ToString(), out d))
                    throw new JsonParseException(ps, "not a number");
                _value = (decimal) d;
            }
            ps.ConsumeWhitespace();
        }
    }

    /// <summary>Represents a JSON string value.</summary>
    public class JsonString : JsonValue
    {
        private string _value;
        /// <summary>Gets the underlying string value.</summary>
        public string Value { get { return _value; } }

        internal JsonString(JsonParserState ps)
        {
            StringBuilder sb = new StringBuilder();
            if (ps.CurSym != Sym.StringStart)
                throw new JsonParseException(ps, "not a string");
            var quotechar = ps.Cur;
            ps.Pos++;
            while (ps.Pos < ps.Json.Length && ps.Cur != quotechar)
            {
                if (ps.Cur == '\\')
                {
                    ps.Pos++;
                    if (ps.Pos >= ps.Json.Length)
                        throw new JsonParseException(ps, "unexpected end of string");
                    switch (ps.Cur)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\'': sb.Append('\''); break;
                        default: sb.Append(ps.Cur); break;
                    }
                }
                else
                {
                    sb.Append(ps.Cur);
                }
                ps.Pos++;
            }
            if (ps.Cur != quotechar)
                throw new JsonParseException(ps, "unexpected end of string");
            ps.Pos++;
            ps.ConsumeWhitespace();
            _value = sb.ToString();
        }
    }

    /// <summary>Represents a JSON list value - which is a sequence of JSON values.</summary>
    public class JsonList : JsonValue
    {
        private List<JsonValue> _value;
        /// <summary>Gets the underlying list of values.</summary>
        public List<JsonValue> Value { get { return _value; } }

        internal JsonList(JsonParserState ps)
        {
            _value = new List<JsonValue>();
            if (ps.CurSym != Sym.ListStart)
                throw new JsonParseException(ps, "not a list");
            ps.Pos++;
            ps.ConsumeWhitespace();
            while (ps.Cur != ']')
            {
                _value.Add(JsonValue.ParseValue(ps));
                if (ps.Cur == ',')
                {
                    ps.Pos++;
                    ps.ConsumeWhitespace();
                }
            }
            ps.Pos++;
            ps.ConsumeWhitespace();
        }
    }

    /// <summary>Represents a JSON dictionary value - which is a mapping from strings to JSON values (case-sensitive).</summary>
    public class JsonDict : JsonValue
    {
        private Dictionary<string, JsonValue> _value;
        /// <summary>Gets the underlying dictionary of values.</summary>
        public Dictionary<string, JsonValue> Value { get { return _value; } }

        internal JsonDict(JsonParserState ps)
        {
            _value = new Dictionary<string, JsonValue>();
            if (ps.CurSym != Sym.DictStart)
                throw new JsonParseException(ps, "not a dict");
            ps.Pos++;
            ps.ConsumeWhitespace();
            while (ps.Cur != '}')
            {
                var name = new JsonString(ps);
                if (ps.Cur != ':')
                    throw new JsonParseException(ps, "expected :");
                ps.Pos++;
                ps.ConsumeWhitespace();
                _value.Add(name.Value, JsonValue.ParseValue(ps));
                if (ps.Cur == ',')
                {
                    ps.Pos++;
                    ps.ConsumeWhitespace();
                }
            }
            ps.Pos++;
            ps.ConsumeWhitespace();
        }
    }

    /// <summary>Offers methods to parse JSON.</summary>
    public static class Json
    {
        /// <summary>Parses the specified string as a JSON string.</summary>
        /// <param name="json">JSON string to parse.</param>
        /// <returns>A parsed representation of the string.</returns>
        public static JsonValue Parse(string json)
        {
            var ps = new JsonParserState(json);
            var result = JsonValue.ParseValue(ps);
            if (ps.CurSym != Sym.EOF)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }
    }
}
