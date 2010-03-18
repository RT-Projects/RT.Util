using System;
using System.Collections.Generic;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Globalization;

namespace RT.Util
{
    /// <summary>
    /// <para>
    /// Provides functionality similar to <see cref="System.Convert"/>, but ensures that all conversions are lossless and roundtrippable.
    /// Whenever a conversion cannot be performed exactly, an <see cref="ExactConvertException"/> is thrown.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <code>
    /// SUPPORTED TYPES
    ///
    /// Supported types are classified as follows. The categories are used in defining
    /// the behaviour of the various conversion types and are vital to understanding what
    /// to expect of the corner cases.
    ///
    ///   Integer types:
    ///       standard - byte, sbyte, short, ushort, int, uint, long, ulong
    ///       bool - as integer this is defined exactly as 0 or 1
    ///       char - as integer this is the binary value of the char, identical to the "ushort" type
    ///       datetime - as integer, this is the number of ticks of the datetime as UTC.
    ///                  The range is DateTime.MinValue.Ticks ... MaxValue.Ticks.
    ///
    ///   Fractional types:
    ///       single, double
    ///       decimal
    ///
    ///   String type:
    ///       string
    ///
    ///   Unsupported type:
    ///       any other type not listed above
    ///       null reference (*ALWAYS* behaves exactly the same as if there
    ///                       was actually an object of an unsupported type)
    ///
    /// Definition of terms:
    ///     "succeed" / "fail" - these terms do not specify the method by which the outcome of
    ///         a conversion is conveyed. This is a separate aspect discussed later.
    ///
    /// ExactConvert only allows a conversion to succeed if a roundtrip conversion would result in
    /// at most a very small error. The "very small error" only exists when a Fractional
    /// type is the destination - in this case, ExactConvert picks the nearest
    /// representable value.
    ///
    /// A general trait of ExactConvert is that the rules are defined for whole source/destination type
    /// pairs. A necessary exception to this principle is conversion from string, due to the arbitrary
    /// nature of string values.
    ///
    /// As a consequence of the above principle, ExactConvert does not allow
    /// Fractional types to be converted to Integer types.
    ///
    /// Something to beware of: some of the built-in conversions use national strings for
    /// values, e.g. for True/False/Infinity etc. To avoid any issues like programs crashing
    /// on Spanish computers but not on British ones, the following strings are hard-coded.
    /// All conversions _from_ strings are case-insensitive.
    ///
    /// * True
    /// * False
    /// * Inf
    /// * NaN
    /// </code>
    /// </remarks>
    public static class ExactConvert
    {
        private static readonly bool[] _isIntegerType;
        private static readonly bool[] _isUnsignedType;
        private static readonly bool[] _isUnsupportedType;

        /// <summary>
        /// Initialises the internally-used lookup tables for determining what kind
        /// of type is being dealt with (e.g. is this an unsigned type?)
        /// </summary>
        static ExactConvert()
        {
            int max = 0;
            foreach (int value in Enum.GetValues(typeof(TypeCode)))
                if (max < value)
                    max = value;

            // Values will default to false unless set to true explicitly

            _isIntegerType = new bool[max + 1];
            _isIntegerType[(int) TypeCode.Byte] = true;
            _isIntegerType[(int) TypeCode.SByte] = true;
            _isIntegerType[(int) TypeCode.Int16] = true;
            _isIntegerType[(int) TypeCode.Int32] = true;
            _isIntegerType[(int) TypeCode.Int64] = true;
            _isIntegerType[(int) TypeCode.UInt16] = true;
            _isIntegerType[(int) TypeCode.UInt32] = true;
            _isIntegerType[(int) TypeCode.UInt64] = true;
            _isIntegerType[(int) TypeCode.Boolean] = true;
            _isIntegerType[(int) TypeCode.Char] = true;
            _isIntegerType[(int) TypeCode.DateTime] = true;

            _isUnsignedType = new bool[max + 1];
            _isUnsignedType[(int) TypeCode.Byte] = true;
            _isUnsignedType[(int) TypeCode.UInt16] = true;
            _isUnsignedType[(int) TypeCode.UInt32] = true;
            _isUnsignedType[(int) TypeCode.UInt64] = true;
            _isUnsignedType[(int) TypeCode.Boolean] = true;
            _isUnsignedType[(int) TypeCode.Char] = true;

            _isUnsupportedType = new bool[max + 1];
            _isUnsupportedType[(int) TypeCode.DBNull] = true;
            _isUnsupportedType[(int) TypeCode.Empty] = true;
            _isUnsupportedType[(int) TypeCode.Object] = true;
        }

        /// <summary>
        /// Returns true if the specified type is one of the 8 built-in "true" integer types:
        /// the signed and unsigned 8, 16, 32 and 64-bit types.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        public static bool IsTrueIntegerType(Type type)
        {
            var code = Type.GetTypeCode(type);
            return IsTrueIntegerType(code);
        }

        /// <summary>
        /// Returns true if the specified type is one of the 8 built-in "true" integer types:
        /// the signed and unsigned 8, 16, 32 and 64-bit types.
        /// </summary>
        /// <param name="typeCode">The code of the type to be tested - use <see cref="GetTypeCode"/>
        /// to get the code of an object's type.</param>
        public static bool IsTrueIntegerType(TypeCode typeCode)
        {
            return (typeCode != TypeCode.Boolean)
                && (typeCode != TypeCode.Char)
                && (typeCode != TypeCode.DateTime)
                && _isIntegerType[(int) typeCode];
        }

        /// <summary>
        /// Returns true if the specified type is a nullable form of one of the 8 built-in "true" integer types:
        /// the signed and unsigned 8, 16, 32 and 64-bit types.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        public static bool IsTrueIntegerNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsTrueIntegerType(type.GetGenericArguments()[0]);
        }

        /// <summary>
        /// Returns true if the specified type is integer-compatible (in other words, a string of digits can be
        /// converted to it using <see cref="ExactConvert"/>). This includes all types that are <see cref="IsTrueIntegerType(Type)"/>
        /// as well as DateTime, Char and Boolean.
        /// </summary>
        /// <param name="type">The type to be tested.</param>
        public static bool IsIntegerCompatibleType(Type type)
        {
            var code = Type.GetTypeCode(type);
            return IsIntegerCompatibleType(code);
        }

        /// <summary>
        /// Returns true if the specified type is integer-compatible (in other words, a string of digits can be
        /// converted to it using <see cref="ExactConvert"/>). This includes all types that are <see cref="IsTrueIntegerType(Type)"/>
        /// as well as DateTime, Char and Boolean.
        /// </summary>
        /// <param name="typeCode">The code of the type to be tested - use <see cref="GetTypeCode"/>
        /// to get the code of an object's type.</param>
        public static bool IsIntegerCompatibleType(TypeCode typeCode)
        {
            return _isIntegerType[(int) typeCode];
        }

        /// <summary>
        /// C# does not allow a boxed integer type to be unboxed as anything other
        /// than the true type of the boxed integer. This utility function unboxes the
        /// integer as the correct type and then casts it to a long, returning the result.
        ///
        /// Throws an exception if the object is null or not one of the built-in integer
        /// types.
        ///
        /// Does not support unboxing of a ulong because the cast to long would be lossy
        /// and misleading. Will throw an exception when given a boxed ulong.
        /// </summary>
        public static long UnboxIntegerToLong(object integer)
        {
            return UnboxIntegerToLong(integer, GetTypeCode(integer));
        }

        /// <summary>
        /// A faster version of UnboxIntegerToLong(object) if the TypeCode is
        /// already provided. Behaviour is undefined if typeCode does not match
        /// the type of the object passed in.
        /// </summary>
        public static long UnboxIntegerToLong(object integer, TypeCode typeCode)
        {
            if (integer == null)
                throw new NullReferenceException("Cannot unbox a null object to a long.");
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (byte) integer;
                case TypeCode.SByte:
                    return (sbyte) integer;
                case TypeCode.Int16:
                    return (short) integer;
                case TypeCode.UInt16:
                    return (ushort) integer;
                case TypeCode.Int32:
                    return (int) integer;
                case TypeCode.UInt32:
                    return (uint) integer;
                case TypeCode.Int64:
                    return (long) integer;
                case TypeCode.Boolean:
                    return (bool) integer ? 1 : 0;
                case TypeCode.Char:
                    return (char) integer;
                case TypeCode.DateTime:
                    switch (((DateTime) integer).Kind)
                    {
                        case DateTimeKind.Utc:
                        case DateTimeKind.Unspecified:
                            return ((DateTime) integer).Ticks;
                        case DateTimeKind.Local:
                            return ((DateTime) integer).ToUniversalTime().Ticks;
                        default:
                            throw new ArgumentException("Unexpected DateTime.Kind while unboxing it to a long.");
                    }
                default:
                    throw new ArgumentException(string.Format("Cannot unbox an object of type {0} to a long.", typeCode));
            }
        }

        /// <summary>
        /// Crutches needed all around... TypeCode.Empty is described as the type code
        /// for "a null reference". Unfortunately the only way to retrieve a TypeCode
        /// is from a Type object, which can't represent the type of a null reference
        /// (well... actually one can't really talk about a _type_ of a _null_ reference
        /// in C# at all as far as I understand).
        ///
        /// Well anyway, wrapping up the rant, this function fills in the spot of a
        /// function that's clearly missing: Type.GetTypeCode(object), which returns
        /// TypeCode.Empty if asked to get the type of a null object.
        ///
        /// Something at the back of my mind tells me that there's one way of looking at
        /// this where the behaviour of the existing API would make sense... but really,
        /// I think this is how it really should have been since it is a lot more useful.
        /// </summary>
        public static TypeCode GetTypeCode(object value)
        {
            if (value == null)
                return TypeCode.Empty;
            else
                return Type.GetTypeCode(value.GetType());
        }

        /// <summary>
        /// Returns true if the specified type is a supported type for converting to other
        /// types supported by <see cref="ExactConvert"/>.
        /// </summary>
        public static bool IsSupportedType(Type type)
        {
            return !ExactConvert._isUnsupportedType[(int) Type.GetTypeCode(type)];
        }

        /// <summary>
        /// Returns true if the specified type code is that of a supported type for converting to
        /// other types supported by <see cref="ExactConvert"/>.
        /// </summary>
        public static bool IsSupportedType(TypeCode typeCode)
        {
            return !ExactConvert._isUnsupportedType[(int) typeCode];
        }

        #region Try - the main implementation of ExactConvert with all the business code

        // Some general overview on when and how the various exact conversions work:
        //
        // from unsupported: never
        // to integer/standard:
        //     from string: only if type.TryParse works
        //     from integer: only if in range
        //     from fractional: never
        // to integer/bool:
        //     from string: "True"/"False" only, case-insensitive
        //     from integer: only if 0 or 1
        //     from fractional: never
        // to integer/datetime:
        //     from string: YYYY-MM-DD hh:mm:ss.ffff +ZZZZ, where various parts are optional
        //     from integer: only if in range, using binary representation
        //     from fractional: never
        // to fractional:
        //     from string: only if type.TryParse works
        //     from integer: always (because all integer types are in range)
        //     from fractional: always
        // to string:
        //     from string: no conversion
        //     from integer/standard: .ToString()
        //     from integer/bool: .ToString()
        //     from integer/datetime: .ToString() using ISO format with various optional parts
        //     from fractional: .ToString('R') ("roundtrip")

        #region To integer/standard

        #region Unsigned

        /// <summary>
        /// Converts the specified object to a byte.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out byte result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Byte) // fast track if it's already the right type
            {
                result = (byte) value;
                return true;
            }

            else if (code == TypeCode.String)
                return byte.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) byte.MaxValue)
                {
                    result = (byte) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) byte.MinValue && val <= (long) byte.MaxValue)
                {
                    result = (byte) val;
                    return true;
                }
            }

            result = default(byte);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a ushort.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out ushort result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt16) // fast track if it's already the right type
            {
                result = (ushort) value;
                return true;
            }

            else if (code == TypeCode.String)
                return ushort.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) ushort.MaxValue)
                {
                    result = (ushort) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) ushort.MinValue && val <= (long) ushort.MaxValue)
                {
                    result = (ushort) val;
                    return true;
                }
            }

            result = default(ushort);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a uint.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out uint result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt32) // fast track if it's already the right type
            {
                result = (uint) value;
                return true;
            }

            else if (code == TypeCode.String)
                return uint.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) uint.MaxValue)
                {
                    result = (uint) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) uint.MinValue && val <= (long) uint.MaxValue)
                {
                    result = (uint) val;
                    return true;
                }
            }

            result = default(uint);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a ulong.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out ulong result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt64) // fast track if it's already the right type
            {
                result = (ulong) value;
                return true;
            }

            else if (code == TypeCode.String)
                return ulong.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) ulong.MinValue) // note no upper limit here because ulong is special
                {
                    result = (ulong) val;
                    return true;
                }
            }

            result = default(ulong);
            return false;
        }

        #endregion

        #region Signed

        /// <summary>
        /// Converts the specified object to an sbyte.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out sbyte result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.SByte) // fast track if it's already the right type
            {
                result = (sbyte) value;
                return true;
            }

            else if (code == TypeCode.String)
                return sbyte.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) sbyte.MaxValue)
                {
                    result = (sbyte) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) sbyte.MinValue && val <= (long) sbyte.MaxValue)
                {
                    result = (sbyte) val;
                    return true;
                }
            }

            result = default(sbyte);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a short.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out short result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int16) // fast track if it's already the right type
            {
                result = (short) value;
                return true;
            }

            else if (code == TypeCode.String)
                return short.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) short.MaxValue)
                {
                    result = (short) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) short.MinValue && val <= (long) short.MaxValue)
                {
                    result = (short) val;
                    return true;
                }
            }

            result = default(short);
            return false;
        }

        /// <summary>
        /// Converts the specified object to an int.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out int result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int32) // fast track if it's already the right type
            {
                result = (int) value;
                return true;
            }

            else if (code == TypeCode.String)
                return int.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) int.MaxValue)
                {
                    result = (int) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) int.MinValue && val <= (long) int.MaxValue)
                {
                    result = (int) val;
                    return true;
                }
            }

            result = default(int);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a long.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out long result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int64) // fast track if it's already the right type
            {
                result = (long) value;
                return true;
            }

            else if (code == TypeCode.String)
                return long.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) long.MaxValue)
                {
                    result = (long) val;
                    return true;
                }
            }

            else if (_isIntegerType[(long) code])
            {   // special case: no limit test is necessary since all other types fit
                result = UnboxIntegerToLong(value, code);
                return true;
            }

            result = default(long);
            return false;
        }

        #endregion

        #endregion

        #region To integer/bool

        /// <summary>
        /// Converts the specified object to a bool.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        ///
        /// If the value is one of the integer types, the exact conversion only succeeds
        /// if the value is in range, i.e. 0 or 1. If converting from a string, the string
        /// must be exactly (case-insensitive) equal to "True" or "False", or the conversion
        /// will fail.
        /// </summary>
        public static bool Try(object value, out bool result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Boolean) // fast track if it's already the right type
            {
                result = (bool) value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                string val = (string) value;
                result = false;
                if (string.Equals(val, "True", StringComparison.InvariantCultureIgnoreCase))
                    result = true; // result = true, return true
                else if (!string.Equals(val, "False", StringComparison.InvariantCultureIgnoreCase))
                    return false;  // result = default(bool), return false
                return true;       // result = false, return true
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                result = false;
                if (val == 1)
                    result = true;
                else if (val != 0)
                    return false; ;
                return true;
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                result = false;
                if (val == 1)
                    result = true;
                else if (val != 0)
                    return false;
                return true;
            }

            result = default(bool);
            return false;
        }

        #endregion

        #region To integer/char

        /// <summary>
        /// Converts the specified object to a char.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out char result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Char) // fast track if it's already the right type
            {
                result = (char) value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                if (((string) value).Length == 1)
                {
                    result = ((string) value)[0];
                    return true;
                }
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) char.MaxValue)
                {
                    result = (char) val;
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long) char.MinValue && val <= (long) char.MaxValue)
                {
                    result = (char) val;
                    return true;
                }
            }

            result = default(char);
            return false;
        }

        #endregion

        #region To integer/datetime

        /// <summary>
        /// Converts the specified object to a DateTime.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        ///
        /// When converting from string, supports a subset of the ISO 8601 formats - for
        /// more details see <see cref="DateTimeExtensions.TryParseIso"/>.
        /// </summary>
        public static bool Try(object value, out DateTime result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.DateTime) // fast track if it's already the right type
            {
                result = (DateTime) value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                return DateTimeExtensions.TryParseIso((string) value, out result);
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong) value;
                if (val <= (ulong) DateTime.MaxValue.Ticks)
                {
                    result = new DateTime((long) val, DateTimeKind.Utc);
                    return true;
                }
            }

            else if (_isIntegerType[(int) code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= DateTime.MinValue.Ticks && val <= DateTime.MaxValue.Ticks)
                {
                    result = new DateTime(val, DateTimeKind.Utc);
                    return true;
                }
            }

            result = default(DateTime);
            return false;
        }

        #endregion

        #region To fractional

        /// <summary>
        /// Converts the specified object to a float.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out float result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Single) // fast track if it's already the right type
            {
                result = (float) value;
                return true;
            }
            else if (code == TypeCode.Double)
            {
                result = (float) (double) value;
                return true;
            }
            else if (code == TypeCode.Decimal)
            {
                result = (float) (decimal) value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                result = 0;
                if (string.Compare((string) value, "Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.PositiveInfinity;
                else if (string.Compare((string) value, "-Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.NegativeInfinity;
                else if (string.Compare((string) value, "NaN", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.NaN;

                if (result == 0)
                    return float.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                else
                    return true;
            }

            else if (code == TypeCode.UInt64)
            {
                result = (float) (ulong) value; // unbox as ulong, convert to float
                return true;
            }

            else if (_isIntegerType[(int) code])
            {
                result = (float) UnboxIntegerToLong(value, code); // unbox as long, convert to float
                return true;
            }

            result = default(float);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a double.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out double result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Double) // fast track if it's already the right type
            {
                result = (double) value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                result = (double) (float) value;
                return true;
            }
            else if (code == TypeCode.Decimal)
            {
                result = (double) (decimal) value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                result = 0;
                if (string.Compare((string) value, "Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.PositiveInfinity;
                else if (string.Compare((string) value, "-Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.NegativeInfinity;
                else if (string.Compare((string) value, "NaN", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.NaN;

                if (result == 0)
                    return double.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                else
                    return true;
            }

            else if (code == TypeCode.UInt64)
            {
                result = (double) (ulong) value; // unbox as ulong, convert to double
                return true;
            }

            else if (_isIntegerType[(int) code])
            {
                result = (double) UnboxIntegerToLong(value, code); // unbox as long, convert to double
                return true;
            }

            result = default(double);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a decimal.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(object value, out decimal result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Decimal) // fast track if it's already the right type
            {
                result = (decimal) value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                float val = (float) value;
                if (val >= (float) decimal.MinValue && val <= (float) decimal.MaxValue)
                {
                    result = (decimal) val;
                    return true;
                }
            }
            else if (code == TypeCode.Double)
            {
                double val = (double) value;
                if (val >= (double) decimal.MinValue && val <= (double) decimal.MaxValue)
                {
                    result = (decimal) val;
                    return true;
                }
            }

            else if (code == TypeCode.String)
                return decimal.TryParse((string) value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            else if (code == TypeCode.UInt64)
            {
                result = (decimal) (ulong) value; // unbox as ulong, convert to decimal
                return true;
            }

            else if (_isIntegerType[(int) code])
            {
                result = (decimal) UnboxIntegerToLong(value, code); // unbox as long, convert to decimal
                return true;
            }

            result = default(decimal);
            return false;
        }

        #endregion

        #region To string

        /// <summary>
        /// Converts the specified object to a string.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to the type's default value
        /// if the conversion is unsuccessful, which in this case means null (!!!).
        ///
        /// Note that the result will only ever be false if the value is one of the
        /// unsupported types - all supported types can be converted to a string.
        /// (So can the unsupported ones but it's a different matter. Unsupported types
        /// are not supported by this method for consistency with the other overloads.)
        /// </summary>
        public static bool Try(object value, out string result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.String) // fast track if it's already the right type
            {
                result = (string) value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                float val = (float) value;
                if (float.IsPositiveInfinity(val))
                    result = "Inf";
                else if (float.IsNegativeInfinity(val))
                    result = "-Inf";
                else if (float.IsNaN(val))
                    result = "NaN";
                else
                    result = val.ToString("R", CultureInfo.InvariantCulture);
                return true;
            }
            else if (code == TypeCode.Decimal)
            {
                result = ((decimal) value).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            else if (code == TypeCode.Double)
            {
                double val = (double) value;
                if (double.IsPositiveInfinity(val))
                    result = "Inf";
                else if (double.IsNegativeInfinity(val))
                    result = "-Inf";
                else if (double.IsNaN(val))
                    result = "NaN";
                else
                    result = val.ToString("R", CultureInfo.InvariantCulture);
                return true;
            }
            else if (code == TypeCode.Boolean)
            {
                result = (bool) value ? "True" : "False";
                return true;
            }
            else if (code == TypeCode.DateTime)
            {
                result = ((DateTime) value).ToIsoStringOptimal();
                return true;
            }
            else if (!_isUnsupportedType[(int) code])
            {
                result = value.ToString();
                return true;
            }

            result = default(string); // which is null
            return false;
        }

        #endregion

        /// <summary>
        /// Converts the specified object to the type <paramref name="toType"/>.
        ///
        /// Returns true if successful, or false if the object cannot be converted exactly.
        /// <paramref name="result"/> is set to null
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool Try(Type toType, object value, out object result)
        {
            var code = Type.GetTypeCode(toType);
            bool success = false;
            object converted = null;
            switch (code)
            {
                case TypeCode.Boolean:
                    { bool temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Byte:
                    { byte temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.SByte:
                    { sbyte temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Int16:
                    { short temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.UInt16:
                    { ushort temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Int32:
                    { int temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.UInt32:
                    { uint temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Int64:
                    { long temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.UInt64:
                    { ulong temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Single:
                    { float temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Double:
                    { double temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Decimal:
                    { decimal temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.DateTime:
                    { DateTime temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.Char:
                    { char temp; success = Try(value, out temp); converted = temp; break; }
                case TypeCode.String:
                    { string temp; success = Try(value, out temp); converted = temp; break; }
            }
            result = success ? converted : null;
            return success;
        }

        #endregion

        #region Convert - result is an "out" parameter; throw on failure

        /// <summary>
        /// Converts the specified object to a bool.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out bool result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(bool));
        }

        /// <summary>
        /// Converts the specified object to a byte.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out byte result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(byte));
        }

        /// <summary>
        /// Converts the specified object to an sbyte.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out sbyte result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(sbyte));
        }

        /// <summary>
        /// Converts the specified object to a short.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out short result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(short));
        }

        /// <summary>
        /// Converts the specified object to a ushort.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out ushort result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(ushort));
        }

        /// <summary>
        /// Converts the specified object to an int.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out int result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(int));
        }

        /// <summary>
        /// Converts the specified object to a uint.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out uint result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(uint));
        }

        /// <summary>
        /// Converts the specified object to a long.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out long result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(long));
        }

        /// <summary>
        /// Converts the specified object to a ulong.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out ulong result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(ulong));
        }

        /// <summary>
        /// Converts the specified object to a float.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out float result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(float));
        }

        /// <summary>
        /// Converts the specified object to a double.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out double result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(double));
        }

        /// <summary>
        /// Converts the specified object to a decimal.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out decimal result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(decimal));
        }

        /// <summary>
        /// Converts the specified object to a DateTime.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out DateTime result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(DateTime));
        }

        /// <summary>
        /// Converts the specified object to a char.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out char result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(char));
        }

        /// <summary>
        /// Converts the specified object to a string.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static void To(object value, out string result)
        {
            if (!Try(value, out result))
                throw new ExactConvertException(value, typeof(string));
        }

        #endregion

        #region ToType - result is returned; throw on failure

        /// <summary>
        /// Converts the specified object to a bool.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static bool ToBool(object value)
        {
            bool result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a byte.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static byte ToByte(object value)
        {
            byte result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to an sbyte.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static sbyte ToSByte(object value)
        {
            sbyte result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a short.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static short ToShort(object value)
        {
            short result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a ushort.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static ushort ToUShort(object value)
        {
            ushort result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to an int.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static int ToInt(object value)
        {
            int result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a uint.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static uint ToUInt(object value)
        {
            uint result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a long.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static long ToLong(object value)
        {
            long result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a ulong.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static ulong ToULong(object value)
        {
            ulong result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a float.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static float ToFloat(object value)
        {
            float result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a double.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static double ToDouble(object value)
        {
            double result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a decimal.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static decimal ToDecimal(object value)
        {
            decimal result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a DateTime.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static DateTime ToDateTime(object value)
        {
            DateTime result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a char.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static char ToChar(object value)
        {
            char result;
            To(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a string.
        /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static string ToString(object value)
        {
            string result;
            To(value, out result);
            return result;
        }

        #endregion

        /// <summary>
        /// Converts the value to type <paramref name="toType"/>. Throws an
        /// <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static object To(Type toType, object value)
        {
            object result;
            if (!Try(toType, value, out result))
                throw new ExactConvertException(value, toType);
            return result;
        }

        /// <summary>
        /// Converts the value to type <typeparamref name="T"/>. Throws an
        /// <see cref="ExactConvertException"/> if the object cannot be converted exactly.
        /// </summary>
        public static T To<T>(object value)
        {
            TypeCode code = Type.GetTypeCode(typeof(T));
            switch (code)
            {
                case TypeCode.Boolean:
                    return (T) (object) ToBool(value);
                case TypeCode.Byte:
                    return (T) (object) ToByte(value);
                case TypeCode.SByte:
                    return (T) (object) ToSByte(value);
                case TypeCode.Int16:
                    return (T) (object) ToShort(value);
                case TypeCode.UInt16:
                    return (T) (object) ToUShort(value);
                case TypeCode.Int32:
                    return (T) (object) ToInt(value);
                case TypeCode.UInt32:
                    return (T) (object) ToUInt(value);
                case TypeCode.Int64:
                    return (T) (object) ToLong(value);
                case TypeCode.UInt64:
                    return (T) (object) ToULong(value);
                case TypeCode.Single:
                    return (T) (object) ToFloat(value);
                case TypeCode.Double:
                    return (T) (object) ToDouble(value);
                case TypeCode.Decimal:
                    return (T) (object) ToDecimal(value);
                case TypeCode.DateTime:
                    return (T) (object) ToDateTime(value);
                case TypeCode.Char:
                    return (T) (object) ToChar(value);
                case TypeCode.String:
                    return (T) (object) ToString(value);
                default:
                    throw new ExactConvertException(value, typeof(T));
            }
        }

        /// <summary>
        /// Contains static methods to perform an exact conversion to a nullable type.
        /// These methods return null only if the input is null. A failed conversion
        /// results in an <see cref="ExactConvertException"/>.
        /// </summary>
        public static class ToNullable
        {
            /// <summary>
            /// Converts the specified object to a nullable bool.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static bool? Bool(object value)
            {
                if (value == null) return null;
                else return ToBool(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable byte.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static byte? Byte(object value)
            {
                if (value == null) return null;
                else return ToByte(value);
            }

            /// <summary>
            /// Converts the specified object to an nullable sbyte.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static sbyte? SByte(object value)
            {
                if (value == null) return null;
                else return ToSByte(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable short.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static short? Short(object value)
            {
                if (value == null) return null;
                else return ToShort(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable ushort.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static ushort? UShort(object value)
            {
                if (value == null) return null;
                else return ToUShort(value);
            }

            /// <summary>
            /// Converts the specified object to an nullable int.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static int? Int(object value)
            {
                if (value == null) return null;
                else return ToInt(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable uint.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static uint? UInt(object value)
            {
                if (value == null) return null;
                else return ToUInt(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable long.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static long? Long(object value)
            {
                if (value == null) return null;
                else return ToLong(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable ulong.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static ulong? ULong(object value)
            {
                if (value == null) return null;
                else return ToULong(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable float.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static float? Float(object value)
            {
                if (value == null) return null;
                else return ToFloat(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable double.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static double? Double(object value)
            {
                if (value == null) return null;
                else return ToDouble(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable decimal.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static decimal? Decimal(object value)
            {
                if (value == null) return null;
                else return ToDecimal(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable DateTime.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static DateTime? DateTime(object value)
            {
                if (value == null) return null;
                else return ToDateTime(value);
            }

            /// <summary>
            /// Converts the specified object to a nullable char.
            /// Returns null if <paramref name="value"/> is null.
            /// Throws an <see cref="ExactConvertException"/> if the object cannot be converted exactly.
            /// </summary>
            public static char? Char(object value)
            {
                if (value == null) return null;
                else return ToChar(value);
            }
        }
    }

    /// <summary>
    /// Represents an exception thrown in the case of conversion failure when using <see cref="ExactConvert"/>.
    /// </summary>
    public class ExactConvertException : RTException
    {
        /// <summary>
        /// Initialises an exception to represent conversion failure when using <see cref="ExactConvert"/>.
        /// </summary>
        public ExactConvertException(object value, Type targetType)
        {
            TypeCode from = ExactConvert.GetTypeCode(value);
            TypeCode to = Type.GetTypeCode(targetType);
            _message = string.Format("Cannot do an exact conversion from value \"{2}\" of type \"{0}\" to type \"{1}\").", from, to, value);
        }
    }

}
