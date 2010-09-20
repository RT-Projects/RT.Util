using System;
using System.Collections.Generic;
using System.Linq.Expressions;


#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.KitchenSink.ParseCs
{
    public abstract class Conversion
    {
        public bool IsImplicit;
        public Type From, To;

        public Conversion(bool isImplicit) { IsImplicit = isImplicit; }

        private static readonly Dictionary<Type, List<Type>> implicitNumericConversions = new Dictionary<Type, List<Type>>
        {
            { typeof(sbyte), new List<Type> { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(byte), new List<Type> { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(short), new List<Type> { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(ushort), new List<Type> { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(int), new List<Type> { typeof(long), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(uint), new List<Type> { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(long), new List<Type> { typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(ulong), new List<Type> { typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(char), new List<Type> { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } }, 
            { typeof(float), new List<Type> { typeof(double) } }, 
        };

        public static Conversion Implicit(Type from, Type to)
        {
            // 6.1.1 Identity conversion
            if (from == to)
                return new IdentityConversion(true);

            // 6.1.2 Implicit numeric conversions
            if (implicitNumericConversions.ContainsKey(from) && implicitNumericConversions[from].Contains(to))
                return new NumericConversion(true);

            // 6.1.4 Implicit nullable conversions
            var fromNullable = from.IsGenericType && from.GetGenericTypeDefinition() == typeof(Nullable<>);
            var toNullable = to.IsGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (toNullable)
            {
                var underlying = Conversion.Implicit(fromNullable ? from.GetGenericArguments()[0] : from, to.GetGenericArguments()[0]);
                return underlying == null ? null : new NullableConversion(true, underlying);
            }
            if (fromNullable && !to.IsValueType)
            {
                var underlying = Conversion.Implicit(from.GetGenericArguments()[0], to);
                return underlying == null ? null : new NullableConversion(true, underlying);
            }

            // 6.1.6 Implicit reference conversions + 6.1.7 Boxing conversions
            if (to.IsAssignableFrom(from))
                return from.IsValueType ? (Conversion) new BoxingConversion(true) : new ReferenceConversion(true);
            if (fromNullable && to.IsAssignableFrom(from.GetGenericArguments()[0]))
                return new BoxingConversion(true);

            // 6.1.10 Implicit conversions involving type parameters
            if (from.IsGenericParameter)
                throw new NotImplementedException();

            return null;
        }

        public static Conversion Implicit(ResolveContext from, Type to)
        {
            var fromExpr = from as ResolveContextExpression;

            // 6.1.3 Implicit enumeration conversions
            if (to.IsEnum)
            {
                if (fromExpr != null)
                {
                    var constExpr = fromExpr.Expression as ConstantExpression;
                    if (constExpr != null && constExpr.Value is int && ((int) constExpr.Value) == 0)
                        return new EnumConversion(true);
                }
            }

            // 6.1.9 Implicit constant expression conversions
            if (fromExpr != null)
            {
                var constExpr = fromExpr.Expression as ConstantExpression;
                if (constExpr != null && constExpr.Value is int)
                {
                    var integer = (int) constExpr.Value;
                    if (to == typeof(sbyte) && integer >= sbyte.MinValue && integer <= sbyte.MaxValue)
                        return new ConstantExpressionConversion(true);
                    if (to == typeof(byte) && integer >= byte.MinValue && integer <= byte.MaxValue)
                        return new ConstantExpressionConversion(true);
                    if (to == typeof(short) && integer >= short.MinValue && integer <= short.MaxValue)
                        return new ConstantExpressionConversion(true);
                    if (to == typeof(ushort) && integer >= ushort.MinValue && integer <= ushort.MaxValue)
                        return new ConstantExpressionConversion(true);
                    if (to == typeof(uint) && integer >= 0)
                        return new ConstantExpressionConversion(true);
                    if (to == typeof(ulong) && integer >= 0)
                        return new ConstantExpressionConversion(true);
                }
                else if (constExpr != null && constExpr.Value is long && to == typeof(ulong))
                {
                    var integer = (long) constExpr.Value;
                    if (integer >= 0)
                        return new ConstantExpressionConversion(true);
                }
            }

            // 6.1.5 Null literal conversions
            if (to.IsGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>) && from is ResolveContextNullLiteral)
                return new NullLiteralConversion(true);
            if (!to.IsValueType && from is ResolveContextNullLiteral)
                return new NullLiteralConversion(true);

            if (from is ResolveContextLambda)
                return null;

            return Implicit(from.ExpressionType, to);
        }
    }

    public class NullableConversion : Conversion
    {
        public Conversion UnderlyingConversion;
        public NullableConversion(bool isImplicit, Conversion underlying)
            : base(isImplicit)
        {
            UnderlyingConversion = underlying;
        }
    }

    public class IdentityConversion : Conversion { public IdentityConversion(bool isImplicit) : base(isImplicit) { } }
    public class NumericConversion : Conversion { public NumericConversion(bool isImplicit) : base(isImplicit) { } }
    public class EnumConversion : Conversion { public EnumConversion(bool isImplicit) : base(isImplicit) { } }
    public class NullLiteralConversion : Conversion { public NullLiteralConversion(bool isImplicit) : base(isImplicit) { } }
    public class ReferenceConversion : Conversion { public ReferenceConversion(bool isImplicit) : base(isImplicit) { } }
    public class BoxingConversion : Conversion { public BoxingConversion(bool isImplicit) : base(isImplicit) { } }
    public class ConstantExpressionConversion : Conversion { public ConstantExpressionConversion(bool isImplicit) : base(isImplicit) { } }
}
