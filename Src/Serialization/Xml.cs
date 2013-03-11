using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RT.Util.Serialization
{
    public sealed class XmlClassifyFormat : IClassifyFormat<XElement>
    {
        public XElement ReadFromStream(Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8))
                return XElement.Load(sr);
        }

        public void WriteToStream(XElement element, Stream stream)
        {
            element.Save(stream);
        }

        public object GetSimpleValue(XElement element)
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

        public IEnumerable<XElement> GetList(XElement element)
        {
            return element.Elements("item");
        }

        public XElement FormatValue(string name, object value)
        {
            throw new NotImplementedException();
        }

        public XElement WithRef(XElement element, string refId)
        {
            throw new NotImplementedException();
        }

        public XElement MakeRef(string name, string refId)
        {
            throw new NotImplementedException();
        }

        public bool IsNull(XElement element)
        {
            return element.Attribute("null") != null;
        }

        public XElement GetSelfValue(XElement element)
        {
            return elem.Elements().FirstOrDefault();
        }

        public void GetKeyValuePair(XElement element, out XElement key, out XElement value)
        {
            key = element.Element("key");
            value = element.Element("value");
        }

        public IEnumerable<KeyValuePair<object, XElement>> GetDictionary(XElement element)
        {
            foreach (var item in element.Elements("item"))
            {
                var keyAttr = item.Attribute("key");
                if (keyAttr != null)
                    yield return new KeyValuePair<object, XElement>(keyAttr.Value, item);
            }
        }

        public bool HasField(XElement element, string fieldName)
        {
            return elem.Element(fieldName) != null;
        }

        public XElement GetField(XElement element, string fieldName)
        {
            return elem.Element(fieldName);
        }

        public bool IsReference(XElement element)
        {
            return elem.Attribute("ref") != null;
        }

        public bool IsReferable(XElement element)
        {
            return elem.Attribute("refid") != null;
        }

        public bool IsFollowID(XElement element)
        {
            return elem.Attribute("id") != null;
        }

        public string GetReferenceID(XElement element)
        {
            return
                element.Attribute("refid").NullOr(a => a.Value) ??
                element.Attribute("ref").NullOr(a => a.Value) ??
                element.Attribute("id").NullOr(a => a.Value);
        }

        public XElement FormatNullValue(string name)
        {
            throw new NotImplementedException();
        }

        public XElement FormatSimpleValue(string name, object value)
        {
            throw new NotImplementedException();
        }

        public XElement FormatSelfValue(string name, XElement value)
        {
            throw new NotImplementedException();
        }

        public XElement FormatList(string name, IEnumerable<XElement> values)
        {
            throw new NotImplementedException();
        }

        public XElement FormatKeyValuePair(string name, XElement key, XElement value)
        {
            throw new NotImplementedException();
        }

        public XElement FormatDictionary(string name, IEnumerable<KeyValuePair<object, XElement>> values)
        {
            throw new NotImplementedException();
        }

        public XElement FormatObject(string name, IEnumerable<KeyValuePair<string, XElement>> fields)
        {
            throw new NotImplementedException();
        }

        public XElement FormatReference(string name, string refId)
        {
            return new XElement(name, new XAttribute("ref", refId));
        }

        public void FormatReferable(XElement element, string refId)
        {
            element.Add(new XAttribute("refid", refId));
        }

        public string GetType(XElement element)
        {
            return element.Attribute("type").NullOr(e => e.Value);
        }

        public void FormatWithType(XElement element, string type)
        {
            element.Add(new XAttribute("type", type));
        }
    }
}
