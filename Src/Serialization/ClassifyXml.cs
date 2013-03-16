﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Serialization
{
    /// <summary>Offers a convenient way to use <see cref="Classify"/> to serialize objects using the XML format.</summary>
    public static class ClassifyXml
    {
        /// <summary>
        ///     Format used when null is passed to methods that take a format. Make sure not to modify this instance if any thread
        ///     in the application might be in the middle of using <see cref="ClassifyXml"/>; ideally the options shoud be set
        ///     once during startup and never changed after that.</summary>
        public static IClassifyFormat<XElement> DefaultFormat = ClassifyXmlFormat.Default;

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
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T DeserializeFile<T>(string filename, ClassifyOptions options = null, IClassifyFormat<XElement> format = null, object parent = null)
        {
            return Classify.DeserializeFile<XElement, T>(filename, format ?? DefaultFormat, options, parent);
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
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object DeserializeFile(Type type, string filename, ClassifyOptions options = null, IClassifyFormat<XElement> format = null, object parent = null)
        {
            return Classify.DeserializeFile<XElement>(type, filename, format ?? DefaultFormat, options, parent);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="xml">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T Deserialize<T>(XElement xml, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            return Classify.Deserialize<XElement, T>(xml, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <param name="type">
        ///     Type of object to reconstruct.</param>
        /// <param name="xml">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object Deserialize(Type type, XElement xml, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            return Classify.Deserialize<XElement>(type, xml, format ?? DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form by applying the values to an
        ///     existing instance of the type.</summary>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="xml">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        public static void DeserializeIntoObject<T>(XElement xml, T intoObject, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            Classify.DeserializeIntoObject<XElement, T>(xml, intoObject, format ?? DefaultFormat, options);
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
        public static void DeserializeFileIntoObject(string filename, object intoObject, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            Classify.DeserializeFileIntoObject<XElement>(filename, intoObject, format ?? DefaultFormat, options);
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
        public static void SerializeToFile<T>(T saveObject, string filename, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            Classify.SerializeToFile<XElement, T>(saveObject, filename, format ?? DefaultFormat, options);
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
        public static void SerializeToFile(Type saveType, object saveObject, string filename, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            Classify.SerializeToFile<XElement>(saveType, saveObject, filename, format ?? DefaultFormat, options);
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
        public static XElement Serialize<T>(T saveObject, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            return Classify.Serialize<XElement, T>(saveObject, format ?? DefaultFormat, options);
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
        public static XElement Serialize(Type saveType, object saveObject, ClassifyOptions options = null, IClassifyFormat<XElement> format = null)
        {
            return Classify.Serialize<XElement>(saveType, saveObject, format ?? DefaultFormat, options);
        }
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by the object being serialized.</summary>
    public interface IClassifyXmlObjectProcessor : IClassifyObjectProcessor<XElement>
    {
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by a class derived from <see
    ///     cref="ClassifyTypeOptions"/> and associated with a type via <see cref="ClassifyOptions.AddTypeOptions"/>.</summary>
    public interface IClassifyXmlTypeProcessor : IClassifyTypeProcessor<XElement>
    {
    }

    /// <summary>
    ///     Provides a format to serialize/deserialize objects as XML using <see cref="Classify"/> and any serialization options
    ///     which are format-specific. This class can only be instantiated through the factory method <see cref="Create"/>. See
    ///     also <see cref="Default"/>.</summary>
    public sealed class ClassifyXmlFormat : IClassifyFormat<XElement>
    {
        /// <summary>Gets the XML classify format with all options at their defaults.</summary>
        public static IClassifyFormat<XElement> Default { get { return _default ?? (_default = new ClassifyXmlFormat()); } }
        private static ClassifyXmlFormat _default;

        /// <summary>
        ///     Creates an XML classify format with the specified XML-specific options.</summary>
        /// <param name="rootTagName">
        ///     Specifies the XML tag name to use for the root element of a serialized object.</param>
        /// <returns/>
        public static IClassifyFormat<XElement> Create(string rootTagName = "item")
        {
            return new ClassifyXmlFormat { _rootTagName = rootTagName };
        }

        private ClassifyXmlFormat() { }

        private string _rootTagName = "item";

        XElement IClassifyFormat<XElement>.ReadFromStream(Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8))
                return XElement.Load(sr);
        }

        void IClassifyFormat<XElement>.WriteToStream(XElement element, Stream stream)
        {
            element.Save(stream);
        }

        object IClassifyFormat<XElement>.GetSimpleValue(XElement element)
        {
            if (element == null || element.Attribute("null") != null)
                return null;

            var enc = element.Attribute("encoding");
            if (enc == null)
                return element.Value;

            switch (enc.Value)
            {
                case "c-literal":
                    return element.Value.CLiteralUnescape();

                case "base64":
                    return element.Value.Base64UrlDecode().FromUtf8();

                case "codepoint":
                    return (char) int.Parse(element.Value);

                default:
                    throw new InvalidDataException("XmlClassifyFormat does not recognize encoding \"{0}\" on XML tags.".Fmt(enc.Value));
            }
        }

        IEnumerable<XElement> IClassifyFormat<XElement>.GetList(XElement element, int? tupleSize)
        {
            return tupleSize == null
                ? element.Elements("item")
                : Enumerable.Range(1, tupleSize.Value).Select(i => element.Element("item" + i));
        }

        bool IClassifyFormat<XElement>.IsNull(XElement element)
        {
            return element.Attribute("null") != null;
        }

        XElement IClassifyFormat<XElement>.GetSelfValue(XElement element)
        {
            return element.Elements().FirstOrDefault();
        }

        void IClassifyFormat<XElement>.GetKeyValuePair(XElement element, out XElement key, out XElement value)
        {
            key = element.Element("key");
            value = element.Element("value");
        }

        IEnumerable<KeyValuePair<object, XElement>> IClassifyFormat<XElement>.GetDictionary(XElement element)
        {
            foreach (var item in element.Elements("item"))
            {
                var keyAttr = item.Attribute("key");
                if (keyAttr != null)
                    yield return new KeyValuePair<object, XElement>(keyAttr.Value, item);
            }
        }

        bool IClassifyFormat<XElement>.HasField(XElement element, string fieldName)
        {
            return element.Element(fieldName) != null;
        }

        XElement IClassifyFormat<XElement>.GetField(XElement element, string fieldName)
        {
            return element.Element(fieldName);
        }

        bool IClassifyFormat<XElement>.IsReference(XElement element)
        {
            return element.Attribute("ref") != null;
        }

        bool IClassifyFormat<XElement>.IsReferable(XElement element)
        {
            return element.Attribute("refid") != null;
        }

        bool IClassifyFormat<XElement>.IsFollowID(XElement element)
        {
            return element.Attribute("id") != null;
        }

        string IClassifyFormat<XElement>.GetReferenceID(XElement element)
        {
            return
                element.Attribute("refid").NullOr(a => a.Value) ??
                element.Attribute("ref").NullOr(a => a.Value) ??
                element.Attribute("id").NullOr(a => a.Value);
        }

        XElement IClassifyFormat<XElement>.FormatNullValue()
        {
            return new XElement(_rootTagName, new XAttribute("null", "1"));
        }

        XElement IClassifyFormat<XElement>.FormatSimpleValue(object value)
        {
            var elem = new XElement(_rootTagName);
            if (value == null)
                elem.Add(new XAttribute("null", "1"));
            else if (value is string)
            {
                string str = (string) value;
                if (str.Any(ch => ch < ' '))
                {
                    elem.Add(new XAttribute("encoding", "c-literal"));
                    elem.Add(str.CLiteralEscape());
                }
                else
                    elem.Add(str);
            }
            else if (value is char)
            {
                char ch = (char) value;
                if (ch <= ' ')
                {
                    elem.Add(new XAttribute("encoding", "codepoint"));
                    elem.Add((int) ch);
                }
                else
                    elem.Add(ch.ToString());
            }
            else
                elem.Add(ExactConvert.ToString(value));

            return elem;
        }

        XElement IClassifyFormat<XElement>.FormatSelfValue(XElement value)
        {
            return new XElement(_rootTagName, value);
        }

        XElement IClassifyFormat<XElement>.FormatList(bool isTuple, IEnumerable<XElement> values)
        {
            var valuesEvaluated = values.ToArray();
            for (int i = 0; i < valuesEvaluated.Length; i++)
                valuesEvaluated[i].Name = isTuple ? "item" + (i + 1) : "item";
            return new XElement(_rootTagName, valuesEvaluated);
        }

        XElement IClassifyFormat<XElement>.FormatKeyValuePair(XElement key, XElement value)
        {
            key.Name = "key";
            value.Name = "value";
            return new XElement(_rootTagName, key, value);
        }

        XElement IClassifyFormat<XElement>.FormatDictionary(IEnumerable<KeyValuePair<object, XElement>> values)
        {
            return new XElement(_rootTagName, values.Select(kvp =>
            {
                kvp.Value.Name = "item";
                kvp.Value.Add(new XAttribute("key", ExactConvert.ToString(kvp.Key)));
                return kvp.Value;
            }));
        }

        XElement IClassifyFormat<XElement>.FormatObject(IEnumerable<KeyValuePair<string, XElement>> fields)
        {
            return new XElement(_rootTagName, fields.Select(kvp => { kvp.Value.Name = kvp.Key; return kvp.Value; }));
        }

        XElement IClassifyFormat<XElement>.FormatReference(string refId)
        {
            return new XElement(_rootTagName, new XAttribute("ref", refId));
        }

        XElement IClassifyFormat<XElement>.FormatReferable(XElement element, string refId)
        {
            element.Add(new XAttribute("refid", refId));
            return element;
        }

        string IClassifyFormat<XElement>.GetType(XElement element, out bool isFullType)
        {
            isFullType = element.Attribute("fulltype") != null;
            return element.Attribute(isFullType ? "fulltype" : "type").NullOr(e => e.Value);
        }

        XElement IClassifyFormat<XElement>.FormatWithType(XElement element, string type, bool isFullType)
        {
            element.Add(new XAttribute(isFullType ? "fulltype" : "type", type));
            return element;
        }

        XElement IClassifyFormat<XElement>.FormatFollowID(string id)
        {
            return new XElement(_rootTagName, new XAttribute("id", id));
        }
    }
}
