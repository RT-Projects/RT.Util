using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

#pragma warning disable 1591

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

        public char? Cur { get { return Pos >= Json.Length ? null : (char?) Json[Pos]; } }

        public string Snippet
        {
            get
            {
                int line, col;
                OffsetConverter.GetLineAndColumn(Pos, out line, out col);
                return "Before: {2}   After: {3}   At: {0},{1}".Fmt(line, col, Json.SubstringSafe(Pos - 15, 15), Json.SubstringSafe(Pos, 15));
            }
        }

        public override string ToString()
        {
            return Snippet;
        }

        public JsonValue ParseValue()
        {
            var cn = Cur;
            switch (cn)
            {
                case null: throw new JsonParseException(this, "unexpected end of input");
                case '{': return ParseDict();
                case '[': return ParseList();
                case '"': return ParseString();
                default:
                    var c = Cur.Value;
                    if (c == '-' || (c >= '0' && c <= '9'))
                        return ParseNumber();
                    else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
                        return parseWord();
                    else
                        throw new JsonParseException(this, "unexpected character");
            }
        }

        private JsonValue parseWord()
        {
            string word = Regex.Match(Json.Substring(Pos), @"^\w+").Captures[0].Value;
            if (word == "true" || word == "false") return ParseBool();
            else if (word == "null") { Pos += 4; ConsumeWhitespace(); return null; }
            else throw new JsonParseException(this, "unknown keyword: \"{0}\"".Fmt(word));
        }

        public JsonString ParseString()
        {
            var sb = new StringBuilder();
            if (Cur != '"')
                throw new JsonParseException(this, "expected a string");
            Pos++;
            while (true)
            {
                switch (Cur)
                {
                    case null: throw new JsonParseException(this, "unexpected end of string");
                    case '"': Pos++; goto while_break; // break out of the while... argh.
                    case '\\':
                        {
                            Pos++;
                            switch (Cur)
                            {
                                case null: throw new JsonParseException(this, "unexpected end of string");
                                case '"': sb.Append('"'); break;
                                case '\\': sb.Append('\\'); break;
                                case '/': sb.Append('/'); break;
                                case 'b': sb.Append('\b'); break;
                                case 'f': sb.Append('\f'); break;
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                case 'u':
                                    var hex = Json.SubstringSafe(Pos + 1, 4);
                                    if (hex.Length != 4)
                                        throw new JsonParseException(this, "unexpected end of a \\u escape sequence");
                                    int code;
                                    if (!int.TryParse(hex, NumberStyles.AllowHexSpecifier, null, out code))
                                        throw new JsonParseException(this, "expected a four-digit hex code");
                                    sb.Append((char) code);
                                    Pos += 4;
                                    break;
                                default:
                                    throw new JsonParseException(this, "unknown escape sequence");
                            }
                        }
                        break;
                    default:
                        sb.Append(Cur.Value);
                        break;
                }
                Pos++;
            }
            while_break: ;
            ConsumeWhitespace();
            return new JsonString(sb.ToString());
        }

        public JsonBool ParseBool()
        {
            JsonBool result;
            if (Regex.IsMatch(Json.Substring(Pos), @"^true\b"))
            {
                result = true;
                Pos += 4;
                ConsumeWhitespace();
            }
            else if (Regex.IsMatch(Json.Substring(Pos), @"^false\b"))
            {
                result = false;
                Pos += 5;
                ConsumeWhitespace();
            }
            else
                throw new JsonParseException(this, "expected a bool");
            return result;
        }

        public JsonNumber ParseNumber()
        {
            int fromPos = Pos;

            if (Cur == '-') // optional minus
                Pos++;

            if (Cur == '0') // either a single zero...
                Pos++;
            else if (Cur >= '1' && Cur <= '9') // ...or a non-zero followed by any number of digits
                while (Cur >= '0' && Cur <= '9')
                    Pos++;
            else
                throw new JsonParseException(this, "expected a single zero or a sequence of digits starting with a non-zero.");

            if (Cur == '.') // a decimal point followed by at least one digit
            {
                Pos++;
                if (!(Cur >= '0' && Cur <= '9'))
                    throw new JsonParseException(this, "expected at least one digit following the decimal point");
                while (Cur >= '0' && Cur <= '9')
                    Pos++;
            }

            if (Cur == 'e' || Cur == 'E')
            {
                Pos++;
                if (Cur == '+' || Cur == '-') // optional plus/minus
                    Pos++;
                if (!(Cur >= '0' && Cur <= '9'))
                    throw new JsonParseException(this, "expected at least one digit following the exponent letter");
                while (Cur >= '0' && Cur <= '9')
                    Pos++;
            }

            JsonNumber result;
            string number = Json.Substring(fromPos, Pos - fromPos);
            long lng;
            if (!long.TryParse(number, out lng))
            {
                double dbl;
                if (!double.TryParse(number, out dbl))
                    throw new JsonParseException(this, "expected a number");
                result = dbl;
            }
            else
                result = lng;
            ConsumeWhitespace();
            return result;
        }

        public JsonList ParseList()
        {
            var result = new JsonList();
            if (Cur != '[')
                throw new JsonParseException(this, "expected a list");
            Pos++;
            ConsumeWhitespace();
            while (true)
            {
                if (Cur == null)
                    throw new JsonParseException(this, "unexpected end of list");
                if (Cur == ']')
                    break;
                result.Add(ParseValue());
                if (Cur == null)
                    throw new JsonParseException(this, "unexpected end of dict");
                if (Cur == ',')
                {
                    Pos++;
                    ConsumeWhitespace();
                    if (Cur == ']')
                        throw new JsonParseException(this, "a list can't end with a comma");
                }
                else if (Cur != ']')
                    throw new JsonParseException(this, "expected a comma to separate list items");
            }
            Pos++;
            ConsumeWhitespace();
            return result;
        }

        public JsonDict ParseDict()
        {
            var result = new JsonDict();
            if (Cur != '{')
                throw new JsonParseException(this, "expected a dict");
            Pos++;
            ConsumeWhitespace();
            while (true)
            {
                if (Cur == null)
                    throw new JsonParseException(this, "unexpected end of dict");
                if (Cur == '}')
                    break;
                var name = ParseString();
                if (Cur != ':')
                    throw new JsonParseException(this, "expected a colon to separate dict key/value");
                Pos++;
                ConsumeWhitespace();
                if (Cur == null)
                    throw new JsonParseException(this, "unexpected end of dict");
                result.Add(name, ParseValue());
                if (Cur == null)
                    throw new JsonParseException(this, "unexpected end of dict");
                if (Cur == ',')
                {
                    Pos++;
                    ConsumeWhitespace();
                    if (Cur == '}')
                        throw new JsonParseException(this, "a dict can't end with a comma");
                }
                else if (Cur != '}')
                    throw new JsonParseException(this, "expected a comma to separate dict entries");
            }
            Pos++;
            ConsumeWhitespace();
            return result;
        }
    }

    public abstract class JsonValue : IEquatable<JsonValue>
    {
        public static JsonValue Parse(string jsonValue)
        {
            var ps = new JsonParserState(jsonValue);
            var result = ps.ParseValue();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonValue, out JsonValue result)
        {
            try
            {
                result = Parse(jsonValue);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public static implicit operator JsonValue(string value) { return value == null ? null : new JsonString(value); }
        public static implicit operator JsonValue(bool value) { return new JsonBool(value); }
        public static implicit operator JsonValue(bool? value) { return value == null ? null : new JsonBool(value.Value); }
        public static implicit operator JsonValue(double value) { return new JsonNumber(value); }
        public static implicit operator JsonValue(double? value) { return value == null ? null : new JsonNumber(value.Value); }
        public static implicit operator JsonValue(long value) { return new JsonNumber(value); }
        public static implicit operator JsonValue(long? value) { return value == null ? null : new JsonNumber(value.Value); }
        public static implicit operator JsonValue(int value) { return new JsonNumber(value); }
        public static implicit operator JsonValue(int? value) { return value == null ? null : new JsonNumber(value.Value); }

        public static explicit operator string(JsonValue value) { return value.AsString; }
        public static explicit operator bool(JsonValue value) { return value.AsBool; }
        public static explicit operator bool?(JsonValue value) { return value == null ? (bool?) null : value.AsBool; }
        public static explicit operator double(JsonValue value) { return value.AsDouble; }
        public static explicit operator double?(JsonValue value) { return value == null ? (double?) null : value.AsDouble; }
        public static explicit operator long(JsonValue value) { return value.AsLong; }
        public static explicit operator long?(JsonValue value) { return value == null ? (long?) null : value.AsLong; }
        public static explicit operator int(JsonValue value) { return value.AsInt; }
        public static explicit operator int?(JsonValue value) { return value == null ? (int?) null : value.AsInt; }

        public JsonList AsList
        {
            get
            {
                var v = this as JsonList;
                if (v == null)
                    throw new NotSupportedException("Only list values can be interpreted as list.");
                return v;
            }
        }

        public JsonDict AsDict
        {
            get
            {
                var v = this as JsonDict;
                if (v == null)
                    throw new NotSupportedException("Only dict values can be interpreted as dict.");
                return v;
            }
        }

        public string AsString
        {
            get
            {
                var v = this as JsonString;
                if (v == null)
                    throw new NotSupportedException("Only string values can be interpreted as string.");
                return v;
            }
        }

        public bool AsBool
        {
            get
            {
                var v = this as JsonBool;
                if (v == null)
                    throw new NotSupportedException("Only bool values can be interpreted as bool.");
                return v;
            }
        }

        public double AsDouble
        {
            get
            {
                var v = this as JsonNumber;
                if (v == null)
                    throw new NotSupportedException("Only numeric values can be interpreted as double.");
                return (double) v;
            }
        }

        public long AsLong
        {
            get
            {
                var v = this as JsonNumber;
                if (v == null)
                    throw new NotSupportedException("Only numeric values can be interpreted as long.");
                return (long) v;
            }
        }

        public int AsInt
        {
            get
            {
                var v = this as JsonNumber;
                if (v == null)
                    throw new NotSupportedException("Only numeric values can be interpreted as int.");
                return (int) v;
            }
        }

        #region Both IList and IDictionary

        public void Clear()
        {
            if (this is JsonList)
                (this as JsonList).List.Clear();
            else if (this is JsonDict)
                (this as JsonDict).Dict.Clear();
            else
                throw new NotSupportedException("This method is only supported on dictionary and list values.");
        }

        public int Count
        {
            get
            {
                if (this is JsonList)
                    return (this as JsonList).List.Count;
                else if (this is JsonDict)
                    return (this as JsonDict).Dict.Count;
                else
                    throw new NotSupportedException("This method is only supported on dictionary and list values.");
            }
        }

        public bool IsReadOnly { get { return !(this is JsonDict || this is JsonList); } }

        #endregion

        #region IList

        public JsonValue this[int index]
        {
            get
            {
                var list = this as JsonList;
                if (list == null)
                    throw new NotSupportedException("This method is only supported on list values.");
                return list.List[index];
            }
            set
            {
                var list = this as JsonList;
                if (list == null)
                    throw new NotSupportedException("This method is only supported on list values.");
                list.List[index] = value;
            }
        }

        public void Add(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.Add(item);
        }

        public bool Remove(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.Remove(item);
        }

        public bool Contains(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.Contains(item);
        }

        public void Insert(int index, JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.RemoveAt(index);
        }

        public int IndexOf(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.IndexOf(item);
        }

        public void CopyTo(JsonValue[] array, int arrayIndex)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.CopyTo(array, arrayIndex);
        }

        #endregion

        #region IDictionary

        public JsonValue this[string key]
        {
            get
            {
                var dict = this as JsonDict;
                if (dict == null)
                    throw new NotSupportedException("This method is only supported on dictionary values.");
                return dict.Dict[key];
            }
            set
            {
                var dict = this as JsonDict;
                if (dict == null)
                    throw new NotSupportedException("This method is only supported on dictionary values.");
                dict.Dict[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                var dict = this as JsonDict;
                if (dict == null)
                    throw new NotSupportedException("This method is only supported on dictionary values.");
                return dict.Dict.Keys;
            }
        }

        public ICollection<JsonValue> Values
        {
            get
            {
                var dict = this as JsonDict;
                if (dict == null)
                    throw new NotSupportedException("This method is only supported on dictionary values.");
                return dict.Dict.Values;
            }
        }

        public bool TryGetValue(string key, out JsonValue value)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.TryGetValue(key, out value);
        }

        public void Add(string key, JsonValue value)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            dict.Dict.Add(key, value);
        }

        public bool Remove(string key)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.Remove(key);
        }

        public bool ContainsKey(string key)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.ContainsKey(key);
        }

        #endregion

        public override bool Equals(object other)
        {
            return other is JsonValue ? Equals((JsonValue) other) : false;
        }

        public bool Equals(JsonValue other)
        {
            if (other == null) return false;
            if (this is JsonBool)
                return other is JsonBool && (this as JsonBool).Equals(other as JsonBool);
            else if (this is JsonString)
                return other is JsonString && (this as JsonString).Equals(other as JsonString);
            else if (this is JsonNumber)
                return other is JsonNumber && (this as JsonNumber).Equals(other as JsonNumber);
            else if (this is JsonList)
                return other is JsonList && (this as JsonList).Equals(other as JsonList);
            else if (this is JsonDict)
                return other is JsonDict && (this as JsonDict).Equals(other as JsonDict);
            else
                return false;
        }

        /// <summary>Always throws.</summary>
        public override int GetHashCode()
        {
            // the compiler doesn't realise that every descendant overrides GetHashCode anyway. This method
            // is just to shut it up.
            throw new NotSupportedException();
        }

        /// <summary>Converts the JSON value to a JSON string that parses back to this value. Supports null values.</summary>
        public static string ToString(JsonValue value)
        {
            return value == null ? "null" : value.ToString();
        }

        /// <summary>Converts the current JSON value to a JSON string that parses back to this value.</summary>
        public override string ToString()
        {
            return ToEnumerable().JoinString();
        }

        /// <summary>Lazy-converts the JSON value to a JSON string that parses back to this value. Supports null values.</summary>
        public static IEnumerable<string> ToEnumerable(JsonValue value)
        {
            if (value == null)
            {
                yield return "null";
                yield break;
            }
            foreach (var piece in value.ToEnumerable())
                yield return piece;
        }

        /// <summary>Lazy-converts the current JSON value to a JSON string that parses back to this value.</summary>
        public abstract IEnumerable<string> ToEnumerable();

        /// <summary>
        /// Formats JSON values into a piece of JavaScript code and then removes almost all unnecessary whitespace and comments.
        /// Values are referenced by names; placeholders for these values are written as {{name}}. Placeholders are only replaced
        /// outside of JavaScript literal strings and regexes. <see cref="JsonRaw"/> instances are inserted unmodified.
        /// </summary>
        /// <param name="js">JavaScript code with placeholders.</param>
        /// <param name="namevalues">Alternating names and associated values, for example ["user", "abc"] specifies one value named "user".</param>
        /// <example>
        /// <para>The following code:</para>
        /// <code>JsonValue.Fmt(@"Foo({{userid}}, {{username}}, {{options}});", "userid", userid, "username", username, "options", null)</code>
        /// <para>might return the following string:</para>
        /// <code>Foo(123, "Matthew Stranger", null);</code>
        /// </example>
        /// <exception cref="ArgumentException">
        /// <paramref name="namevalues"/> has an odd number of values. OR
        /// <paramref name="js"/> contains a {{placeholder}} whose name is not listed in <paramref name="namevalues"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="js"/> is null. OR <paramref name="namevalues"/> is null.</exception>
        public static string Fmt(string js, params JsonValue[] namevalues)
        {
            if (js == null)
                throw new ArgumentNullException("js");
            if (namevalues == null)
                throw new ArgumentNullException("namevalues");
            if (namevalues.Length % 2 != 0)
                throw new ArgumentException("namevalues must have an even number of values.", "namevalues");

            bool hadRaw = false;
            var str = new StringBuilder();
            foreach (Match m in Regex.Matches(js, @"
                \{\{(?<placeholder>[^\{\}]*?)\}\}|
                (?<required_whitespace>(?<=\p{L}|\p{Nd}|[_\$])\s+(?=\p{L}|\p{Nd}|[_\$])|(?<=\+)\s+(?=\+))|
                (?<comment>//[^\n]*|/\*.*?\*/)|
                '(?:[^'\\]|\\.)*'|""(?:[^""\\]|\\.)*""|(?<!(?:\p{L}|\p{Nd}|[_\)\]\}\$])\s*)/(?:[^/\\]|\\.)*/|
                .", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace))
            {
                if (m.Groups["placeholder"].Success)
                {
                    var name = m.Groups["placeholder"].Value;
                    var index = Enumerable.Range(0, namevalues.Length / 2).IndexOf(i => namevalues[2 * i].AsString == name);
                    if (index == -1)
                        throw new ArgumentException("namevalues does not contain a value named \"{0}\".".Fmt(name));
                    var raw = namevalues[2 * index + 1] as JsonRaw;
                    var value = raw == null ? JsonValue.ToString(namevalues[2 * index + 1]) : raw.Raw;
                    if (raw != null) hadRaw = true;
                    if (value.Length > 0 && (
                        ((char.IsLetter(str[str.Length - 1]) || char.IsDigit(str[str.Length - 1]) || "_$".Contains(str[str.Length - 1])) && (char.IsLetter(value[0]) || char.IsDigit(value[0]) || "_$".Contains(value[0]))) ||
                        (str[str.Length - 1] == '-' && value[0] == '-')))
                        str.Append(" ");
                    str.Append(value);
                }
                else if (m.Groups["required_whitespace"].Success)
                    str.Append(" ");
                else if (m.Groups["comment"].Success || (m.Value.Length == 1 && char.IsWhiteSpace(m.Value, 0)))
                    continue;
                else
                    str.Append(m.Value);
            }
            return hadRaw ? Fmt(str.ToString()) : str.ToString(); // the Raw value is expected to contain no placeholders.
        }
    }

    public class JsonList : JsonValue, IList<JsonValue>, IEquatable<JsonList>
    {
        internal List<JsonValue> List;

        public JsonList() { List = new List<JsonValue>(); }

        public JsonList(IEnumerable<JsonValue> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            List = new List<JsonValue>(items is ICollection<JsonValue> ? ((ICollection<JsonValue>) items).Count + 2 : 4);
            List.AddRange(items);
        }

        public static new JsonList Parse(string jsonList)
        {
            if (jsonList == null)
                throw new ArgumentNullException("jsonList");
            var ps = new JsonParserState(jsonList);
            var result = ps.ParseList();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonNumber, out JsonList result)
        {
            try
            {
                result = Parse(jsonNumber);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public IEnumerator<JsonValue> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object other)
        {
            return other is JsonList ? Equals((JsonList) other) : false;
        }

        public bool Equals(JsonList other)
        {
            if (other == null) return false;
            if (this.Count != other.Count) return false;
            return this.Zip(other, (v1, v2) => (v1 == null) == (v2 == null) && (v1 == null || v1.Equals(v2))).All(b => b);
        }

        public override int GetHashCode()
        {
            int result = 977;
            unchecked
            {
                foreach (var item in this)
                    result ^= result * 211 + (item == null ? 1979 : item.GetHashCode());
            }
            return result;
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return "[";
            bool first = true;
            foreach (var value in List)
            {
                if (!first)
                    yield return ",";
                foreach (var piece in JsonValue.ToEnumerable(value))
                    yield return piece;
                first = false;
            }
            yield return "]";
        }
    }

    public class JsonDict : JsonValue, IDictionary<string, JsonValue>, IEquatable<JsonDict>
    {
        internal Dictionary<string, JsonValue> Dict;

        public JsonDict() { Dict = new Dictionary<string, JsonValue>(); }

        public JsonDict(IEnumerable<KeyValuePair<string, JsonValue>> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            Dict = new Dictionary<string, JsonValue>(items is ICollection<KeyValuePair<string, JsonValue>> ? ((ICollection<KeyValuePair<string, JsonValue>>) items).Count + 2 : 4);
            foreach (var item in items)
                Dict.Add(item.Key, item.Value);
        }

        public static new JsonDict Parse(string jsonDict)
        {
            var ps = new JsonParserState(jsonDict);
            var result = ps.ParseDict();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonDict, out JsonDict result)
        {
            try
            {
                result = Parse(jsonDict);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
        {
            return Dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region IEnumerable<KeyValuePair> methods

        void ICollection<KeyValuePair<string, JsonValue>>.Add(KeyValuePair<string, JsonValue> item)
        {
            ((ICollection<KeyValuePair<string, JsonValue>>) Dict).Add(item);
        }
        bool ICollection<KeyValuePair<string, JsonValue>>.Remove(KeyValuePair<string, JsonValue> item)
        {
            return ((ICollection<KeyValuePair<string, JsonValue>>) Dict).Remove(item);
        }
        bool ICollection<KeyValuePair<string, JsonValue>>.Contains(KeyValuePair<string, JsonValue> item)
        {
            return ((ICollection<KeyValuePair<string, JsonValue>>) Dict).Contains(item);
        }
        void ICollection<KeyValuePair<string, JsonValue>>.CopyTo(KeyValuePair<string, JsonValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, JsonValue>>) Dict).CopyTo(array, arrayIndex);
        }

        #endregion

        public override bool Equals(object other)
        {
            return other is JsonDict && Equals((JsonDict) other);
        }

        public bool Equals(JsonDict other)
        {
            if (other == null) return false;
            if (this.Count != other.Count) return false;
            foreach (var kvp in this)
            {
                JsonValue val;
                if (!other.TryGetValue(kvp.Key, out val))
                    return false;
                if ((kvp.Value == null) != (val == null))
                    return false;
                if (kvp.Value != null && !kvp.Value.Equals(val))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = 1307;
            unchecked
            {
                foreach (var kvp in this)
                    result ^= result * 647 + (kvp.Value == null ? 1979 : kvp.Value.GetHashCode()) + kvp.Key.GetHashCode();
            }
            return result;
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return "{";
            bool first = true;
            foreach (var kvp in Dict)
            {
                if (!first)
                    yield return ",";
                yield return kvp.Key.JsEscape(JsQuotes.Double);
                yield return ":";
                foreach (var piece in JsonValue.ToEnumerable(kvp.Value))
                    yield return piece;
                first = false;
            }
            yield return "}";
        }
    }

    public class JsonString : JsonValue, IEquatable<JsonString>
    {
        private string _value;
        public JsonString(string value) { if (value == null) throw new ArgumentNullException(); _value = value; }

        public static new JsonString Parse(string jsonString)
        {
            var ps = new JsonParserState(jsonString);
            var result = ps.ParseString();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonString, out JsonString result)
        {
            try
            {
                result = Parse(jsonString);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public static implicit operator string(JsonString value) { return value == null ? null : value._value; }
        public static implicit operator JsonString(string value) { return value == null ? null : new JsonString(value); }

        public override bool Equals(object other)
        {
            return other is JsonString ? Equals((JsonString) other) : false;
        }

        public bool Equals(JsonString other)
        {
            if (other == null) return false;
            return this._value == other._value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return _value.JsEscape(JsQuotes.Double);
        }

        public string ToString(JsQuotes quotes)
        {
            return _value.JsEscape(quotes);
        }
    }

    public class JsonBool : JsonValue, IEquatable<JsonBool>
    {
        private bool _value;
        public JsonBool(bool value) { _value = value; }

        public static new JsonBool Parse(string jsonBool)
        {
            var ps = new JsonParserState(jsonBool);
            var result = ps.ParseBool();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonBool, out JsonBool result)
        {
            try
            {
                result = Parse(jsonBool);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public static implicit operator bool(JsonBool value) { return value._value; }
        public static implicit operator bool?(JsonBool value) { return value == null ? (bool?) null : value._value; }
        public static implicit operator JsonBool(bool value) { return new JsonBool(value); }
        public static implicit operator JsonBool(bool? value) { return value == null ? null : new JsonBool(value.Value); }

        public override bool Equals(object other)
        {
            return other is JsonBool ? Equals((JsonBool) other) : false;
        }

        public bool Equals(JsonBool other)
        {
            if (other == null) return false;
            return this._value == other._value;
        }

        public override int GetHashCode()
        {
            return _value ? 13259 : 22093;
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return _value ? "true" : "false";
        }
    }

    public class JsonNumber : JsonValue, IEquatable<JsonNumber>
    {
        private long _long;
        private double _double = double.NaN;
        public JsonNumber(double value) { _double = value; if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("JSON disallows NaNs and infinities."); }
        public JsonNumber(long value) { _long = value; }
        public JsonNumber(int value) { _double = value; }

        public static new JsonNumber Parse(string jsonNumber)
        {
            var ps = new JsonParserState(jsonNumber);
            var result = ps.ParseNumber();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        public static bool TryParse(string jsonNumber, out JsonNumber result)
        {
            try
            {
                result = Parse(jsonNumber);
                return true;
            }
            catch (JsonParseException)
            {
                result = null;
                return false;
            }
        }

        public static explicit operator double(JsonNumber value) { return double.IsNaN(value._double) ? (double) value._long : value._double; }
        public static explicit operator double?(JsonNumber value) { return value == null ? (double?) null : double.IsNaN(value._double) ? (double) value._long : value._double; }

        public static explicit operator long(JsonNumber value)
        {
            if (double.IsNaN(value._double))
            {
                return (long) value._long;
            }
            else
            {
                if (value._double != Math.Truncate(value._double))
                    throw new InvalidCastException("Only integer values can be interpreted as long.");
                if (value._double < long.MinValue || value._double > long.MaxValue)
                    throw new InvalidCastException("Cannot cast to long because the value exceeds the representable range.");
                return (long) value._double;
            }
        }
        public static explicit operator long?(JsonNumber value) { return value == null ? (long?) null : (long) value; }

        public static explicit operator int(JsonNumber value)
        {
            if (double.IsNaN(value._double))
            {
                if (value._long < int.MinValue || value._long > int.MaxValue)
                    throw new InvalidCastException("Cannot cast to int because the value exceeds the representable range.");
                return (int) value._long;
            }
            else
            {
                if (value._double != Math.Truncate(value._double))
                    throw new InvalidCastException("Only integer values can be interpreted as int.");
                if (value._double < int.MinValue || value._double > int.MaxValue)
                    throw new InvalidCastException("Cannot cast to int because the value exceeds the representable range.");
                return (int) value._double;
            }
        }
        public static explicit operator int?(JsonNumber value) { return value == null ? (int?) null : (int) value; }

        public static implicit operator JsonNumber(double value) { return new JsonNumber(value); }
        public static implicit operator JsonNumber(double? value) { return value == null ? null : new JsonNumber(value.Value); }
        public static implicit operator JsonNumber(long value) { return new JsonNumber(value); }
        public static implicit operator JsonNumber(long? value) { return value == null ? null : new JsonNumber(value.Value); }
        public static implicit operator JsonNumber(int value) { return new JsonNumber(value); }
        public static implicit operator JsonNumber(int? value) { return value == null ? null : new JsonNumber(value.Value); }

        public override bool Equals(object other)
        {
            return other is JsonNumber ? Equals((JsonNumber) other) : false;
        }

        public bool Equals(JsonNumber other)
        {
            if (other == null) return false;
            if (double.IsNaN(this._double) && double.IsNaN(other._double))
                return this._long == other._long;
            else
                return (double) this == (double) other;
        }

        public override int GetHashCode()
        {
            return double.IsNaN(_double) ? _long.GetHashCode() : _double.GetHashCode();
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return double.IsNaN(_double) ? _long.ToString() : _double.ToString();
        }
    }

    /// <summary>
    /// A special type of value which is never produced as a result of parsing valid JSON. Its sole purpose is to allow embedding
    /// arbitrary JavaScript code using <see cref="JsonValue.Fmt"/>.
    /// </summary>
    public class JsonRaw : JsonValue
    {
        public string Raw { get; private set; }

        public JsonRaw(string raw)
        {
            Raw = raw;
        }

        public static JsonRaw FromDate(DateTime datetime)
        {
            return new JsonRaw
            (
                datetime.TimeOfDay == TimeSpan.Zero
                    ? "new Date({0}, {1}, {2})".Fmt(datetime.Year, datetime.Month - 1, datetime.Day)
                    : "new Date({0}, {1}, {2}, {3}, {4}, {5}, {6})".Fmt(datetime.Year, datetime.Month - 1, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond)
            );
        }

        public override IEnumerable<string> ToEnumerable()
        {
            yield return Raw;
        }
    }
}
