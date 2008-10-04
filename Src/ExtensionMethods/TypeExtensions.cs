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
        /// Determines whether the current type is or implements the specified single-parameter generic interface.
        /// </summary>
        /// <param name="T">The current type.</param>
        /// <param name="Interface">The single-parameter interface to check for, e.g. typeof(ICollection&lt;&gt;).</param>
        /// <param name="ValueType">Will receive the generic type parameter of the interface.</param>
        /// <returns>True if the current type is or implements the specified single-parameter generic interface.</returns>
        public static bool ImplementsInterface1(this Type T, Type Interface, out Type ValueType)
        {
            ValueType = null;

            if (T.IsGenericType && T.GetGenericTypeDefinition() == Interface)
            {
                ValueType = T.GetGenericArguments()[0];
                return true;
            }

            var Implements = T.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == Interface, null).FirstOrDefault();
            if (Implements == null)
                return false;

            ValueType = Implements.GetGenericArguments()[0];
            return true;
        }

        /// <summary>
        /// Determines whether the current type is or implements the specified two-parameter generic interface.
        /// </summary>
        /// <param name="T">The current type.</param>
        /// <param name="Interface">The two-parameter interface to check for, e.g. typeof(IDictionary&lt;,&gt;).</param>
        /// <param name="KeyType">Will receive the first generic type parameter of the interface.</param>
        /// <param name="ValueType">Will receive the second generic type parameter of the interface.</param>
        /// <returns>True if the current type is or implements the specified two-parameter generic interface.</returns>
        public static bool ImplementsInterface2(this Type T, Type Interface, out Type KeyType, out Type ValueType)
        {
            KeyType = null;
            ValueType = null;

            if (T.IsGenericType && T.GetGenericTypeDefinition() == Interface)
            {
                KeyType = T.GetGenericArguments()[0];
                ValueType = T.GetGenericArguments()[1];
                return true;
            }

            var Implements = T.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == Interface, null).FirstOrDefault();
            if (Implements == null)
                return false;

            KeyType = Implements.GetGenericArguments()[0];
            ValueType = Implements.GetGenericArguments()[1];
            return true;
        }
    }
}
