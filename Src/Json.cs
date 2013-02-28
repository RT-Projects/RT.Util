using System;
using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>Keeps track of the JSON parser state.</summary>
    internal class JsonParserState
    {
        public string Json;
        public int Pos;

        private OffsetToLineCol _offsetConverter;
        public OffsetToLineCol OffsetConverter { get { if (_offsetConverter == null) _offsetConverter = new OffsetToLineCol(Json); return _offsetConverter; } }

        private bool _allowJavaScript;

        private JsonParserState() { }

        public JsonParserState(string json, bool allowJavaScript)
        {
            Json = json;
            Pos = 0;
            _allowJavaScript = allowJavaScript;
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
                    else if (_allowJavaScript && cn == '\'')
                        return ParseString();
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

        private JsonString parseDictKey()
        {
            if (Cur == null)
                throw new JsonParseException(this, "unexpected end of dictionary");

            if (_allowJavaScript)
            {
                char c = Cur.Value;
                if (c == '_' || c == '$' || char.IsLetter(c))
                {
                    int pos = Pos;
                    Pos++;
                    while (Cur != null && (Cur == '_' || Cur == '$' || char.IsLetter(Cur.Value)))
                        Pos++;
                    ConsumeWhitespace();
                    return new JsonString(Json.Substring(pos, Pos - pos));
                }
            }
            return ParseString();
        }

        public JsonString ParseString()
        {
            var sb = new StringBuilder();
            bool isJavaScript = Cur == '\'';
            if (Cur != '"' && (!_allowJavaScript || Cur != '\''))
                throw new JsonParseException(this, "expected a string");
            Pos++;
            while (true)
            {
                if (_allowJavaScript && isJavaScript && Cur == '\'')
                {
                    Pos++;
                    break;
                    // Note: JavaScript string support is incomplete; it allows only the same escapes as JSON strings.
                }
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
                                    if (_allowJavaScript && Cur == '\'')
                                    {
                                        sb.Append('\'');
                                        break;
                                    }
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
                var name = parseDictKey();
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

    /// <summary>Encapsulates a JSON value (e.g. a boolean, a number, a string, a list, a dictionary, etc.)</summary>
    public abstract class JsonValue : IEquatable<JsonValue>
    {
        /// <summary>
        ///     Parses the specified string into a JSON value.</summary>
        /// <param name="jsonValue">
        ///     A string containing JSON syntax.</param>
        /// <param name="allowJavaScript">
        ///     <para>True to allow certain notations that are allowed in JavaScript but not strictly in JSON:</para>
        ///     <list type="bullet">
        ///     <item><description>
        ///         allows keys in dictionaries to be unquoted</description></item>
        ///     <item><description>
        ///         allows strings to be delimited with single-quotes in addition to double-quotes</description></item>
        ///     </list></param>
        /// <returns>
        ///     A <see cref="JsonValue"/> instance representing the value.</returns>
        public static JsonValue Parse(string jsonValue, bool allowJavaScript = false)
        {
            var ps = new JsonParserState(jsonValue, allowJavaScript);
            var result = ps.ParseValue();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON value.</summary>
        /// <param name="jsonValue">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonValue"/> representing the value, or null if unsuccessful. (But note that null is
        ///     also a possible valid value in case of success.)</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Constructs a <see cref="JsonValue"/> from the specified string.</summary>
        public static implicit operator JsonValue(string value) { return value == null ? null : new JsonString(value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified boolean.</summary>
        public static implicit operator JsonValue(bool value) { return new JsonBool(value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified nullable boolean.</summary>
        public static implicit operator JsonValue(bool? value) { return value == null ? null : new JsonBool(value.Value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified double.</summary>
        public static implicit operator JsonValue(double value) { return new JsonNumber(value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified nullable double.</summary>
        public static implicit operator JsonValue(double? value) { return value == null ? null : new JsonNumber(value.Value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified long.</summary>
        public static implicit operator JsonValue(long value) { return new JsonNumber(value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified nullable long.</summary>
        public static implicit operator JsonValue(long? value) { return value == null ? null : new JsonNumber(value.Value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified int.</summary>
        public static implicit operator JsonValue(int value) { return new JsonNumber(value); }
        /// <summary>Constructs a <see cref="JsonValue"/> from the specified nullable int.</summary>
        public static implicit operator JsonValue(int? value) { return value == null ? null : new JsonNumber(value.Value); }

        /// <summary>See <see cref="AsString"/>.</summary>
        public static explicit operator string(JsonValue value) { return value == null ? (string) null : value.AsString; }
        /// <summary>See <see cref="AsBool"/>.</summary>
        public static explicit operator bool(JsonValue value) { return value.AsBool; }
        /// <summary>See <see cref="AsBool"/>.</summary>
        public static explicit operator bool?(JsonValue value) { return value == null ? (bool?) null : value.AsBool; }
        /// <summary>See <see cref="AsDouble"/>.</summary>
        public static explicit operator double(JsonValue value) { return value.AsDouble; }
        /// <summary>See <see cref="AsDouble"/>.</summary>
        public static explicit operator double?(JsonValue value) { return value == null ? (double?) null : value.AsDouble; }
        /// <summary>See <see cref="AsLong"/>.</summary>
        public static explicit operator long(JsonValue value) { return value.AsLong; }
        /// <summary>See <see cref="AsLong"/>.</summary>
        public static explicit operator long?(JsonValue value) { return value == null ? (long?) null : value.AsLong; }
        /// <summary>See <see cref="AsInt"/>.</summary>
        public static explicit operator int(JsonValue value) { return value.AsInt; }
        /// <summary>See <see cref="AsInt"/>.</summary>
        public static explicit operator int?(JsonValue value) { return value == null ? (int?) null : value.AsInt; }

        /// <summary>
        ///     Returns the current value cast to <see cref="JsonList"/> if it is a <see cref="JsonList"/>; otherwise,
        ///     throws.</summary>
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

        /// <summary>
        ///     Returns the current value cast to <see cref="JsonDict"/> if it is a <see cref="JsonDict"/>; otherwise,
        ///     throws.</summary>
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

        /// <summary>
        ///     Returns the current value as a string if it is a <see cref="JsonString"/>; otherwise, throws. Identical to an
        ///     explicit cast to <c>string</c>, except that the explicit cast also supports nulls.</summary>
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

        /// <summary>
        ///     Returns the current value as a bool if it is a <see cref="JsonBool"/>; otherwise, throws. Identical to an
        ///     explicit cast to <c>bool</c>.</summary>
        public bool AsBool
        {
            get
            {
                var v = this as JsonBool;
                if (v == null)
                    throw new NotSupportedException("Only bool values can be interpreted as bool.");
                return (bool) v;
            }
        }

        /// <summary>
        ///     Returns the current value as a double if it is a <see cref="JsonNumber"/>; otherwise, throws. Identical to an
        ///     explicit cast to <c>double</c>.</summary>
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

        /// <summary>
        ///     Returns the current value as a decimal if it is a <see cref="JsonNumber"/>; otherwise, throws. This property
        ///     is slightly lossy; see Remarks on <see cref="JsonNumber"/>. Identical to an explicit cast to <c>decimal</c>.</summary>
        public decimal AsDecimal
        {
            get
            {
                var v = this as JsonNumber;
                if (v == null)
                    throw new NotSupportedException("Only numeric values can be interpreted as decimal.");
                return (decimal) v;
            }
        }

        /// <summary>
        ///     Returns the current value as a long if it is a <see cref="JsonNumber"/> containing an integer within the
        ///     range of long; otherwise, throws. Identical to an explicit cast to <c>long</c>.</summary>
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

        /// <summary>
        ///     Returns the current value as an int if it is a <see cref="JsonNumber"/> containing an integer within the
        ///     range of int; otherwise, throws. Identical to an explicit cast to <c>int</c>.</summary>
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

        /// <summary>
        ///     Removes all items from the current value if it is a <see cref="JsonList"/> or <see cref="JsonDict"/>;
        ///     otherwise, throws.</summary>
        public void Clear()
        {
            if (this is JsonList)
                (this as JsonList).List.Clear();
            else if (this is JsonDict)
                (this as JsonDict).Dict.Clear();
            else
                throw new NotSupportedException("This method is only supported on dictionary and list values.");
        }

        /// <summary>
        ///     Returns the number of items in the current value if it is a <see cref="JsonList"/> or <see cref="JsonDict"
        ///     />; otherwise, throws.</summary>
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

        /// <summary>
        ///     Returns false if this value is a <see cref="JsonDict"/> or a <see cref="JsonList"/>; otherwise, returns
        ///     true.</summary>
        public bool IsReadOnly { get { return !(this is JsonDict || this is JsonList); } }

        #endregion

        #region IList

        /// <summary>
        ///     Returns the item at the specified <paramref name="index"/> within the current list if it is a <see
        ///     cref="JsonList"/>; otherwise, throws.</summary>
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

        /// <summary>
        ///     Add the specified <paramref name="item"/> to the current list if it is a <see cref="JsonList"/>; otherwise,
        ///     throws.</summary>
        public void Add(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.Add(item);
        }

        /// <summary>
        ///     Removes the first instance of the specified <paramref name="item"/> from the current list if it is a <see
        ///     cref="JsonList"/>; otherwise, throws.</summary>
        /// <returns>
        ///     True if an item was removed; otherwise, false.</returns>
        public bool Remove(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.Remove(item);
        }

        /// <summary>
        ///     Determines whether the specified <paramref name="item"/> is contained in the current list if it is a <see
        ///     cref="JsonList"/>; otherwise, throws.</summary>
        public bool Contains(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.Contains(item);
        }

        /// <summary>
        ///     Inserts the specified <paramref name="item"/> at the specified <paramref name="index"/> to the current list
        ///     if it is a <see cref="JsonList"/>; otherwise, throws.</summary>
        public void Insert(int index, JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.Insert(index, item);
        }

        /// <summary>
        ///     Removes the item at the specified <paramref name="index"/> from the current list if it is a <see
        ///     cref="JsonList"/>; otherwise, throws.</summary>
        public void RemoveAt(int index)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.RemoveAt(index);
        }

        /// <summary>
        ///     Returns the index of the first occurrence of the specified <paramref name="item"/> within the current list if
        ///     it is a <see cref="JsonList"/>; otherwise, throws.</summary>
        /// <returns>
        ///     The index of the item, or -1 if the item is not in the list.</returns>
        public int IndexOf(JsonValue item)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            return list.List.IndexOf(item);
        }

        /// <summary>
        ///     Copies the entire list to a compatible one-dimensional <paramref name="array"/>, starting at the specified
        ///     <paramref name="arrayIndex"/> of the target array, if this is a <see cref="JsonList"/>; otherwise, throws.</summary>
        /// <param name="array">
        ///     The one-dimensional array that is the destination of the elements copied from the list. The array must have
        ///     zero-based indexing.</param>
        /// <param name="arrayIndex">
        ///     The zero-based index in array at which copying begins.</param>
        public void CopyTo(JsonValue[] array, int arrayIndex)
        {
            var list = this as JsonList;
            if (list == null)
                throw new NotSupportedException("This method is only supported on list values.");
            list.List.CopyTo(array, arrayIndex);
        }

        #endregion

        #region IDictionary

        /// <summary>
        ///     Gets or sets the value associated with the specified <paramref name="key"/> if this value is a <see
        ///     cref="JsonDict"/>; otherwise, throws.</summary>
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

        /// <summary>Returns the keys contained in the dictionary if this is a <see cref="JsonDict"/>; otherwise, throws.</summary>
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

        /// <summary>Returns the values contained in the dictionary if this is a <see cref="JsonDict"/>; otherwise, throws.</summary>
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

        /// <summary>
        ///     Attempts to retrieve the value associated with the specified <paramref name="key"/> if this is a <see
        ///     cref="JsonDict"/>; otherwise, throws.</summary>
        /// <param name="key">
        ///     The key for which to try to retrieve the value.</param>
        /// <param name="value">
        ///     Receives the value associated with the specified <paramref name="key"/>, or null if the key is not in the
        ///     dictionary. (Note that null may also be a valid value in case of success.)</param>
        /// <returns>
        ///     True if the key was in the dictionary; otherwise, false.</returns>
        public bool TryGetValue(string key, out JsonValue value)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.TryGetValue(key, out value);
        }

        /// <summary>
        ///     Adds the specified key/value pair to the dictionary if this is a <see cref="JsonDict"/>; otherwise, throws.</summary>
        /// <param name="key">
        ///     The key to add.</param>
        /// <param name="value">
        ///     The value to add.</param>
        public void Add(string key, JsonValue value)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            dict.Dict.Add(key, value);
        }

        /// <summary>
        ///     Removes the entry with the specified <paramref name="key"/> from the dictionary if this is a <see
        ///     cref="JsonDict"/>; otherwise, throws.</summary>
        /// <param name="key">
        ///     The key that identifies the entry to remove.</param>
        /// <returns>
        ///     True if an entry was removed; false if the key wasn’t in the dictionary.</returns>
        public bool Remove(string key)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.Remove(key);
        }

        /// <summary>
        ///     Determines whether an entry with the specified <paramref name="key"/> exists in the dictionary if this is a
        ///     <see cref="JsonDict"/>; otherwise, throws.</summary>
        public bool ContainsKey(string key)
        {
            var dict = this as JsonDict;
            if (dict == null)
                throw new NotSupportedException("This method is only supported on dictionary values.");
            return dict.Dict.ContainsKey(key);
        }

        #endregion

        /// <summary>
        ///     Determines whether this value is equal to the <paramref name="other"/> value. (See also remarks in the other
        ///     overload, <see cref="Equals(JsonValue)"/>.)</summary>
        public override bool Equals(object other)
        {
            return other is JsonValue ? Equals((JsonValue) other) : false;
        }

        /// <summary>
        ///     Determines whether this value is equal to the <paramref name="other"/> value. (See also remarks.)</summary>
        /// <remarks>
        ///     Two values are only considered equal if they are of the same type (e.g. a <see cref="JsonString"/> is never
        ///     equal to a <see cref="JsonNumber"/> even if they contain the same number). Lists are equal if they contain
        ///     the same values in the same order. Dictionaries are equal if they contain the same set of key/value pairs.</remarks>
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

        /// <summary>Returns a hash code representing this object.</summary>
        public abstract override int GetHashCode();

        /// <summary>
        ///     Converts this JSON value to <c>double</c>. Unlike <see cref="AsDouble"/>, this function also converts string
        ///     values, provided the string is parseable as a JSON-compatible numeric value.</summary>
        public virtual double ToDouble()
        {
            throw new NotSupportedException("Only numeric values and strings can be converted to double.");
        }

        /// <summary>
        ///     Converts this JSON value to <c>decimal</c>. Unlike <see cref="AsDecimal"/>, this function also converts
        ///     string values, provided the string is parseable as a JSON-compatible numeric value. This function is slightly
        ///     lossy; see Remarks on <see cref="JsonNumber"/> (but not lossy when converting a string).</summary>
        public virtual decimal ToDecimal()
        {
            throw new NotSupportedException("Only numeric values and strings can be converted to decimal.");
        }

        /// <summary>
        ///     Converts this JSON value to <c>int</c>. Throws if the value is outside the range supported by <c>int</c>, or
        ///     represents a number that isn't an integer. Unlike <see cref="AsInt"/>, this function also converts string
        ///     values, provided the string is parseable as a JSON-compatible integer value.</summary>
        public virtual int ToInt(bool allowZeroFraction = false)
        {
            throw new NotSupportedException("Only numeric values and strings can be converted to int.");
        }

        /// <summary>
        ///     Converts this JSON value to <c>long</c>. Throws if the value is outside the range supported by <c>long</c>, or
        ///     represents a number that isn't an integer. Unlike <see cref="AsLong"/>, this function also converts string
        ///     values, provided the string is parseable as a JSON-compatible integer value.</summary>
        public virtual long ToLong(bool allowZeroFraction = false)
        {
            throw new NotSupportedException("Only numeric values and strings can be converted to long.");
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
        ///     Formats JSON values into a piece of JavaScript code and then removes almost all unnecessary whitespace and
        ///     comments. Values are referenced by names; placeholders for these values are written as {{name}}. Placeholders
        ///     are only replaced outside of JavaScript literal strings and regexes. <see cref="JsonRaw"/> instances are
        ///     inserted unmodified.</summary>
        /// <param name="js">
        ///     JavaScript code with placeholders.</param>
        /// <param name="namevalues">
        ///     Alternating names and associated values, for example ["user", "abc"] specifies one value named "user".</param>
        /// <example>
        ///     <para>The following code:</para>
        ///     <code>JsonValue.Fmt(@"Foo({{userid}}, {{username}}, {{options}});", "userid", userid, "username", username,
        ///     "options", null)</code>
        ///     <para>might return the following string:</para>
        ///     <code>Foo(123, "Matthew Stranger", null);</code></example>
        /// <exception cref="ArgumentException">
        ///     <paramref name="namevalues"/> has an odd number of values. OR <paramref name="js"/> contains a
        ///     {{placeholder}} whose name is not listed in <paramref name="namevalues"/>.</exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="js"/> is null. OR <paramref name="namevalues"/> is null.</exception>
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
                '(?:[^'\\]|\\.)*'|""(?:[^""\\]|\\.)*""|((?<!(?:\p{L}|\p{Nd}|[_\)\]\}\$])\s*)|(?<=return\s*))/(?:[^/\\]|\\.)*/|
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

    /// <summary>Encapsulates a list of <see cref="JsonValue"/> values.</summary>
    public class JsonList : JsonValue, IList<JsonValue>, IEquatable<JsonList>
    {
        internal List<JsonValue> List;

        /// <summary>Constructs an empty list.</summary>
        public JsonList() { List = new List<JsonValue>(); }

        /// <summary>
        ///     Constructs a <see cref="JsonList"/> instance containing a copy of the specified collection of <paramref
        ///     name="items"/>.</summary>
        public JsonList(IEnumerable<JsonValue> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            List = new List<JsonValue>(items is ICollection<JsonValue> ? ((ICollection<JsonValue>) items).Count + 2 : 4);
            List.AddRange(items);
        }

        /// <summary>
        ///     Parses the specified JSON as a JSON list. All other types of JSON values result in a <see
        ///     cref="JsonParseException"/>.</summary>
        /// <param name="jsonList">
        ///     JSON syntax to parse.</param>
        /// <param name="allowJavaScript">
        ///     See <see cref="JsonValue.Parse"/>.</param>
        public static new JsonList Parse(string jsonList, bool allowJavaScript = false)
        {
            if (jsonList == null)
                throw new ArgumentNullException("jsonList");
            var ps = new JsonParserState(jsonList, allowJavaScript);
            var result = ps.ParseList();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON list.</summary>
        /// <param name="jsonNumber">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonList"/> representing the list, or null if unsuccessful.</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Enumerates the values in this list.</summary>
        public IEnumerator<JsonValue> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public override bool Equals(object other)
        {
            return other is JsonList ? Equals((JsonList) other) : false;
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public bool Equals(JsonList other)
        {
            if (other == null) return false;
            if (this.Count != other.Count) return false;
            return this.Zip(other, (v1, v2) => (v1 == null) == (v2 == null) && (v1 == null || v1.Equals(v2))).All(b => b);
        }

        /// <summary>Returns a hash code representing this object.</summary>
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

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
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

    /// <summary>Encapsulates a JSON dictionary (a set of key/value pairs).</summary>
    public class JsonDict : JsonValue, IDictionary<string, JsonValue>, IEquatable<JsonDict>
    {
        internal Dictionary<string, JsonValue> Dict;

        /// <summary>Constructs an empty dictionary.</summary>
        public JsonDict() { Dict = new Dictionary<string, JsonValue>(); }

        /// <summary>Constructs a dictionary containing a copy of the specified collection of key/value pairs.</summary>
        public JsonDict(IEnumerable<KeyValuePair<string, JsonValue>> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            Dict = new Dictionary<string, JsonValue>(items is ICollection<KeyValuePair<string, JsonValue>> ? ((ICollection<KeyValuePair<string, JsonValue>>) items).Count + 2 : 4);
            foreach (var item in items)
                Dict.Add(item.Key, item.Value);
        }

        /// <summary>
        ///     Parses the specified JSON as a JSON dictionary. All other types of JSON values result in a <see
        ///     cref="JsonParseException"/>.</summary>
        /// <param name="jsonDict">
        ///     JSON syntax to parse.</param>
        /// <param name="allowJavaScript">
        ///     See <see cref="JsonValue.Parse"/>.</param>
        public static new JsonDict Parse(string jsonDict, bool allowJavaScript = false)
        {
            var ps = new JsonParserState(jsonDict, allowJavaScript);
            var result = ps.ParseDict();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON dictionary.</summary>
        /// <param name="jsonDict">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonDict"/> representing the dictionary, or null if unsuccessful.</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Enumerates the key/value pairs in this dictionary.</summary>
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

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public override bool Equals(object other)
        {
            return other is JsonDict && Equals((JsonDict) other);
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
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

        /// <summary>Returns a hash code representing this object.</summary>
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

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
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

    /// <summary>Encapsulates a string as a JSON value.</summary>
    public class JsonString : JsonValue, IEquatable<JsonString>
    {
        private string _value;

        /// <summary>Constructs a <see cref="JsonString"/> instance from the specified string.</summary>
        public JsonString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            _value = value;
        }

        /// <summary>
        ///     Parses the specified JSON as a JSON string. All other types of JSON values result in a <see
        ///     cref="JsonParseException"/>.</summary>
        /// <param name="jsonString">
        ///     JSON syntax to parse.</param>
        /// <param name="allowJavaScript">
        ///     See <see cref="JsonValue.Parse"/>.</param>
        public static new JsonString Parse(string jsonString, bool allowJavaScript = false)
        {
            var ps = new JsonParserState(jsonString, allowJavaScript);
            var result = ps.ParseString();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON string.</summary>
        /// <param name="jsonString">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonString"/> representing the string, or null if unsuccessful.</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Converts the specified <see cref="JsonString"/> value to an ordinary string.</summary>
        public static implicit operator string(JsonString value) { return value == null ? null : value._value; }
        /// <summary>Converts the specified ordinary string to a <see cref="JsonString"/> value.</summary>
        public static implicit operator JsonString(string value) { return value == null ? null : new JsonString(value); }

        /// <summary>Converts this JSON string to <c>double</c>.</summary>
        public override double ToDouble()
        {
            double result = double.Parse(_value);
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException("This string cannot be converted to a double because JSON doesn't support NaNs and infinities.");
            return result;
        }

        /// <summary>
        ///     Converts this JSON string to <c>decimal</c>. Unlike <see cref="JsonValue.AsDecimal"/> or explicit casts, this
        ///     function is not lossy, provided the string does not have more precision than can be supported by
        ///     <c>decimal</c>.</summary>
        public override decimal ToDecimal()
        {
            return decimal.Parse(_value);
        }

        /// <summary>Converts this JSON value to <c>int</c>.</summary>
        public override int ToInt(bool allowZeroFraction = false)
        {
            if (allowZeroFraction)
            {
                decimal result = decimal.Parse(_value);
                if (result != decimal.Truncate(result))
                    throw new InvalidOperationException("String must represent an integer, but \"{0}\" has a fractional part.".Fmt(_value));
                return (int) result;
            }
            return int.Parse(_value);
        }

        /// <summary>Converts this JSON value to <c>long</c>.</summary>
        public override long ToLong(bool allowZeroFraction = false)
        {
            if (allowZeroFraction)
            {
                decimal result = decimal.Parse(_value);
                if (result != decimal.Truncate(result))
                    throw new InvalidOperationException("String must represent an integer, but \"{0}\" has a fractional part.".Fmt(_value));
                return (long) result;
            }
            return long.Parse(_value);
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public override bool Equals(object other)
        {
            return other is JsonString ? Equals((JsonString) other) : false;
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public bool Equals(JsonString other)
        {
            if (other == null) return false;
            return this._value == other._value;
        }

        /// <summary>Returns a hash code representing this object.</summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
        public override IEnumerable<string> ToEnumerable()
        {
            yield return _value.JsEscape(JsQuotes.Double);
        }

        /// <summary>
        ///     Returns a JavaScript-compatible representation of this string.</summary>
        /// <param name="quotes">
        ///     Specifies the style of quotes to use around the string.</param>
        public string ToString(JsQuotes quotes)
        {
            return _value.JsEscape(quotes);
        }
    }

    /// <summary>Encapsulates a boolean value as a <see cref="JsonValue"/>.</summary>
    public class JsonBool : JsonValue, IEquatable<JsonBool>
    {
        private bool _value;

        /// <summary>Constructs a <see cref="JsonBool"/> from the specified boolean.</summary>
        public JsonBool(bool value) { _value = value; }

        /// <summary>
        ///     Parses the specified JSON as a JSON boolean. All other types of JSON values result in a <see
        ///     cref="JsonParseException"/>.</summary>
        /// <param name="jsonBool">
        ///     JSON syntax to parse.</param>
        /// <param name="allowJavaScript">
        ///     See <see cref="JsonValue.Parse"/>.</param>
        public static new JsonBool Parse(string jsonBool, bool allowJavaScript = false)
        {
            var ps = new JsonParserState(jsonBool, allowJavaScript);
            var result = ps.ParseBool();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON boolean.</summary>
        /// <param name="jsonBool">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonBool"/> representing the boolean, or null if unsuccessful.</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Converts the specified <see cref="JsonBool"/> value to an ordinary boolean.</summary>
        public static explicit operator bool(JsonBool value) { return value._value; }
        /// <summary>Converts the specified <see cref="JsonBool"/> value to a nullable boolean.</summary>
        public static implicit operator bool?(JsonBool value) { return value == null ? (bool?) null : value._value; }
        /// <summary>Converts the specified ordinary boolean to a <see cref="JsonBool"/> value.</summary>
        public static implicit operator JsonBool(bool value) { return new JsonBool(value); }
        /// <summary>Converts the specified nullable boolean to a <see cref="JsonBool"/> value or null.</summary>
        public static implicit operator JsonBool(bool? value) { return value == null ? null : new JsonBool(value.Value); }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public override bool Equals(object other)
        {
            return other is JsonBool ? Equals((JsonBool) other) : false;
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public bool Equals(JsonBool other)
        {
            if (other == null) return false;
            return this._value == other._value;
        }

        /// <summary>Returns a hash code representing this object.</summary>
        public override int GetHashCode()
        {
            return _value ? 13259 : 22093;
        }

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
        public override IEnumerable<string> ToEnumerable()
        {
            yield return _value ? "true" : "false";
        }
    }

    /// <summary>
    ///     Encapsulates a number, which may be a floating-point number or an integer, as a <see cref="JsonValue"/>. See
    ///     Remarks.</summary>
    /// <remarks>
    ///     JSON does not define any specific limits for numeric values. This implementation supports integers in the signed
    ///     64-bit range, as well as IEEE 64-bit doubles (except NaNs and infinities). Conversions to/from <c>decimal</c> are
    ///     exact for integers, but can be approximate for non-integers, depending on the exact value.</remarks>
    public class JsonNumber : JsonValue, IEquatable<JsonNumber>
    {
        private long _long;
        private double _double = double.NaN;

        /// <summary>Constructs a <see cref="JsonNumber"/> from the specified double-precision floating-point number.</summary>
        public JsonNumber(double value) { _double = value; if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("JSON disallows NaNs and infinities."); }
        /// <summary>Constructs a <see cref="JsonNumber"/> from the specified 64-bit integer.</summary>
        public JsonNumber(long value) { _long = value; }
        /// <summary>Constructs a <see cref="JsonNumber"/> from the specified 32-bit integer.</summary>
        public JsonNumber(int value) { _double = value; }
        /// <summary>
        ///     Constructs a <see cref="JsonNumber"/> from the specified decimal. This operation is slightly lossy; see
        ///     Remarks on <see cref="JsonNumber"/>.</summary>
        public JsonNumber(decimal value)
        {
            if (value == decimal.Truncate(value) && value >= long.MinValue && value <= long.MaxValue)
                _long = (long) value;
            else
                _double = (double) value;
        }

        /// <summary>
        ///     Parses the specified JSON as a JSON number. All other types of JSON values result in a <see
        ///     cref="JsonParseException"/>.</summary>
        /// <param name="jsonNumber">
        ///     JSON syntax to parse.</param>
        /// <param name="allowJavaScript">
        ///     See <see cref="JsonValue.Parse"/>.</param>
        public static new JsonNumber Parse(string jsonNumber, bool allowJavaScript = false)
        {
            var ps = new JsonParserState(jsonNumber, allowJavaScript);
            var result = ps.ParseNumber();
            if (ps.Cur != null)
                throw new JsonParseException(ps, "expected end of input");
            return result;
        }

        /// <summary>
        ///     Attempts to parse the specified string into a JSON number.</summary>
        /// <param name="jsonNumber">
        ///     A string containing JSON syntax.</param>
        /// <param name="result">
        ///     Receives the <see cref="JsonNumber"/> representing the number, or null if unsuccessful.</param>
        /// <returns>
        ///     True if parsing was successful; otherwise, false.</returns>
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

        /// <summary>Converts the specified <see cref="JsonNumber"/> to a double.</summary>
        public static explicit operator double(JsonNumber value) { return double.IsNaN(value._double) ? (double) value._long : value._double; }
        /// <summary>Converts the specified <see cref="JsonNumber"/> to a nullable double.</summary>
        public static implicit operator double?(JsonNumber value) { return value == null ? (double?) null : double.IsNaN(value._double) ? (double) value._long : value._double; }

        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a decimal. This operator is slightly lossy; see Remarks on
        ///     <see cref="JsonNumber"/>.</summary>
        public static explicit operator decimal(JsonNumber value) { return double.IsNaN(value._double) ? (decimal) value._long : (decimal) value._double; }
        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a nullable decimal. This operator is slightly lossy; see
        ///     Remarks on <see cref="JsonNumber"/>.</summary>
        public static explicit operator decimal?(JsonNumber value) { return value == null ? (decimal?) null : (decimal) value; }

        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a 64-bit integer. Throws if the number is not an integer
        ///     or does not fit in the range of a 64-bit integer.</summary>
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

        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a nullable 64-bit integer. Throws if the number is not an
        ///     integer or does not fit in the range of a 64-bit integer.</summary>
        public static explicit operator long?(JsonNumber value) { return value == null ? (long?) null : (long) value; }

        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a 32-bit integer. Throws if the number is not an integer
        ///     or does not fit in the range of a 32-bit integer.</summary>
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

        /// <summary>
        ///     Converts the specified <see cref="JsonNumber"/> to a nullable 32-bit integer. Throws if the number is not an
        ///     integer or does not fit in the range of a 32-bit integer.</summary>
        public static explicit operator int?(JsonNumber value) { return value == null ? (int?) null : (int) value; }

        /// <summary>Converts the specified double to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(double value) { return new JsonNumber(value); }
        /// <summary>Converts the specified nullable double to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(double? value) { return value == null ? null : new JsonNumber(value.Value); }
        /// <summary>Converts the specified 64-bit integer to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(long value) { return new JsonNumber(value); }
        /// <summary>Converts the specified nullable 64-bit integer to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(long? value) { return value == null ? null : new JsonNumber(value.Value); }
        /// <summary>Converts the specified 32-bit integer to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(int value) { return new JsonNumber(value); }
        /// <summary>Converts the specified nullable 32-bit integer to a <see cref="JsonNumber"/> value.</summary>
        public static implicit operator JsonNumber(int? value) { return value == null ? null : new JsonNumber(value.Value); }
        /// <summary>
        ///     Converts the specified decimal to a <see cref="JsonNumber"/> value. This operator is slightly lossy; see
        ///     Remarks on <see cref="JsonNumber"/>.</summary>
        public static explicit operator JsonNumber(decimal value) { return new JsonNumber(value); }
        /// <summary>
        ///     Converts the specified nullable decimal to a <see cref="JsonNumber"/> value. This operator is slightly lossy;
        ///     see Remarks on <see cref="JsonNumber"/>.</summary>
        public static explicit operator JsonNumber(decimal? value) { return value == null ? null : new JsonNumber(value.Value); }

        /// <summary>Converts this JSON value to <c>double</c>.</summary>
        public override double ToDouble() { return (double) this; }

        /// <summary>
        ///     Converts this JSON value to <c>decimal</c>. This function is slightly lossy; see Remarks on <see
        ///     cref="JsonNumber"/>.</summary>
        public override decimal ToDecimal() { return (decimal) this; }

        /// <summary>Converts this JSON value to <c>int</c>. Throws if the value is outside the range supported by <c>int</c>.</summary>
        public override int ToInt(bool allowZeroFraction = false)
        {
            return (int) this;
        }

        /// <summary>
        ///     Converts this JSON value to <c>long</c>. Throws if the value is outside the range supported by <c>long</c>.</summary>
        public override long ToLong(bool allowZeroFraction = false)
        {
            return (long) this;
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public override bool Equals(object other)
        {
            return other is JsonNumber ? Equals((JsonNumber) other) : false;
        }

        /// <summary>See <see cref="JsonValue.Equals(JsonValue)"/>.</summary>
        public bool Equals(JsonNumber other)
        {
            if (other == null) return false;
            if (double.IsNaN(this._double) && double.IsNaN(other._double))
                return this._long == other._long;
            else
                return (double) this == (double) other;
        }

        /// <summary>Returns a hash code representing this object.</summary>
        public override int GetHashCode()
        {
            return double.IsNaN(_double) ? _long.GetHashCode() : _double.GetHashCode();
        }

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
        public override IEnumerable<string> ToEnumerable()
        {
            yield return double.IsNaN(_double) ? _long.ToString() : _double.ToString();
        }
    }

    /// <summary>
    ///     A special type of value which is never produced as a result of parsing valid JSON. Its sole purpose is to allow
    ///     embedding arbitrary JavaScript code using <see cref="JsonValue.Fmt"/>.</summary>
    public class JsonRaw : JsonValue
    {
        /// <summary>Gets the raw JSON.</summary>
        public string Raw { get; private set; }

        /// <summary>Constructs a <see cref="JsonRaw"/> instance from the specified raw JSON.</summary>
        public JsonRaw(string raw)
        {
            Raw = raw;
        }

        /// <summary>Generates a <see cref="JsonRaw"/> instance from the specified date/time stamp.</summary>
        public static JsonRaw FromDate(DateTime datetime)
        {
            return new JsonRaw
            (
                datetime.TimeOfDay == TimeSpan.Zero
                    ? "new Date({0}, {1}, {2})".Fmt(datetime.Year, datetime.Month - 1, datetime.Day)
                    : "new Date({0}, {1}, {2}, {3}, {4}, {5}, {6})".Fmt(datetime.Year, datetime.Month - 1, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond)
            );
        }

        /// <summary>See <see cref="JsonValue.ToEnumerable()"/>.</summary>
        public override IEnumerable<string> ToEnumerable()
        {
            yield return Raw;
        }

        /// <summary>Returns a hash code representing this object.</summary>
        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }
    }
}
