using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.ParseCs
{
    class CandidateParameterInfo
    {
        public Type ParameterType;
        public Type UninstantiatedParameterType;
        public ResolveContext Argument;
        public int ArgumentIndex;       // -1 if it’s an unspecified optional parameter
        public ArgumentMode Mode;
    }
    class CandidateInfo<T> where T : MemberInfo
    {
        public T Member;
        public CandidateParameterInfo[] Parameters;
        public int OriginalNumberOfParameters;
        public bool IsLiftedOperator;
        public bool IsExpandedForm;
        public CandidateInfo(T member, CandidateParameterInfo[] parameters, int originalNumberOfParameters, bool isLiftedOperator, bool isExpandedForm)
        {
            Member = member;
            Parameters = parameters;
            OriginalNumberOfParameters = originalNumberOfParameters;
            IsLiftedOperator = isLiftedOperator;
            IsExpandedForm = isExpandedForm;
        }

        public IEnumerable<CandidateParameterInfo> ParametersInSpecifiedOrder()
        {
            if (Parameters.Length == 0)
                yield break;
            var max = Parameters.Max(p => p.ArgumentIndex);
            if (max == -1)
                yield break;
            for (int i = 0; i <= max; i++)
                yield return Parameters.First(p => p.ArgumentIndex == i);
        }

        public bool EquivalentSpecifiedParameterTypes(CandidateInfo<T> other)
        {
            using (var e1 = ParametersInSpecifiedOrder().GetEnumerator())
            using (var e2 = other.ParametersInSpecifiedOrder().GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    // We do not expect to reach the end of the ‘other’ array prematurely
                    if (!e2.MoveNext())
                        return false;
                    if (e1.Current.ParameterType != e2.Current.ParameterType)
                        return false;
                }
                // We do expect to reach the end of the ‘other’ array now
                return !e2.MoveNext();
            }
        }

        public int Better(CandidateInfo<T> other)
        {
            // §7.5.3.2 Better function member
            // First check whether all the applicable parameter types are equivalent, in which case the tie-breaking rules apply
            if (EquivalentSpecifiedParameterTypes(other))
            {
                // • If MP is a non-generic method and MQ is a generic method, then MP is better than MQ.
                var mgen = Member.IsGenericMethod();
                if (mgen != other.Member.IsGenericMethod())
                    return mgen ? 1 : -1;

                // • If MP is applicable in its normal form and MQ has a params array and is applicable only in its expanded form, then MP is better than MQ.
                if (IsExpandedForm != other.IsExpandedForm)
                    return IsExpandedForm ? 1 : -1;

                // • If MP has more declared parameters than MQ, then MP is better than MQ. This can occur if both methods have params arrays and are applicable only in their expanded forms.
                if (OriginalNumberOfParameters != other.OriginalNumberOfParameters)
                    return OriginalNumberOfParameters < other.OriginalNumberOfParameters ? 1 : -1;

                // • If all parameters of MP have a corresponding argument whereas default arguments need to be substituted for at least one optional parameter in MQ then MP is better than MQ. 
                var any1 = Parameters.Any(p => p.ArgumentIndex == -1);
                var any2 = other.Parameters.Any(p => p.ArgumentIndex == -1);
                if (any1 != any2)
                    return any1 ? 1 : -1;

                // • If MP has more specific parameter types than MQ, then MP is better than MQ.
                int isMoreSpecific = ParserUtil.Better(Parameters.Select(p => p.UninstantiatedParameterType), other.Parameters.Select(p => p.UninstantiatedParameterType), ParserUtil.MoreSpecific);
                if (isMoreSpecific != 0)
                    return isMoreSpecific;

                // • If one member is a non-lifted operator and the other is a lifted operator, the non-lifted one is better.
                if (IsLiftedOperator != other.IsLiftedOperator)
                    return IsLiftedOperator ? 1 : -1;

                // • Otherwise, neither function member is better.
                return 0;
            }

            return ParserUtil.Better(ParametersInSpecifiedOrder(), other.ParametersInSpecifiedOrder(), (p1, p2) =>
            {
                var c1 = Conversion.Implicit(p1.Argument, p1.ParameterType);
                var c2 = Conversion.Implicit(p2.Argument, p2.ParameterType);

                // Given an implicit conversion C1 that converts from an expression E to a type T1, and an implicit conversion C2 that
                // converts from an expression E to a type T2, C1 is a better conversion than C2 if at least one of the following holds:
                // • E has a type S and an identity conversion exists from S to T1 but not from S to T2
                if (c1 is IdentityConversion != c2 is IdentityConversion)
                    return c1 is IdentityConversion ? -1 : 1;

                // • E is not an anonymous function and T1 is a better conversion target than T2 (§7.5.3.5)
                var isAnonymousFunction = p1.Argument is ResolveContextExpression && ((ResolveContextExpression) p1.Argument).WasAnonymousFunction;
                if (!isAnonymousFunction)
                    return betterConversionTarget(p1.ParameterType, p2.ParameterType);

                // • E is an anonymous function, T1 is either a delegate type D1 or an expression tree type Expression<D1>, T2 is either a delegate type D2 or an expression tree type Expression<D2> and one of the following holds:
                //     o D1 is a better conversion target than D2
                //     o D1 and D2 have identical parameter lists, and one of the following holds:
                //          • D1 has a return type Y1, and D2 has a return type Y2, an inferred return type X exists for E in the context of that parameter list (§7.5.2.12), and the conversion from X to Y1 is better than the conversion from X to Y2
                //          • D1 has a return type Y, and D2 is void returning
                else
                {
                    var del1 = ParserUtil.GetDelegateType(p1.ParameterType);
                    var del2 = ParserUtil.GetDelegateType(p2.ParameterType);
                    if (del1 == null || del2 == null)
                        throw new InternalErrorException("Internal error 23087");
                    var del1param = ParserUtil.GetDelegateParameterTypes(p1.ParameterType);
                    var del2param = ParserUtil.GetDelegateParameterTypes(p2.ParameterType);
                    if (del1param.SequenceEqual(del2param))
                    {
                        var ret1 = ParserUtil.GetDelegateReturnType(p1.ParameterType);
                        var ret2 = ParserUtil.GetDelegateReturnType(p2.ParameterType);
                        if ((ret1 == typeof(void)) != (ret2 == typeof(void)))
                            return (ret1 == typeof(void)) ? 1 : -1;
                        var realRet = ParserUtil.GetDelegateReturnType(((ResolveContextExpression) p1.Argument).ExpressionType);
                        var rc1 = Conversion.Implicit(realRet, ret1);
                        var rc2 = Conversion.Implicit(realRet, ret2);
                        if (rc1 is IdentityConversion != rc2 is IdentityConversion)
                            return rc1 is IdentityConversion ? -1 : 1;
                        return betterConversionTarget(ret1, ret2);
                    }
                    return betterConversionTarget(del1, del2);
                }
            });
        }

        private int betterConversionTarget(Type t1, Type t2)
        {
            // 7.5.3.5 Better conversion target
            // Given two different types T1 and T2, T1 is a better conversion target than T2 if at least one of the following holds:
            // • An implicit conversion from T1 to T2 exists, and no implicit conversion from T2 to T1 exists
            var conv1 = Conversion.Implicit(t1, t2);
            var conv2 = Conversion.Implicit(t2, t1);
            if ((conv1 == null) != (conv2 == null))
                return (conv1 == null) ? 1 : -1;

            // • T1 is a signed integral type and T2 is an unsigned integral type. Specifically:
            //     o T1 is sbyte and T2 is byte, ushort, uint, or ulong
            //     o T1 is short and T2 is ushort, uint, or ulong
            //     o T1 is int and T2 is uint, or ulong
            //     o T1 is long and T2 is ulong
            if ((t1 == typeof(sbyte) && (t2 == typeof(byte) || t2 == typeof(ushort) || t2 == typeof(uint) || t2 == typeof(ulong))) ||
                (t1 == typeof(short) && (t2 == typeof(ushort) || t2 == typeof(uint) || t2 == typeof(ulong))) ||
                (t1 == typeof(int) && (t2 == typeof(uint) || t2 == typeof(ulong))) ||
                (t1 == typeof(long) && t2 == typeof(ulong)))
                return -1;
            if ((t2 == typeof(sbyte) && (t1 == typeof(byte) || t1 == typeof(ushort) || t1 == typeof(uint) || t1 == typeof(ulong))) ||
                (t2 == typeof(short) && (t1 == typeof(ushort) || t1 == typeof(uint) || t1 == typeof(ulong))) ||
                (t2 == typeof(int) && (t1 == typeof(uint) || t1 == typeof(ulong))) ||
                (t2 == typeof(long) && t1 == typeof(ulong)))
                return 1;

            return 0;
        }
    }

    class ArgumentInfo
    {
        public string Name;     // null for positional arguments
        public ResolveContext Argument;
        public ArgumentMode Mode;       // in/out/ref
        public ArgumentInfo(string name, ResolveContext argument, ArgumentMode mode)
        {
            Name = name;
            Argument = argument;
            Mode = mode;
        }
    }
}
