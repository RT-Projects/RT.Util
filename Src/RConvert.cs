using System;
using System.Collections.Generic;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    /// Offers various types of conversion routines from an "object"-typed variable
    /// holding one of the supported types to another one of the supported types.
    /// 
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
    /// CONVERSION KINDS
    /// 
    /// Logically, the following kinds of conversions can be considered. This class currently
    /// implements a subset of these conversions since they requre a lot of effort and code
    /// and may never actually come in handy. The list attempts to be complete for academic
    /// reasons (and also to ensure that the most important and most disjoint kinds can be
    /// chosen to be implemented).
    /// 
    /// Definition of terms:
    ///     "succeed" / "fail" - these terms do not specify the method by which the outcome of
    ///         a conversion is conveyed. This is a separate aspect discussed later.
    ///         
    /// Roundtrip:
    ///     Converting from an object of type A to type B only succeeds if a roundtrip conversion
    ///     from the resulting value (of type B) can be converted back to a value of type A which
    ///     is equal to the original value.
    ///     
    ///     This kind is made tricky by the fact that "equal" may mean different things. It could
    ///     mean:
    ///       - binary identity, in which case the various valid representations of double.NaN
    ///         and the various non-normalized variants of strings must be preserved exactly
    ///       - (some hard-to-define inbetween type of identity, of which there are probably
    ///          loads, each one useful in certain rare circumstances)
    ///       - equality identity, which just means that == (and .Equals) say that the "before"
    ///         and "after" are equal. This is also ill-defined because this one is impossible
    ///         for "single" and "double", as double.NaN != double.NaN
    ///         
    ///     Hence it seems that the only possibility for roundtrip conversions is by preserving
    ///     the exact binary information.
    ///
    /// Exact:
    ///     Only allows a conversion to succeed if a roundtrip conversion would result in
    ///     at most a very small error. The "very small error" only exists when a Fractional
    ///     type is the destination - in this case, the Exact conversion picks the nearest
    ///     representable value.
    ///     
    ///     A general trait of the Exact conversion is that the rules are defined for
    ///     whole source/destination type pairs - compare to the Approximating conversion,
    ///     in which certain values may be convertible while others may not. A necessary
    ///     exception to this principle is conversion from string, due to the arbitrary
    ///     nature of string values.
    ///     
    ///     As a consequence of the above principle, the Exact conversion does not allow
    ///     Fractional types to be converted to Integer types - at all.
    ///     
    /// Approx(imating):
    ///     This type attempts to convert the value to the nearest representable value in the
    ///     destination type. For example, it can convert the string "47.3" to the int 47.
    ///     This type is more lenient with bools also, allowing the conversion of any non-zero
    ///     number to the true bool, as well as the conversion of a string holding such a number.
    ///     Note that this type still does error checking and will not convert complete
    ///     nonsense, so e.g. the string "" cannot be converted to an int or a bool, and
    ///     neither can the string "asldfjkl" or double.NaN. However, double.PositiveInfinity
    ///     can easily be converted to an integer type's MaxValue.
    ///     
    ///     As a fairly special case, double.NegativeInfinity is not considered convertible to
    ///     the MinValue of unsigned integer types (including bool & char).
    /// 
    /// Duck:
    ///     A very lenient and very lossy kind, this is basically the same as the Approximating
    ///     conversion except that whenever the Approximating conversion would fail, the Duck
    ///     conversion simply succeeds while returning a predefined value. E.g. converting a
    ///     string like "aifuhdwlk" to an int would result in 0.
    ///     
    ///     Note that Duck conversion could also be seen as merely a kind of failure handling
    ///     mechanism for the Approximating conversion.
    /// 
    /// HANDLING FAILURES
    /// 
    /// This is mainly an API question. The most efficient way (both in speed and expressive
    /// power of the API) seems to be to return a bool indicating success/failure while
    /// putting the result into an "out"-typed parameter. This has the advantage that the
    /// methods can be easily overloaded and used unambiguously (due to C#'s strictness
    /// regarding "out" parameter types).
    /// 
    /// Other possibilities are:
    ///   - throw an exception on failure
    ///   - return a supplied default value on failure
    ///   - return a pre-defined default value on failure
    /// 
    /// Due to the size/complexity of this class, and also to keep the API more clean and
    /// testable, no alternative failure handling mechanisms are provided. These can be
    /// easily layered on top of this class.
    /// 
    /// ------------------------
    /// NOTES
    /// 
    /// - For more detail see comments relating to various regions, e.g. the Exact region!
    /// 
    /// - Something to beware of: some of the built-in conversions use national strings for
    ///   values, e.g. for True/False/Infinity etc. To avoid any issues like programs crashing
    ///   on Spanish computers but not on British ones, the following strings are hard-coded.
    ///   All conversions _from_ strings are case-insensitive.
    ///   
    ///   * True
    ///   * False
    ///   * Inf
    ///   * NaN
    /// </summary>
    public static class RConvert
    {
        private static readonly bool[] IsIntegerType;
        private static readonly bool[] IsUnsignedType;
        public static readonly bool[] IsUnsupportedType;

        /// <summary>
        /// Initialises the internally-used lookup tables for determining what kind
        /// of type is being dealt with (e.g. is this an unsigned type?)
        /// </summary>
        static RConvert()
        {
            int max = 0;
            foreach (int value in Enum.GetValues(typeof(TypeCode)))
                if (max < value)
                    max = value;

            // Values will default to false unless set to true explicitly

            IsIntegerType = new bool[max+1];
            IsIntegerType[(int)TypeCode.Byte] = true;
            IsIntegerType[(int)TypeCode.SByte] = true;
            IsIntegerType[(int)TypeCode.Int16] = true;
            IsIntegerType[(int)TypeCode.Int32] = true;
            IsIntegerType[(int)TypeCode.Int64] = true;
            IsIntegerType[(int)TypeCode.UInt16] = true;
            IsIntegerType[(int)TypeCode.UInt32] = true;
            IsIntegerType[(int)TypeCode.UInt64] = true;
            IsIntegerType[(int)TypeCode.Boolean] = true;
            IsIntegerType[(int)TypeCode.Char] = true;
            IsIntegerType[(int)TypeCode.DateTime] = true;

            IsUnsignedType = new bool[max+1];
            IsUnsignedType[(int)TypeCode.Byte] = true;
            IsUnsignedType[(int)TypeCode.UInt16] = true;
            IsUnsignedType[(int)TypeCode.UInt32] = true;
            IsUnsignedType[(int)TypeCode.UInt64] = true;
            IsUnsignedType[(int)TypeCode.Boolean] = true;
            IsUnsignedType[(int)TypeCode.Char] = true;

            IsUnsupportedType = new bool[max+1];
            IsUnsupportedType[(int)TypeCode.DBNull] = true;
            IsUnsupportedType[(int)TypeCode.Empty] = true;
            IsUnsupportedType[(int)TypeCode.Object] = true;
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
                    return (byte)integer;
                case TypeCode.SByte:
                    return (sbyte)integer;
                case TypeCode.Int16:
                    return (short)integer;
                case TypeCode.UInt16:
                    return (ushort)integer;
                case TypeCode.Int32:
                    return (int)integer;
                case TypeCode.UInt32:
                    return (uint)integer;
                case TypeCode.Int64:
                    return (long)integer;
                case TypeCode.Boolean:
                    return (bool)integer ? 1 : 0;
                case TypeCode.Char:
                    return (char)integer;
                case TypeCode.DateTime:
                    switch (((DateTime)integer).Kind)
                    {
                        case DateTimeKind.Utc: case DateTimeKind.Unspecified:
                            return ((DateTime)integer).Ticks;
                        case DateTimeKind.Local:
                            return ((DateTime)integer).ToUniversalTime().Ticks;
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

        #region ExactTry - the main implementation of EXACT with all the business code

        /// Some general overview on when and how the various exact conversions work:
        /// 
        /// from unsupported: never
        /// to integer/standard:
        ///     from string: only if type.TryParse works
        ///     from integer: only if in range
        ///     from fractional: never
        /// to integer/bool:
        ///     from string: "True"/"False" only, case-insensitive
        ///     from integer: only if 0 or 1
        ///     from fractional: never
        /// to integer/datetime:
        ///     from string: YYYY-MM-DD hh:mm:ss.ffff +ZZZZ, where various parts are optional
        ///     from integer: only if in range, using binary representation
        ///     from fractional: never
        /// to fractional:
        ///     from string: only if type.TryParse works
        ///     from integer: always (because all integer types are in range)
        ///     from fractional: always
        /// to string:
        ///     from string: no conversion
        ///     from integer/standard: .ToString()
        ///     from integer/bool: .ToString()
        ///     from integer/datetime: .ToString() using ISO format with various optional parts
        ///     from fractional: .ToString('R') ("roundtrip")

        #region To integer/standard

        #region Unsigned

        /// <summary>
        /// Converts the specified object to a byte using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out byte result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Byte) // fast track if it's already the right type
            {
                result = (byte)value;
                return true;
            }

            else if (code == TypeCode.String)
                return byte.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)byte.MaxValue)
                {
                    result = (byte)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)byte.MinValue && val <= (long)byte.MaxValue)
                {
                    result = (byte)val;
                    return true;
                }
            }

            result = default(byte);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a ushort using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out ushort result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt16) // fast track if it's already the right type
            {
                result = (ushort)value;
                return true;
            }

            else if (code == TypeCode.String)
                return ushort.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)ushort.MaxValue)
                {
                    result = (ushort)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)ushort.MinValue && val <= (long)ushort.MaxValue)
                {
                    result = (ushort)val;
                    return true;
                }
            }

            result = default(ushort);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a uint using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out uint result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt32) // fast track if it's already the right type
            {
                result = (uint)value;
                return true;
            }

            else if (code == TypeCode.String)
                return uint.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)uint.MaxValue)
                {
                    result = (uint)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)uint.MinValue && val <= (long)uint.MaxValue)
                {
                    result = (uint)val;
                    return true;
                }
            }

            result = default(uint);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a ulong using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out ulong result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.UInt64) // fast track if it's already the right type
            {
                result = (ulong)value;
                return true;
            }

            else if (code == TypeCode.String)
                return ulong.TryParse((string)value, out result);

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)ulong.MinValue) // note no upper limit here because ulong is special
                {
                    result = (ulong)val;
                    return true;
                }
            }

            result = default(ulong);
            return false;
        }

        #endregion

        #region Signed

        /// <summary>
        /// Converts the specified object to an sbyte using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out sbyte result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.SByte) // fast track if it's already the right type
            {
                result = (sbyte)value;
                return true;
            }

            else if (code == TypeCode.String)
                return sbyte.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)sbyte.MaxValue)
                {
                    result = (sbyte)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)sbyte.MinValue && val <= (long)sbyte.MaxValue)
                {
                    result = (sbyte)val;
                    return true;
                }
            }

            result = default(sbyte);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a short using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out short result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int16) // fast track if it's already the right type
            {
                result = (short)value;
                return true;
            }

            else if (code == TypeCode.String)
                return short.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)short.MaxValue)
                {
                    result = (short)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)short.MinValue && val <= (long)short.MaxValue)
                {
                    result = (short)val;
                    return true;
                }
            }

            result = default(short);
            return false;
        }

        /// <summary>
        /// Converts the specified object to an int using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out int result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int32) // fast track if it's already the right type
            {
                result = (int)value;
                return true;
            }

            else if (code == TypeCode.String)
                return int.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)int.MaxValue)
                {
                    result = (int)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)int.MinValue && val <= (long)int.MaxValue)
                {
                    result = (int)val;
                    return true;
                }
            }

            result = default(int);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a long using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out long result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Int64) // fast track if it's already the right type
            {
                result = (long)value;
                return true;
            }

            else if (code == TypeCode.String)
                return long.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)long.MaxValue)
                {
                    result = (long)val;
                    return true;
                }
            }

            else if (IsIntegerType[(long)code])
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
        /// Converts the specified object to a bool using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// 
        /// If the value is one of the integer types, the exact conversion only succeeds
        /// if the value is in range, i.e. 0 or 1. If converting from a string, the string
        /// must be exactly (case-insensitive) equal to "True" or "False", or the conversion
        /// will fail.
        /// </summary>
        public static bool ExactTry(object value, out bool result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Boolean) // fast track if it's already the right type
            {
                result = (bool)value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                string val = (string)value;
                result = false;
                if (string.Equals(val, "True", StringComparison.InvariantCultureIgnoreCase))
                    result = true; // result = true, return true
                else if (!string.Equals(val, "False", StringComparison.InvariantCultureIgnoreCase))
                    return false;  // result = default(bool), return false
                return true;       // result = false, return true
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                result = false;
                if (val == 1)
                    result = true;
                else if (val != 0)
                    return false;;
                return true;
            }

            else if (IsIntegerType[(int)code])
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
        /// Converts the specified object to a char using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out char result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Char) // fast track if it's already the right type
            {
                result = (char)value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                if (((string)value).Length == 1)
                {
                    result = ((string)value)[0];
                    return true;
                }
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)char.MaxValue)
                {
                    result = (char)val;
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
            {
                long val = UnboxIntegerToLong(value, code);
                if (val >= (long)char.MinValue && val <= (long)char.MaxValue)
                {
                    result = (char)val;
                    return true;
                }
            }

            result = default(char);
            return false;
        }

        #endregion

        #region To integer/datetime

        /// <summary>
        /// Converts the specified object to a DateTime using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// 
        /// When converting from string, supports a subset of the ISO 8601 formats - for
        /// more details see <see cref="DateTimeExtensions"/>.<see cref="TryParseIso"/>.
        /// </summary>
        public static bool ExactTry(object value, out DateTime result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.DateTime) // fast track if it's already the right type
            {
                result = (DateTime)value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                return DateTimeExtensions.TryParseIso((string)value, out result);
            }

            else if (code == TypeCode.UInt64)
            {
                ulong val = (ulong)value;
                if (val <= (ulong)DateTime.MaxValue.Ticks)
                {
                    result = new DateTime((long)val, DateTimeKind.Utc);
                    return true;
                }
            }

            else if (IsIntegerType[(int)code])
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
        /// Converts the specified object to a float using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out float result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Single) // fast track if it's already the right type
            {
                result = (float)value;
                return true;
            }
            else if (code == TypeCode.Double)
            {
                result = (float)(double)value;
                return true;
            }
            else if (code == TypeCode.Decimal)
            {
                result = (float)(decimal)value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                result = 0;
                if (string.Compare((string)value, "Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.PositiveInfinity;
                else if (string.Compare((string)value, "-Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.NegativeInfinity;
                else if (string.Compare((string)value, "NaN", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = float.NaN;

                if (result == 0)
                    return float.TryParse((string)value, out result);
                else
                    return true;
            }

            else if (code == TypeCode.UInt64)
            {
                result = (float)(ulong)value; // unbox as ulong, convert to float
                return true;
            }

            else if (IsIntegerType[(int)code])
            {
                result = (float)UnboxIntegerToLong(value, code); // unbox as long, convert to float
                return true;
            }

            result = default(float);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a double using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out double result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Double) // fast track if it's already the right type
            {
                result = (double)value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                result = (double)(float)value;
                return true;
            }
            else if (code == TypeCode.Decimal)
            {
                result = (double)(decimal)value;
                return true;
            }

            else if (code == TypeCode.String)
            {
                result = 0;
                if (string.Compare((string)value, "Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.PositiveInfinity;
                else if (string.Compare((string)value, "-Inf", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.NegativeInfinity;
                else if (string.Compare((string)value, "NaN", StringComparison.InvariantCultureIgnoreCase) == 0)
                    result = double.NaN;

                if (result == 0)
                    return double.TryParse((string)value, out result);
                else
                    return true;
            }

            else if (code == TypeCode.UInt64)
            {
                result = (double)(ulong)value; // unbox as ulong, convert to double
                return true;
            }

            else if (IsIntegerType[(int)code])
            {
                result = (double)UnboxIntegerToLong(value, code); // unbox as long, convert to double
                return true;
            }

            result = default(double);
            return false;
        }

        /// <summary>
        /// Converts the specified object to a decimal using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful.
        /// </summary>
        public static bool ExactTry(object value, out decimal result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.Decimal) // fast track if it's already the right type
            {
                result = (decimal)value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                float val = (float)value;
                if (val >= (float)decimal.MinValue && val <= (float)decimal.MaxValue)
                {
                    result = (decimal)val;
                    return true;
                }
            }
            else if (code == TypeCode.Double)
            {
                double val = (double)value;
                if (val >= (double)decimal.MinValue && val <= (double)decimal.MaxValue)
                {
                    result = (decimal)val;
                    return true;
                }
            }

            else if (code == TypeCode.String)
                return decimal.TryParse((string)value, out result);

            else if (code == TypeCode.UInt64)
            {
                result = (decimal)(ulong)value; // unbox as ulong, convert to decimal
                return true;
            }

            else if (IsIntegerType[(int)code])
            {
                result = (decimal)UnboxIntegerToLong(value, code); // unbox as long, convert to decimal
                return true;
            }

            result = default(decimal);
            return false;
        }

        #endregion

        #region To string

        /// <summary>
        /// Converts the specified object to a string using the Exact conversion.
        /// 
        /// Returns true if successful, or false if the object cannot be converted using
        /// the Exact conversion. The <see cref="result"/> is set to the type's default value
        /// if the conversion is unsuccessful, which in this case means null (!!!).
        /// 
        /// Note that the result will only ever be false if the value is one of the
        /// unsupported types - all supported types can be converted to a string.
        /// (So can the unsupported ones but it's a different matter. Unsupported types
        /// are not supported by this method for consistency with the other overloads.)
        /// </summary>
        public static bool ExactTry(object value, out string result)
        {
            TypeCode code = GetTypeCode(value);

            if (code == TypeCode.String) // fast track if it's already the right type
            {
                result = (string)value;
                return true;
            }
            else if (code == TypeCode.Single)
            {
                float val = (float)value;
                if (float.IsPositiveInfinity(val))
                    result = "Inf";
                else if (float.IsNegativeInfinity(val))
                    result = "-Inf";
                else if (float.IsNaN(val))
                    result = "NaN";
                else
                    result = val.ToString("R");
                return true;
            }
            else if (code == TypeCode.Double)
            {
                double val = (double)value;
                if (double.IsPositiveInfinity(val))
                    result = "Inf";
                else if (double.IsNegativeInfinity(val))
                    result = "-Inf";
                else if (double.IsNaN(val))
                    result = "NaN";
                else
                    result = val.ToString("R");
                return true;
            }
            else if (code == TypeCode.Boolean)
            {
                result = (bool)value ? "True" : "False";
                return true;
            }
            else if (code == TypeCode.DateTime)
            {
                result = ((DateTime)value).ToIsoStringOptimal();
                return true;
            }
            else if (!IsUnsupportedType[(int)code])
            {
                result = value.ToString();
                return true;
            }

            result = default(string); // which is null
            return false;
        }

        #endregion

        #endregion

        #region Exact - result is an "out" parameter; throw on failure

        /// <summary>
        /// Converts the specified object to a bool using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out bool result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(bool));
        }

        /// <summary>
        /// Converts the specified object to a byte using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out byte result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(byte));
        }

        /// <summary>
        /// Converts the specified object to an sbyte using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out sbyte result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(sbyte));
        }

        /// <summary>
        /// Converts the specified object to a short using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out short result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(short));
        }

        /// <summary>
        /// Converts the specified object to a ushort using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out ushort result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(ushort));
        }

        /// <summary>
        /// Converts the specified object to an int using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out int result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(int));
        }

        /// <summary>
        /// Converts the specified object to a uint using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out uint result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(uint));
        }

        /// <summary>
        /// Converts the specified object to a long using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out long result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(long));
        }

        /// <summary>
        /// Converts the specified object to a ulong using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out ulong result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(ulong));
        }

        /// <summary>
        /// Converts the specified object to a float using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out float result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(float));
        }

        /// <summary>
        /// Converts the specified object to a double using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out double result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(double));
        }

        /// <summary>
        /// Converts the specified object to a decimal using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out decimal result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(decimal));
        }

        /// <summary>
        /// Converts the specified object to a DateTime using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out DateTime result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(DateTime));
        }

        /// <summary>
        /// Converts the specified object to a char using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out char result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(char));
        }

        /// <summary>
        /// Converts the specified object to a string using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static void Exact(object value, out string result)
        {
            if (!ExactTry(value, out result))
                throw new RConvertException(value, typeof(string));
        }

        #endregion

        #region ExactToType - result is returned; throw on failure

        /// <summary>
        /// Converts the specified object to a bool using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static bool ExactToBool(object value)
        {
            bool result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a byte using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static byte ExactToByte(object value)
        {
            byte result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to an sbyte using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static sbyte ExactToSByte(object value)
        {
            sbyte result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a short using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static short ExactToShort(object value)
        {
            short result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a ushort using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static ushort ExactToUShort(object value)
        {
            ushort result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to an int using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static int ExactToInt(object value)
        {
            int result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a uint using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static uint ExactToUInt(object value)
        {
            uint result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a long using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static long ExactToLong(object value)
        {
            long result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a ulong using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static ulong ExactToULong(object value)
        {
            ulong result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a float using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static float ExactToFloat(object value)
        {
            float result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a double using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static double ExactToDouble(object value)
        {
            double result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a decimal using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static decimal ExactToDecimal(object value)
        {
            decimal result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a DateTime using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static DateTime ExactToDateTime(object value)
        {
            DateTime result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a char using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static char ExactToChar(object value)
        {
            char result;
            Exact(value, out result);
            return result;
        }

        /// <summary>
        /// Converts the specified object to a string using the Exact conversion.
        /// Throws an <see cref="RConvertException"/> if the Exact conversion fails.
        /// </summary>
        public static string ExactToString(object value)
        {
            string result;
            Exact(value, out result);
            return result;
        }

        #endregion

    }

    public class RConvertException : RTException
    {
        public RConvertException(object value, Type targetType)
        {
            TypeCode from = RConvert.GetTypeCode(value);
            TypeCode to = Type.GetTypeCode(targetType);
            _message = string.Format("Cannot do an exact conversion from value \"{2}\" of type \"{0}\" to type \"{1}\").", from, to, value);
        }
    }

}
