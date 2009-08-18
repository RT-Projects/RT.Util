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
    }
}
