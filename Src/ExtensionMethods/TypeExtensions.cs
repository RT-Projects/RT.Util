using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// <param name="T">The current type.</param>
        /// <param name="Interface">A generic type definition for an interface, e.g. typeof(ICollection&lt;&gt;) or typeof(IDictionary&lt;,&gt;).</param>
        /// <param name="TypeParameters">Will receive an array containing the generic type parameters of the interface.</param>
        /// <returns>True if the current type is or implements the specified generic interface.</returns>
        public static bool TryGetInterfaceGenericParameters(this Type T, Type Interface, out Type[] TypeParameters)
        {
            TypeParameters = null;

            if (T.IsGenericType && T.GetGenericTypeDefinition() == Interface)
            {
                TypeParameters = T.GetGenericArguments();
                return true;
            }

            var Implements = T.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == Interface, null).FirstOrDefault();
            if (Implements == null)
                return false;

            TypeParameters = Implements.GetGenericArguments();
            return true;
        }
    }
}
