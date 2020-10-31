using System;
using System.Linq;
using System.Reflection;

namespace RT.Serialization
{
    /// <summary>
    ///     Serves as a base class for all classify attributes which can be re-declared in your own library, 
    ///     and is not intended to be used by consumers of Classify directly.</summary>
    public class ClassifyAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, it is ignored by <see
    ///     cref="Classify"/>. Data stored in this field or automatically-implemented property is not persisted.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyIgnoreAttribute : ClassifyAttribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if the value is null, 0, false, etc. If it is used on a type, it applies to all fields and
    ///     automatically-implemented properties in the type. See also remarks.</summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Using this together with <see cref="ClassifyIgnoreIfEmptyAttribute"/> will cause the distinction between
    ///             null and an empty collection to be lost. However, a collection containing only null elements is persisted
    ///             correctly.</description></item>
    ///         <item><description>
    ///             Do not use this custom attribute on a field that has a non-default value set in the containing class’s
    ///             constructor. Doing so will cause a serialized null/0/false value to revert to that constructor value upon
    ///             deserialization.</description></item></list></remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class ClassifyIgnoreIfDefaultAttribute : ClassifyAttribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if that serialization would be completely empty. If it is used on a type, it applies to all
    ///     collection-type fields in the type. See also remarks.</summary>
    /// <remarks>
    ///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between null
    ///     and an empty collection to be lost. However, a collection containing only null elements is persisted correctly.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class ClassifyIgnoreIfEmptyAttribute : ClassifyAttribute { }

    /// <summary>
    ///     Specifies that Classify shall not set this field or automatically-implemented property to <c>null</c>. If the
    ///     serialized form is <c>null</c>, the field or automatically-implemented property is instead left at the default
    ///     value assigned by the object’s default constructor.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyNotNullAttribute : ClassifyAttribute { }

    /// <summary>
    ///     To be used on a field or automatically-implemented property of an enum type or a collection involving an enum
    ///     type. Specifies that Classify shall not allow integer values that are not explicitly declared in the relevant enum
    ///     type. If the serialized form is such an integer, fields or automatically-implemented properties of an enum type
    ///     are instead left at the default value assigned by the object’s default constructor, while in collections, the
    ///     relevant element is omitted (changing the size of the collection). If the enum type has the [Flags] attribute,
    ///     bitwise combinations of the declared values are allowed.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyEnforceEnumAttribute : ClassifyAttribute { }

    internal static class ClassifyAttributeExtensions
    {
        public static bool IsDefinedOrIsNamedLike<T>(this MemberInfo member, bool inherit = false)
        {
            return member.IsDefined(typeof(T), inherit) || member.GetCustomAttributes(inherit).Any(a => a.GetType().Name == typeof(T).Name);
        }
    }
}
