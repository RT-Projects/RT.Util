using System.Xml.Linq;
using RT.Serialization;

namespace RT.Util.ExtensionMethods;

/// <summary>
///     Provides extension methods on the classes belonging to the LINQ XML API (<see cref="XElement"/>, <see
///     cref="XContainer"/>, and <see cref="XAttribute"/>).</summary>
public static class XmlLinqExtensions
{
    /// <summary>
    ///     Returns the value of this element, converted to type T. If the element does not exist returns the default value.
    ///     If the element's value cannot be converted, throws an exception.</summary>
    public static T ValueOrDefault<T>(this XElement element, XName name, T defaultValue)
    {
        XElement el = element.Element(name);
        if (el == null)
            return defaultValue;
        else
            try { return ExactConvert.To<T>(el.Value); }
            catch (ExactConvertException E) { throw new InvalidOperationException(("Element \"{0}/{1}\", when present, must contain a value convertible to a certain type: " + E.Message).Fmt(element.Path(), name)); }
    }
}
