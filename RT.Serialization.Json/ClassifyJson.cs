using System.Numerics;
using System.Text;
using RT.Internal;
using RT.Json;

namespace RT.Serialization;

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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Json string to reconstruct object from.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <param name="format">
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static T Deserialize<T>(string json, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
    {
        return Classify.Deserialize<JsonValue, T>(JsonValue.Parse(json), format ?? DefaultFormat, options);
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Json string to reconstruct object from.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <param name="format">
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static object Deserialize(Type type, string json, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
    {
        return Classify.Deserialize(type, JsonValue.Parse(json), format ?? DefaultFormat, options);
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Json string to reconstruct object from.</param>
    /// <param name="intoObject">
    ///     Object to assign values to in order to reconstruct the original object.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <param name="format">
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
    public static void DeserializeIntoObject<T>(string json, T intoObject, ClassifyOptions options = null, IClassifyFormat<JsonValue> format = null)
    {
        Classify.DeserializeIntoObject(JsonValue.Parse(json), intoObject, format ?? DefaultFormat, options);
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
    ///     Implementation of a Classify format. See <see cref="ClassifyJsonFormat"/> for an example.</param>
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
///     (de)serializes it. To have effect, this interface must be implemented by a class and passed into <see
///     cref="ClassifyOptions.AddTypeProcessor{TElement}"/>.</summary>
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

        return
            element is JsonString ? element.GetString() :
            element is JsonBool ? element.GetBool() :
            element is JsonNumber ? ((JsonNumber) element).RawValue :
            null;
    }

    JsonValue IClassifyFormat<JsonValue>.GetSelfValue(JsonValue element)
    {
        return element[":value"];
    }

    IEnumerable<JsonValue> IClassifyFormat<JsonValue>.GetList(JsonValue element, int? tupleSize) =>
        element is JsonDict && element.ContainsKey(":value") ? element[":value"].GetList() : element.GetList();

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
        return consider is JsonDict && consider.ContainsKey(":declaringTypes")
            ? consider[":values"][consider[":declaringTypes"].IndexOf(declaringType)]
            : consider;
    }

    byte[] IClassifyFormat<JsonValue>.GetRawData(JsonValue element)
    {
        if (element is JsonDict && element.ContainsKey(":value"))
            element = element[":value"];

        if (element is JsonList)
            // Support a list of integers as this was how Classify encoded byte arrays before GetRawData was introduced
            return element.GetList().Select(el => (byte) (el.GetIntLenientSafe() ?? 0)).ToArray();

        var str = element.GetStringSafe();
        return str == null ? null : Convert.FromBase64String(str);
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
            throw new InvalidOperationException("The JSON Classify format encountered a contractual violation perpetrated by Classify. GetReferenceID() should not be called unless IsReference() or IsReferable() returned true.");
    }

    JsonValue IClassifyFormat<JsonValue>.FormatNullValue()
    {
        return null;
    }

    JsonValue IClassifyFormat<JsonValue>.FormatSimpleValue(object value)
    {
        if (value == null)
            return null;

        // JSON can’t represent NaN and infinities, so use ExactConvert.ToString() for those.
        return value switch
        {
            double dbl when !double.IsNaN(dbl) && !double.IsInfinity(dbl) => dbl,
            float flt when !float.IsNaN(flt) && !float.IsInfinity(flt) => flt,
            byte b => (long) b,
            sbyte sb => sb,
            short sh => sh,
            ushort ush => (ulong) ush,
            int i => i,
            uint ui => (ulong) ui,
            long l => l,
            ulong ul => ul,
            decimal dc => dc,
            bool bl => bl,
            string str => str,
            BigInteger bi => ExactConvert.Try(bi, out ulong ul) ? ul : (JsonValue) ExactConvert.ToString(bi),
            // This takes care of enum types, DateTime and doubles/floats that are NaN or infinities
            _ => ExactConvert.ToString(value)
        };
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

    JsonValue IClassifyFormat<JsonValue>.FormatRawData(byte[] value)
    {
        return new JsonString(Convert.ToBase64String(value));
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
        throw new InvalidOperationException($@"An object reference ("":ref"": {refID}) was encountered, but no matching object ("":refid"": {refID}) was encountered during deserialization. If such an object is present somewhere in the JSON, the relevant object was not deserialized (most likely because a field corresponding to a parent object was removed from its class declaration).");
    }
}
