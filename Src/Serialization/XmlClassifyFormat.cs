using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Serialization
{
    /// <summary>Provides a format to serialize/deserialize objects as XML using <see cref="Classify"/>.</summary>
    public sealed class XmlClassifyFormat : IClassifyFormat<XElement>
    {
        /// <summary>
        ///     Initializes an instance of <see cref="XmlClassifyFormat"/> using default values. (It is advisable to use <see
        ///     cref="ClassifyFormats.Xml"/> instead.)</summary>
        public XmlClassifyFormat() { _rootTagName = "item"; }

        /// <summary>
        ///     Initializes an instance of <see cref="XmlClassifyFormat"/> using the specified options.</summary>
        /// <param name="rootTagName">
        ///     Specifies the XML tag name to use for the root element of a serialized object.</param>
        public XmlClassifyFormat(string rootTagName) { _rootTagName = rootTagName; }

        private string _rootTagName;

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
