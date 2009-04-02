using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Extension methods for the classes belonging to the LINQ XML API.
    /// </summary>
    public static class XmlLinqExtensions
    {
        /// <summary>
        /// Returns the path to this element. The path consists of the names of all parents
        /// of this element up to the root node, separated with forward slashes.
        /// </summary>
        public static string Path(this XContainer element)
        {
            List<string> list = new List<string>();
            while (element != null && element is XElement)
            {
                list.Add(((XElement) element).Name.ToString());
                element = element.Parent;
            }
            list.Reverse();
            return "/".Join(list);
        }

        /// <summary>
        /// Returns the path of this attribute. The path consists of the <see fref="XElement.Path()"/>
        /// of this attribute's element, followed by attribute name in square brackets.
        /// </summary>
        public static string Path(this XAttribute attribute)
        {
            return attribute.Parent.Path() + "[" + attribute.Name + "]";
        }

        /// <summary>
        /// Returns the first element matching "name", or if none, throws an exception to say
        /// which element was missing which sub-element. This is a counterpart to
        /// <see cref="XContainer.Element"/>.
        /// </summary>
        public static XElement ChkElement(this XContainer element, XName name)
        {
            XElement result = element.Element(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an element named \"{1}\".".Fmt(element.Path(), name));
            else
                return result;
        }

        /// <summary>
        /// Returns the first attribute matching "name", or if none, throws an exception to say
        /// which element was missing which attribute. This is a counterpart to
        /// <see cref="XElement.Attribute"/>.
        /// </summary>
        public static XAttribute ChkAttribute(this XElement element, XName name)
        {
            XAttribute result = element.Attribute(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an attribute named \"{1}\".".Fmt(element.Path(), name));
            else
                return result;
        }

        /// <summary>
        /// Returns the value of this attribute, converted to a double. If the conversion
        /// cannot succeed, throws an exception describing which attribute was expected to be a double.
        /// </summary>
        public static double AsDouble(this XAttribute attribute)
        {
            string value = attribute.Value;
            double result;
            if (double.TryParse(value, out result))
                return result;
            else
                throw new RTException("Attribute \"{0}\" is expected to contain a number (convertible to \"double\")".Fmt(attribute.Path()));
        }

        /// <summary>
        /// Returns the value of this element, converted to type T. If the element does not exist returns
        /// the default value. If the element's value cannot be converted, throws an exception.
        /// </summary>
        public static T ValueOrDefault<T>(this XElement element, XName name, T defaultValue)
        {
            XElement el = element.Element(name);
            if (el == null)
                return defaultValue;
            else
                try { return RConvert.Exact<T>(el.Value); }
                catch (RConvertException E) { throw new RTException(("Element \"{0}\", when present, must contain a value convertible to a certain type: " + E.Message).Fmt(element.Path())); }
        }
    }
}
