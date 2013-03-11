using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Serialization
{
    public static partial class ClassifyFormats
    {
        private class xmlClassifyFormat : IClassifyFormat<XElement>
        {
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

            bool IClassifyFormat<XElement>.GetKeyValuePair(XElement element, out XElement key, out XElement value)
            {
                key = element.Element("key");
                value = element.Element("value");
                return key != null && value != null;
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

            XElement IClassifyFormat<XElement>.FormatNullValue(string name)
            {
                return new XElement(name, new XAttribute("null", "1"));
            }

            XElement IClassifyFormat<XElement>.FormatSimpleValue(string name, object value)
            {
                var elem = new XElement(name);
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

            XElement IClassifyFormat<XElement>.FormatSelfValue(string name, XElement value)
            {
                return new XElement(name, value);
            }

            XElement IClassifyFormat<XElement>.FormatList(string name, bool isTuple, IEnumerable<XElement> values)
            {
                var valuesEvaluated = values.ToArray();
                for (int i = 0; i < valuesEvaluated.Length; i++)
                    valuesEvaluated[i].Name = isTuple ? "item" + (i + 1) : "item";
                return new XElement(name, valuesEvaluated);
            }

            XElement IClassifyFormat<XElement>.FormatKeyValuePair(string name, XElement key, XElement value)
            {
                key.Name = "key";
                value.Name = "value";
                return new XElement(name, key, value);
            }

            XElement IClassifyFormat<XElement>.FormatDictionary(string name, IEnumerable<KeyValuePair<object, XElement>> values)
            {
                return new XElement(name, values.Select(kvp =>
                {
                    kvp.Value.Name = "item";
                    kvp.Value.Add(new XAttribute("key", ExactConvert.ToString(kvp.Key)));
                    return kvp.Value;
                }));
            }

            XElement IClassifyFormat<XElement>.FormatObject(string name, IEnumerable<KeyValuePair<string, XElement>> fields)
            {
                return new XElement(name, fields.Select(kvp => { kvp.Value.Name = kvp.Key; return kvp.Value; }));
            }

            XElement IClassifyFormat<XElement>.FormatReference(string name, string refId)
            {
                return new XElement(name, new XAttribute("ref", refId));
            }

            XElement IClassifyFormat<XElement>.FormatReferable(XElement element, string refId)
            {
                element.Add(new XAttribute("refid", refId));
                return element;
            }

            string IClassifyFormat<XElement>.GetType(XElement element)
            {
                return element.Attribute("type").NullOr(e => e.Value);
            }

            XElement IClassifyFormat<XElement>.FormatWithType(XElement element, string type)
            {
                element.Add(new XAttribute("type", type));
                return element;
            }

            XElement IClassifyFormat<XElement>.FormatFollowID(string name, string id)
            {
                return new XElement(name, new XAttribute("id", id));
            }

            bool IClassifyFormat<XElement>.IsEmpty(XElement element)
            {
                return !element.HasAttributes && !element.HasElements && element.FirstNode == null;
            }
        }
    }
}
