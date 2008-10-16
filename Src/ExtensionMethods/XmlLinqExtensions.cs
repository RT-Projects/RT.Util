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
        public static string Path(this XElement element)
        {
            List<string> list = new List<string>();
            while (element != null)
            {
                list.Add(element.Name.ToString());
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
        /// <see fref="XElement.Element()"/>.
        /// </summary>
        public static XElement ChkElement(this XElement element, XName name)
        {
            XElement result = element.Element(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an element named \"{1}\".",
                    element.Path(), name);
            else
                return result;
        }

        /// <summary>
        /// Returns the first attribute matching "name", or if none, throws an exception to say
        /// which element was missing which attribute. This is a counterpart to
        /// <see fref="XElement.Attribute()"/>.
        /// </summary>
        public static XAttribute ChkAttribute(this XElement element, XName name)
        {
            XAttribute result = element.Attribute(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an attribute named \"{1}\".",
                    element.Path(), name);
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
                throw new RTException("Attribute \"{0}\" is expected to contain a number (convertible to \"double\")",
                    attribute.Path());
        }
    }
}
