using System;
using System.Linq;
using System.Reflection;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        ///     Returns the set of custom attributes of the specified <typeparamref name="TAttribute"/> type that are attached
        ///     to the declaration of the enum value represented by <paramref name="enumValue"/>.</summary>
        /// <typeparam name="TAttribute">
        ///     The type of the custom attributes to retrieve.</typeparam>
        /// <param name="enumValue">
        ///     The enum value for which to retrieve the custom attributes.</param>
        /// <returns>
        ///     An array containing the custom attributes, or <c>null</c> if <paramref name="enumValue"/> does not correspond
        ///     to a declared value.</returns>
        /// <remarks>
        ///     This method keeps an internal cache forever.</remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="enumValue"/> is <c>null</c>.</exception>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));
            var enumType = enumValue.GetType();
            var dic = EnumAttributeCache<TAttribute>.Dictionary;
            TAttribute[] arr;
            if (!dic.ContainsKeys(enumType, enumValue))
            {
                arr = null;
                foreach (var field in enumType.GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    var attrs = field.GetCustomAttributes<TAttribute>().ToArray();
                    var enumVal = (Enum) field.GetValue(null);
                    dic.AddSafe(enumType, enumVal, attrs);
                    if (enumVal.Equals(enumValue))
                        arr = attrs;
                }
                return arr;
            }
            return dic.TryGetValue(enumType, enumValue, out arr) ? arr : null;
        }

        /// <summary>
        ///     Returns the single custom attribute of the specified <typeparamref name="TAttribute"/> type that is attached
        ///     to the declaration of the enum value represented by <paramref name="enumValue"/>, or <c>null</c> if there is
        ///     no such attribute.</summary>
        /// <typeparam name="TAttribute">
        ///     The type of the custom attribute to retrieve.</typeparam>
        /// <param name="enumValue">
        ///     The enum value for which to retrieve the custom attribute.</param>
        /// <returns>
        ///     The custom attribute, or <c>null</c> if the enum value does not have a custom attribute of the specified type
        ///     attached to it. If <paramref name="enumValue"/> does not correspond to a declared enum value, or there is more
        ///     than one custom attribute of the same type, an exception is thrown.</returns>
        /// <remarks>
        ///     This method uses <see cref="Ut.GetCustomAttributes{TAttribute}(Enum)"/>, which keeps an internal cache
        ///     forever.</remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="enumValue"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     There is more than one custom attribute of the specified type attached to the enum value declaration.</exception>
        public static TAttribute GetCustomAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));
            return GetCustomAttributes<TAttribute>(enumValue).SingleOrDefault();
        }

        /// <summary>Returns true if this value is equal to the default value for this type.</summary>
        public static bool IsDefault<T>(this T val) where T : struct
        {
            return val.Equals(default(T));
        }

        /// <summary>
        ///     Creates a delegate using Action&lt;,*&gt; or Func&lt;,*&gt; depending on the number of parameters of the
        ///     specified method.</summary>
        /// <param name="firstArgument">
        ///     Object to call the method on, or null for static methods.</param>
        /// <param name="method">
        ///     The method to call.</param>
        public static Delegate CreateDelegate(object firstArgument, MethodInfo method)
        {
            var param = method.GetParameters();
            return Delegate.CreateDelegate(
                method.ReturnType == typeof(void)
                    ? param.Length == 0 ? typeof(Action) : actionType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).ToArray())
                    : funcType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).Concat(method.ReturnType).ToArray()),
                firstArgument,
                method
            );
        }

        private static Type funcType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Func<>);
                case 1: return typeof(Func<,>);
                case 2: return typeof(Func<,,>);
                case 3: return typeof(Func<,,,>);
                case 4: return typeof(Func<,,,,>);
                case 5: return typeof(Func<,,,,,>);
                case 6: return typeof(Func<,,,,,,>);
                case 7: return typeof(Func<,,,,,,,>);
                case 8: return typeof(Func<,,,,,,,,>);
                case 9: return typeof(Func<,,,,,,,,,>);
                case 10: return typeof(Func<,,,,,,,,,,>);
                case 11: return typeof(Func<,,,,,,,,,,,>);
                case 12: return typeof(Func<,,,,,,,,,,,,>);
                case 13: return typeof(Func<,,,,,,,,,,,,,>);
                case 14: return typeof(Func<,,,,,,,,,,,,,,>);
                case 15: return typeof(Func<,,,,,,,,,,,,,,,>);
                case 16: return typeof(Func<,,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", nameof(numParameters));
        }

        private static Type actionType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Action);
                case 1: return typeof(Action<>);
                case 2: return typeof(Action<,>);
                case 3: return typeof(Action<,,>);
                case 4: return typeof(Action<,,,>);
                case 5: return typeof(Action<,,,,>);
                case 6: return typeof(Action<,,,,,>);
                case 7: return typeof(Action<,,,,,,>);
                case 8: return typeof(Action<,,,,,,,>);
                case 9: return typeof(Action<,,,,,,,,>);
                case 10: return typeof(Action<,,,,,,,,,>);
                case 11: return typeof(Action<,,,,,,,,,,>);
                case 12: return typeof(Action<,,,,,,,,,,,>);
                case 13: return typeof(Action<,,,,,,,,,,,,>);
                case 14: return typeof(Action<,,,,,,,,,,,,,>);
                case 15: return typeof(Action<,,,,,,,,,,,,,,>);
                case 16: return typeof(Action<,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", nameof(numParameters));
        }
    }
}
