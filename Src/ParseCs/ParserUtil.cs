using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RT.Util;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.KitchenSink.ParseCs
{
    public static class ParserUtil
    {
        private enum Suffix { None, U, L, UL }
        public static object ParseNumericLiteral(string literal)
        {
            // F, D, M suffixes force a specific type (UL too, but we leave that for later because of hexadecimal)
            if (literal.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                return float.Parse(literal.Substring(0, literal.Length - 1));

            if (literal.EndsWith("d", StringComparison.OrdinalIgnoreCase))
                return double.Parse(literal.Substring(0, literal.Length - 1));

            if (literal.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                return decimal.Parse(literal.Substring(0, literal.Length - 1));

            // The other suffixes are more flexible, e.g. L can become long or ulong
            var suffix = Suffix.None;
            if (literal.EndsWith("u", StringComparison.OrdinalIgnoreCase))
            {
                suffix = Suffix.U;
                literal = literal.Substring(0, literal.Length - 1);
            }
            else if (literal.EndsWith("l", StringComparison.OrdinalIgnoreCase))
            {
                suffix = Suffix.L;
                literal = literal.Substring(0, literal.Length - 1);
            }
            else if (literal.EndsWith("ul", StringComparison.OrdinalIgnoreCase) || literal.EndsWith("lu", StringComparison.OrdinalIgnoreCase))
            {
                suffix = Suffix.UL;
                literal = literal.Substring(0, literal.Length - 2);
            }

            if (literal.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Hexadecimal
                var hex = literal.Substring(2).ToLowerInvariant();
                if (suffix == Suffix.None && (hex.Length < 8 || (hex.Length == 8 && hex[0] >= '0' && hex[0] <= '7')))
                    return int.Parse(hex, NumberStyles.AllowHexSpecifier);
                else if ((suffix == Suffix.None || suffix == Suffix.U) && hex.Length <= 8)
                    return uint.Parse(hex, NumberStyles.AllowHexSpecifier);
                else if ((suffix == Suffix.None || suffix == Suffix.L) && hex.Length < 16 || (hex.Length == 16 && hex[0] >= '0' && hex[0] <= '7'))
                    return long.Parse(hex, NumberStyles.AllowHexSpecifier);
                else if (hex.Length == 16)
                    return ulong.Parse(hex, NumberStyles.AllowHexSpecifier);
                throw new InvalidOperationException("Integer literal too large.");
            }
            else if (literal.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
            {
                // Hexadecimal
                var hex = literal.Substring(3).ToLowerInvariant();
                if (suffix == Suffix.None && (hex.Length < 8 || (hex.Length == 8 && hex[0] >= '0' && hex[0] <= '7')))
                    return -int.Parse(hex, NumberStyles.AllowHexSpecifier);
                else if ((suffix == Suffix.None || suffix == Suffix.L) && hex.Length < 16 || (hex.Length == 16 && hex[0] >= '0' && hex[0] <= '7'))
                    return -long.Parse(hex, NumberStyles.AllowHexSpecifier);
                throw new InvalidOperationException("Integer literal too large (or too small, since it’s negative).");
            }

            // Assume decimal
            if (literal.StartsWith("-"))
            {
                var deci = literal.Substring(1);
                if (suffix == Suffix.None && (deci.Length < 10 || (deci.Length == 10 && deci.CompareTo("2147483648") <= 0)))
                    return int.Parse(literal);
                else if ((suffix == Suffix.None || suffix == Suffix.L) && (deci.Length < 19 || (deci.Length == 19 && deci.CompareTo("9223372036854775808") <= 0)))
                    return long.Parse(literal);
                throw new InvalidOperationException("Integer literal too large (or too small, since it’s negative).");
            }
            else
            {
                if (suffix == Suffix.None && (literal.Length < 10 || (literal.Length == 10 && literal.CompareTo("2147483647") <= 0)))
                    return int.Parse(literal);
                else if ((suffix == Suffix.None || suffix == Suffix.U) && (literal.Length < 10 || (literal.Length == 10 && literal.CompareTo("4294967295") <= 0)))
                    return uint.Parse(literal);
                else if ((suffix == Suffix.None || suffix == Suffix.L) && (literal.Length < 19 || (literal.Length == 19 && literal.CompareTo("9223372036854775807") <= 0)))
                    return long.Parse(literal);
                else if ((literal.Length < 20 || (literal.Length == 20 && literal.CompareTo("18446744073709551615") <= 0)))
                    return ulong.Parse(literal);
                throw new InvalidOperationException("Integer literal too large.");
            }
        }

        public static bool IsGenericMethod(this MemberInfo info)
        {
            return info is MethodInfo && ((MethodInfo) info).IsGenericMethod;
        }

        public static int Better<T>(IEnumerable<T> types1, IEnumerable<T> types2, Func<T, T, int> comparer)
        {
            bool all1NotWorseThan2 = true;
            bool all2NotWorseThan1 = true;
            bool any1BetterThan2 = false;
            bool any2BetterThan1 = false;

            using (var e1 = types1.GetEnumerator())
            using (var e2 = types2.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    bool isNext = e2.MoveNext();
                    Ut.Assert(isNext);

                    var ms = comparer(e1.Current, e2.Current);
                    if (ms == -1)
                    {
                        all2NotWorseThan1 = false;
                        any1BetterThan2 = true;
                    }
                    else if (ms == 1)
                    {
                        all1NotWorseThan2 = false;
                        any2BetterThan1 = true;
                    }
                }
            }

            if (all1NotWorseThan2 && any1BetterThan2)
                return -1;
            else if (all2NotWorseThan1 && any2BetterThan1)
                return 1;
            else
                return 0;
        }

        public static int MoreSpecific(Type type1, Type type2)
        {
            if (type1.IsGenericParameter != type2.IsGenericParameter)
                return type1.IsGenericParameter ? 1 : -1;
            if (type1.IsGenericType && type2.IsGenericType && type1.GetGenericArguments().Length == type2.GetGenericArguments().Length)
                return Better(type1.GetGenericArguments(), type2.GetGenericArguments(), MoreSpecific);
            if (type1.IsArray && type2.IsArray && type1.GetArrayRank() == type2.GetArrayRank())
                return MoreSpecific(type1.GetElementType(), type2.GetElementType());
            return 0;
        }

        public static Type GetDelegateType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Expression<>))
                type = type.GetGenericArguments()[0];

            if (!typeof(Delegate).IsAssignableFrom(type))
                return null;

            return (type != typeof(Delegate) && type != typeof(MulticastDelegate) && !type.IsGenericParameter) ? type : null;
        }

        public static Type GetDelegateReturnType(Type type)
        {
            var delegateType = GetDelegateType(type);
            if (delegateType == null)
                return null;
            var invoke = delegateType.GetMethod("Invoke");
            Ut.Assert(invoke != null);
            return invoke.ReturnType;
        }

        public static IEnumerable<Type> GetDelegateParameterTypes(Type type)
        {
            var delegateType = GetDelegateType(type);
            var invoke = delegateType.GetMethod("Invoke");
            Ut.Assert(invoke != null);
            return invoke.GetParameters().Select(p => p.ParameterType);
        }

        public static IEnumerable<CandidateInfo<T>> EvaluateArgumentList<T>(T member, ParameterInfo[] parameterInfo, IEnumerable<ArgumentInfo> args, NameResolver resolver) where T : MemberInfo
        {
            var i = 0;
            var seenNamedArgument = false;
            var inParamsArray = false;
            var evaluatedParameterTypes = new AutoList<Type>();
            var evaluatedArgumentIndex = new AutoList<int>();
            var evaluatedArguments = new AutoList<ResolveContext>();
            var modes = new AutoList<ArgumentMode>(parameterInfo.Select(pi => pi.IsOut ? ArgumentMode.Out : pi.ParameterType.IsByRef ? ArgumentMode.Ref : ArgumentMode.In));
            var paramsArray = new List<ResolveContext>();
            var paramsArrayIndex = parameterInfo.IndexOf(p => p.IsDefined<ParamArrayAttribute>());

            foreach (var argument in args)
            {
                if (argument.Name != null)
                {
                    // Named argument
                    seenNamedArgument = true;
                    var index = parameterInfo.IndexOf(pi => pi.Name == argument.Name);
                    if (index == -1)
                        // There is no parameter by this name ⇒ resolution fails
                        yield break;
                    if (evaluatedArguments[index] != null)
                        throw new InvalidOperationException("The named parameter “{0}” has been specified multiple times.".Fmt(argument.Name));
                    if (argument.Mode != modes[index])
                        // in/out/ref doesn’t match parameter ⇒ resolution fails
                        yield break;
                    evaluatedArguments[index] = argument.Argument;
                    evaluatedParameterTypes[index] = parameterInfo[index].ParameterType;
                    evaluatedArgumentIndex[index] = i;
                }
                else
                {
                    // Positional argument
                    if (seenNamedArgument)
                        throw new InvalidOperationException("Cannot use a positional argument after a named argument.");

                    if (i >= parameterInfo.Length && !inParamsArray)
                        yield break;    // too many positional arguments: candidate is inapplicable

                    if (paramsArrayIndex != -1 && i >= paramsArrayIndex)
                    {
                        if (argument.Mode != ArgumentMode.In)
                            // Can’t use out/ref in a params array
                            yield break;
                        paramsArray.Add(argument.Argument);
                    }
                    else
                    {
                        if (argument.Mode != modes[i])
                            // in/out/ref doesn’t match parameter ⇒ resolution fails
                            yield break;
                        evaluatedArguments[i] = argument.Argument;
                        evaluatedParameterTypes[i] = parameterInfo[i].ParameterType;
                        evaluatedArgumentIndex[i] = i;
                    }
                }

                i++;
            }

            // Fill in the default values for the unspecified optional parameters
            for (int j = 0; j < parameterInfo.Length; j++)
            {
                if (j != paramsArrayIndex && evaluatedArguments[j] == null)
                {
                    if ((parameterInfo[j].Attributes & ParameterAttributes.HasDefault) != 0)
                    {
                        evaluatedArguments[j] = new ResolveContextExpression(Expression.Constant(parameterInfo[j].DefaultValue, parameterInfo[j].ParameterType), wasAnonymousFunction: false);
                        evaluatedParameterTypes[j] = parameterInfo[j].ParameterType;
                        evaluatedArgumentIndex[j] = -1;
                    }
                    else
                        yield break;    // a non-optional parameter has no argument specified ⇒ not a candidate
                }
            }

            Func<int, CandidateParameterInfo[]> getParameters = count =>
                Enumerable.Range(0, count).Select(p => new CandidateParameterInfo
                {
                    ParameterType = evaluatedParameterTypes[p],
                    Mode = modes[p],
                    Argument = evaluatedArguments[p],
                    ArgumentIndex = evaluatedArgumentIndex[p],
                    UninstantiatedParameterType = evaluatedParameterTypes[p],
                }).ToArray();

            if (paramsArrayIndex == -1 || paramsArray.Count == 1)
            {
                if (paramsArrayIndex != -1)
                {
                    // Emit the normal form
                    evaluatedArguments[paramsArrayIndex] = paramsArray[0];
                    evaluatedParameterTypes[paramsArrayIndex] = parameterInfo[paramsArrayIndex].ParameterType;
                }
                yield return new CandidateInfo<T>(member, getParameters(evaluatedParameterTypes.Count), parameterInfo.Length, isLiftedOperator: false, isExpandedForm: false);
            }

            if (paramsArrayIndex != -1)
            {
                // Emit the expanded form
                var arrayElementType = parameterInfo[paramsArrayIndex].ParameterType.GetElementType();
                for (int k = 0; k < paramsArray.Count; k++)
                {
                    evaluatedArguments[paramsArrayIndex + k] = paramsArray[k];
                    evaluatedParameterTypes[paramsArrayIndex + k] = arrayElementType;
                    evaluatedArgumentIndex[paramsArrayIndex + k] = paramsArrayIndex + k;
                    modes[paramsArrayIndex + k] = ArgumentMode.In;
                }
                // If paramsArray.Count == 0, then the expanded form is *shorter* than the normal form
                yield return new CandidateInfo<T>(member, getParameters(paramsArrayIndex + paramsArray.Count), parameterInfo.Length, isLiftedOperator: false, isExpandedForm: true);
            }
        }

        public static CandidateInfo<T> ResolveOverloads<T>(List<Tuple<T, ParameterInfo[]>> overloads, IEnumerable<ArgumentInfo> arguments, NameResolver resolver) where T : MemberInfo
        {
            var candidates = overloads.SelectMany(ov => ParserUtil.EvaluateArgumentList(ov.Item1, ov.Item2, arguments, resolver)).ToList();

            // Type inference
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Member is MethodInfo && ((MethodInfo) (MemberInfo) candidates[i].Member).IsGenericMethodDefinition)
                    candidates[i] = TypeInferer.TypeInference(candidates[i], resolver);

            // Remove nulls (entries where type inference failed) and entries that are not applicable (§7.5.3.1 Applicable function member)
            candidates = candidates.Where(c => c != null && c.Parameters.All(p => p.Mode == ArgumentMode.In
                ? Conversion.Implicit(p.Argument, p.ParameterType) != null
                : p.ParameterType == p.Argument.ExpressionType)).ToList();

            if (candidates.Count == 0)
                return null;
            if (candidates.Count == 1)
                return candidates[0];

            // We have more than one candidate, so need to find the “best” one 
            bool[] cannot = new bool[candidates.Count];
            for (int i = 0; i < cannot.Length; i++)
            {
                for (int j = i + 1; j < cannot.Length; j++)
                {
                    int compare = candidates[i].Better(candidates[j]);
                    if (compare != 1) // j is not better
                        cannot[j] = true;
                    if (compare != -1) // i is not better
                        cannot[i] = true;
                }
            }

            CandidateInfo<T> candidate = null;
            for (int i = 0; i < cannot.Length; i++)
            {
                if (!cannot[i])
                {
                    if (candidate == null)
                        candidate = candidates[i];
                    else
                        // There is more than one applicable candidate — method call is ambiguous
                        return null;
                }
            }

            // Either candidate == null, in which case no candidate was better than all others, or this is the successful candidate
            return candidate;
        }
    }
}
