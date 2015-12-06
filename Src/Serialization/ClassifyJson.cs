using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace RT.Util.Serialization
{
    /// <summary>Offers a convenient way to use <see cref="Classify"/> to serialize objects using the JSON format.</summary>
    public static class ClassifyJson
    {
        /// <summary>
        ///     Format used when null is passed to methods that take a format. Make sure not to modify this instance if any thread
        ///     in the application might be in the middle of using <see cref="ClassifyJson"/>; ideally the options shoud be set
        ///     once during startup and never changed after that.</summary>
        public static IClassifyFormat<JsonValue> DefaultFormat = ClassifyJsonFormat.Default;

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <typeparam name="T">
        ///     Type of object to read.</typeparam>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T DeserializeFile<T>(string filename, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.DeserializeFile<JsonValue, T>(filename, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <param name="type">
        ///     Type of object to read.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object DeserializeFile(Type type, string filename, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.DeserializeFile(type, filename, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="json">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T Deserialize<T>(JsonValue json, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.Deserialize<JsonValue, T>(json, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <param name="type">
        ///     Type of object to reconstruct.</param>
        /// <param name="json">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object Deserialize(Type type, JsonValue json, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.Deserialize(type, json, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form by applying the values to an
        ///     existing instance of the type.</summary>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="json">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        public static void DeserializeIntoObject<T>(JsonValue json, T intoObject, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            Classify.DeserializeIntoObject(json, intoObject, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired type.
        ///     The type of object is inferred from the object passed in.</summary>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
        ///     expected.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        public static void DeserializeFileIntoObject(string filename, object intoObject, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            Classify.DeserializeFileIntoObject(filename, intoObject, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <typeparam name="T">
        ///     Type of the object to store.</typeparam>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        public static void SerializeToFile<T>(T saveObject, string filename, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            Classify.SerializeToFile(saveObject, filename, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <param name="saveType">
        ///     Type of the object to store.</param>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        public static void SerializeToFile(Type saveType, object saveObject, string filename, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            Classify.SerializeToFile(saveType, saveObject, filename, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Converts the specified object into a serialized form.</summary>
        /// <typeparam name="T">
        ///     Type of object to convert.</typeparam>
        /// <param name="saveObject">
        ///     Object to be serialized.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static JsonValue Serialize<T>(T saveObject, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.Serialize(saveObject, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Converts the specified object into a serialized form.</summary>
        /// <param name="saveType">
        ///     Type of object to convert.</param>
        /// <param name="saveObject">
        ///     Object to be serialized.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static JsonValue Serialize(Type saveType, object saveObject, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
        {
            return Classify.Serialize(saveType, saveObject, format ?? DefaultFormat, options);
        }
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by the object being serialized.</summary>
    public interface IClassifyJsonObjectProcessor : IClassifyObjectProcessor<JsonValue>
    {
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by a class derived from <see
    ///     cref="ClassifyTypeOptions"/> and associated with a type via <see cref="ClassifyOptions.AddTypeOptions"/>.</summary>
    public interface IClassifyJsonTypeProcessor : IClassifyTypeProcessor<JsonValue>
    {
    }

    /// <summary>
    ///     Provides a format to serialize/deserialize objects as JSON using <see cref="Classify"/> and any serialization options
    ///     which are format-specific. To use this format, see <see cref="Default"/>.</summary>
    public sealed class ClassifyJsonFormat : IClassifyFormat<JsonValue>
    {
        /// <summary>Gets the JSON Classify format with all options at their defaults.</summary>
        public static IClassifyFormat<JsonValue> Default { get { return _default ?? (_default = new ClassifyJsonFormat()); } }
        private static ClassifyJsonFormat _default;

        private ClassifyJsonFormat() { }

        JsonValue IClassifyFormat<JsonValue>.ReadFromStream(Stream stream)
        {
            return JsonValue.Parse(stream.ReadAllText());
        }

        void IClassifyFormat<JsonValue>.WriteToStream(JsonValue element, Stream stream)
        {
            using (var textWriter = new StreamWriter(stream, Encoding.UTF8))
                textWriter.Write(JsonValue.ToStringIndented(element));
        }

        bool IClassifyFormat<JsonValue>.IsNull(JsonValue element)
        {
            return element == null || element is JsonNoValue;
        }

        object IClassifyFormat<JsonValue>.GetSimpleValue(JsonValue element)
        {
            if (element is JsonDict && element.ContainsKey(":value"))
                element = element[":value"];

            if (element is JsonString)
                return element.GetString();
            else if (element is JsonBool)
                return element.GetBool();
            else if (element is JsonNumber)
                return ((JsonNumber) element).RawValue;
            else
                return null;
        }

        JsonValue IClassifyFormat<JsonValue>.GetSelfValue(JsonValue element)
        {
            return element[":value"];
        }

        IEnumerable<JsonValue> IClassifyFormat<JsonValue>.GetList(JsonValue element, int? tupleSize)
        {
            if (element is JsonDict && element.ContainsKey(":value"))
                return element[":value"].GetList();
            return element.GetList();
        }

        void IClassifyFormat<JsonValue>.GetKeyValuePair(JsonValue element, out JsonValue key, out JsonValue value)
        {
            if (element is JsonDict && element.ContainsKey(":value"))
                element = element[":value"];
            key = element[0];
            value = element[1];
        }

        IEnumerable<KeyValuePair<object, JsonValue>> IClassifyFormat<JsonValue>.GetDictionary(JsonValue element)
        {
            return element.GetDict()
                .Where(kvp => !kvp.Key.StartsWith(":") || kvp.Key.StartsWith("::"))
                .Select(kvp => new KeyValuePair<object, JsonValue>(kvp.Key.StartsWith(":") ? kvp.Key.Substring(1) : kvp.Key, kvp.Value));
        }

        bool IClassifyFormat<JsonValue>.HasField(JsonValue element, string fieldName, string declaringType)
        {
            if (fieldName.StartsWith(':'))
                fieldName = ":" + fieldName;
            return element is JsonDict && element.ContainsKey(fieldName) && (
                !(element[fieldName] is JsonDict) ||
                !element[fieldName].ContainsKey(":declaringTypes") ||
                element[fieldName][":declaringTypes"].Contains(declaringType));
        }

        JsonValue IClassifyFormat<JsonValue>.GetField(JsonValue element, string fieldName, string declaringType)
        {
            if (fieldName.StartsWith(':'))
                fieldName = ":" + fieldName;
            var consider = element[fieldName];
            if (consider is JsonDict && consider.ContainsKey(":declaringTypes"))
                return consider[":values"][consider[":declaringTypes"].IndexOf(declaringType)];
            return consider;
        }

        string IClassifyFormat<JsonValue>.GetType(JsonValue element, out bool isFullType)
        {
            if (element is JsonDict)
            {
                isFullType = element.ContainsKey(":fulltype");
                return element.Safe[isFullType ? ":fulltype" : ":type"].GetStringSafe();
            }
            isFullType = false;
            return null;
        }

        bool IClassifyFormat<JsonValue>.IsReference(JsonValue element)
        {
            return element is JsonDict && element.ContainsKey(":ref");
        }

        bool IClassifyFormat<JsonValue>.IsReferable(JsonValue element)
        {
            return element is JsonDict && element.ContainsKey(":refid");
        }
        
        int IClassifyFormat<JsonValue>.GetReferenceID(JsonValue element)
        {
            return
                element.ContainsKey(":ref") ? element[":ref"].GetInt() :
                element.ContainsKey(":refid") ? element[":refid"].GetInt() :
                Ut.Throw<int>(new InvalidOperationException("The JSON Classify format encountered a contractual violation perpetrated by Classify. GetReferenceID() should not be called unless IsReference() or IsReferable() returned true."));
        }
        
        JsonValue IClassifyFormat<JsonValue>.FormatNullValue()
        {
            return JsonNoValue.Instance;
        }

        JsonValue IClassifyFormat<JsonValue>.FormatSimpleValue(object value)
        {
            if (value == null)
                return null;

            if (value is double)
                return (double) value;
            if (value is float)
                return (float) value;
            if (value is byte)
                return (byte) value;
            if (value is sbyte)
                return (sbyte) value;
            if (value is short)
                return (short) value;
            if (value is ushort)
                return (ushort) value;
            if (value is int)
                return (int) value;
            if (value is uint)
                return (uint) value;
            if (value is long)
                return (long) value;
            if (value is ulong)
                return (double) (ulong) value;
            if (value is decimal)
                return (decimal) value;
            if (value is bool)
                return (bool) value;
            if (value is string)
                return (string) value;

            // This takes care of enum types and DateTime
            return ExactConvert.ToString(value);
        }

        JsonValue IClassifyFormat<JsonValue>.FormatSelfValue(JsonValue value)
        {
            return new JsonDict { { ":value", value } };
        }

        JsonValue IClassifyFormat<JsonValue>.FormatList(bool isTuple, IEnumerable<JsonValue> values)
        {
            return new JsonList(values);
        }

        JsonValue IClassifyFormat<JsonValue>.FormatKeyValuePair(JsonValue key, JsonValue value)
        {
            return new JsonList(new[] { key, value });
        }

        JsonValue IClassifyFormat<JsonValue>.FormatDictionary(IEnumerable<KeyValuePair<object, JsonValue>> values)
        {
            return values.ToJsonDict(kvp => ExactConvert.ToString(kvp.Key).Apply(key => key.StartsWith(":") ? ":" + key : key), kvp => kvp.Value);
        }

        JsonValue IClassifyFormat<JsonValue>.FormatObject(IEnumerable<ObjectFieldInfo<JsonValue>> fields)
        {
            return new JsonDict(fields
                .GroupBy(f => f.FieldName)
                .Select(gr => new KeyValuePair<string, JsonValue>(
                    gr.Key.StartsWith(':') ? ":" + gr.Key : gr.Key,
                    gr.Skip(1).Any()
                        ? new JsonDict
                            {
                                { ":declaringTypes", new JsonList(gr.Select(elem => (JsonValue) elem.DeclaringType)) },
                                { ":values", new JsonList(gr.Select(elem => (JsonValue) elem.Value)) }
                            }
                        : gr.First().Value
                )));
        }
        
        JsonValue IClassifyFormat<JsonValue>.FormatReference(int refId)
        {
            return new JsonDict { { ":ref", refId } };
        }

        JsonValue IClassifyFormat<JsonValue>.FormatReferable(JsonValue element, int refId)
        {
            if (!(element is JsonDict))
                return new JsonDict { { ":refid", refId }, { ":value", element } };

            element[":refid"] = refId;
            return element;
        }

        JsonValue IClassifyFormat<JsonValue>.FormatWithType(JsonValue element, string type, bool isFullType)
        {
            if (!(element is JsonDict))
                return new JsonDict { { isFullType ? ":fulltype" : ":type", type }, { ":value", element } };

            element[isFullType ? ":fulltype" : ":type"] = type;
            return element;
        }

        void IClassifyFormat<JsonValue>.ThrowMissingReferable(int refID)
        {
            throw new InvalidOperationException(@"An object reference ("":ref"": {0}) was encountered, but no matching object ("":refid"": {0}) was encountered during deserialization. If such an object is present somewhere in the JSON, the relevant object was not deserialized (most likely because a field corresponding to a parent object was removed from its class declaration).".Fmt(refID));
        }
    }
}
