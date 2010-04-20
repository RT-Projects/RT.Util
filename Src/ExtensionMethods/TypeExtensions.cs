using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines whether the current type is or implements the specified generic interface, and determines that interface's generic type parameters.
        /// </summary>
        /// <param name="type">The current type.</param>
        /// <param name="interface">A generic type definition for an interface, e.g. typeof(ICollection&lt;&gt;) or typeof(IDictionary&lt;,&gt;).</param>
        /// <param name="typeParameters">Will receive an array containing the generic type parameters of the interface.</param>
        /// <returns>True if the current type is or implements the specified generic interface.</returns>
        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
        {
            typeParameters = null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
            {
                typeParameters = type.GetGenericArguments();
                return true;
            }

            var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
            if (implements == null)
                return false;

            typeParameters = implements.GetGenericArguments();
            return true;
        }

        /// <summary>
        /// Returns all fields contained in the specified type, including private fields inherited from base classes.
        /// </summary>
        /// <param name="type">The type to return all fields of.</param>
        /// <returns>An <see cref="IEnumerable&lt;FieldInfo&gt;"/> containing all fields contained in this type, including private fields inherited from base classes.</returns>
        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var baseType = type.BaseType;
            return (baseType == null) ? fields : GetAllFields(baseType).Concat(fields);
        }

        /// <summary>
        /// Returns all properties contained in the specified type, including private properties inherited from base classes.
        /// </summary>
        /// <param name="type">The type to return all properties of.</param>
        /// <returns>An <see cref="IEnumerable&lt;PropertyInfo&gt;"/> containing all properties contained in this type, including private properties inherited from base classes.</returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var baseType = type.BaseType;
            return (baseType == null) ? properties : GetAllProperties(baseType).Concat(properties);
        }

        /// <summary>
        /// Returns a proper statically-typed collection of the custom attributes on the current member.
        /// </summary>
        /// <param name="member">Member whose custom attributes to return.</param>
        /// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
        /// <typeparam name="T">The type of attribute to search for. Only attributes that are assignable to this type are returned.</typeparam>
        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo member, bool inherit = false)
        {
            return member.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        /// <summary>
        /// Returns a proper statically-typed collection of the custom attributes on the current assembly.
        /// </summary>
        /// <param name="assembly">Assembly whose custom attributes to return.</param>
        /// <typeparam name="T">The type of attribute to search for. Only attributes that are assignable to this type are returned.</typeparam>
        public static IEnumerable<T> GetCustomAttributes<T>(this Assembly assembly)
        {
            return assembly.GetCustomAttributes(typeof(T), false).Cast<T>();
        }

        /// <summary>
        /// Indicates whether one or more instance of the specified attribute type is applied to this member.
        /// </summary>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        /// <param name="member">Member whose custom attributes to search.</param>
        /// <param name="inherit">Specifies whether to search this member's inheritance chain to find the attributes.</param>
        public static bool IsDefined<T>(this MemberInfo member, bool inherit = false)
        {
            return member.IsDefined(typeof(T), inherit);
        }
    }
}
