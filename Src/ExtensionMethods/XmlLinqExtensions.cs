using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    ///     Provides extension methods on the classes belonging to the LINQ XML API (<see cref="XElement"/>, <see
    ///     cref="XContainer"/>, and <see cref="XAttribute"/>).</summary>
    public static class XmlLinqExtensions
    {
        /// <summary>
        ///     Returns the path to this element. The path consists of the names of all parents of this element up to the root
        ///     node, separated with forward slashes.</summary>
        public static string Path(this XContainer element)
        {
            List<string> list = new List<string>();
            while (element != null && element is XElement)
            {
                list.Add(((XElement) element).Name.ToString());
                element = element.Parent;
            }
            list.Reverse();
            return list.JoinString("/");
        }

        /// <summary>
        ///     Returns the path of this attribute. The path consists of the <see fref="XElement.Path()"/> of this attribute's
        ///     element, followed by attribute name in square brackets.</summary>
        public static string Path(this XAttribute attribute)
        {
            return attribute.Parent.Path() + "[" + attribute.Name + "]";
        }

        /// <summary>
        ///     Returns the first element matching "name", or if none, throws an exception to say which element was missing
        ///     which sub-element. This is a counterpart to <see cref="XContainer.Element"/>.</summary>
        public static XElement ChkElement(this XContainer element, XName name)
        {
            XElement result = element.Element(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an element named \"{1}\".".Fmt(element.Path(), name));
            else
                return result;
        }

        /// <summary>
        ///     Returns the first attribute matching "name", or if none, throws an exception to say which element was missing
        ///     which attribute. This is a counterpart to <see cref="XElement.Attribute"/>.</summary>
        public static XAttribute ChkAttribute(this XElement element, XName name)
        {
            XAttribute result = element.Attribute(name);
            if (result == null)
                throw new RTException("Element \"{0}\" is expected contain an attribute named \"{1}\".".Fmt(element.Path(), name));
            else
                return result;
        }

        /// <summary>
        ///     Returns the value of this attribute, converted to a double. If the conversion cannot succeed, throws an
        ///     exception describing which attribute was expected to be a double.</summary>
        public static double AsDouble(this XAttribute attribute)
        {
            string value = attribute.Value;
            double result;
            if (double.TryParse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result))
                return result;
            else
                throw new RTException("Attribute \"{0}\" is expected to contain a number (convertible to \"double\")".Fmt(attribute.Path()));
        }

        /// <summary>
        ///     Returns the value of this element, converted to type T. If the element does not exist returns the default
        ///     value. If the element's value cannot be converted, throws an exception.</summary>
        public static T ValueOrDefault<T>(this XElement element, XName name, T defaultValue)
        {
            XElement el = element.Element(name);
            if (el == null)
                return defaultValue;
            else
                try { return ExactConvert.To<T>(el.Value); }
                catch (ExactConvertException E) { throw new RTException(("Element \"{0}/{1}\", when present, must contain a value convertible to a certain type: " + E.Message).Fmt(element.Path(), name)); }
        }

        /// <summary>
        ///     Gets the first (in document order) child element with the specified local name (ignoring the namespace).</summary>
        /// <param name="element">
        ///     XML element to search.</param>
        /// <param name="name">
        ///     The name to match.</param>
        /// <returns>
        ///     The first element found or <c>null</c> if no such element exists.</returns>
        public static XElement ElementI(this XElement element, string name)
        {
            return element.Elements().FirstOrDefault(elem => elem.Name.LocalName == name);
        }

        /// <summary>
        ///     Returns a filtered collection of the child elements of this element or document, in document order. Only
        ///     elements that have the specified local name (ignoring the namespace) are included in the collection.</summary>
        /// <param name="element">
        ///     XML element to search.</param>
        /// <param name="name">
        ///     The name to match.</param>
        /// <returns>
        ///     The first element found or <c>null</c> if no such element exists.</returns>
        public static IEnumerable<XElement> ElementsI(this XContainer element, string name)
        {
            return element.Elements().Where(elem => elem.Name.LocalName == name);
        }

        /// <summary>
        ///     Returns the first attribute of this element that has the specified local name (ignoring the namespace).</summary>
        /// <param name="element">
        ///     XML element to search.</param>
        /// <param name="name">
        ///     The name to match.</param>
        /// <returns>
        ///     The first attribute if found or <c>null</c> if no such attribute exists.</returns>
        public static XAttribute AttributeI(this XElement element, string name)
        {
            return element.Attributes().FirstOrDefault(attr => attr.Name.LocalName == name);
        }

        /// <summary>
        ///     Returns a filtered collection of attributes of this element. Only elements that have the specified local name
        ///     (ignoring the namespace) are included in the collection.</summary>
        /// <param name="element">
        ///     XML element to search.</param>
        /// <param name="name">
        ///     The name to match.</param>
        /// <returns>
        ///     The filtered collection of attributes..</returns>
        public static IEnumerable<XAttribute> AttributesI(this XElement element, string name)
        {
            return element.Attributes().Where(attr => attr.Name.LocalName == name);
        }
    }
}
