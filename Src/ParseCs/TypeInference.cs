using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using RT.Util;
using RT.Util.ExtensionMethods;


#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.KitchenSink.ParseCs
{
    class TypeInferer
    {
        public static CandidateInfo<T> TypeInference<T>(CandidateInfo<T> candidateInfo, NameResolver resolver) where T : MemberInfo
        {
            var method = candidateInfo.Member as MethodInfo;
            if (method == null || !method.IsGenericMethodDefinition)
                throw new InternalErrorException("Type inference can only be performed on a generic method definition.");

            var inferer = new TypeInferer(candidateInfo.Parameters, method, resolver);
            if (!inferer.infer())
                return null;

            var inferredMethod = method.MakeGenericMethod(inferer._inferred);
            // Return the list of parameter types with the generic type parameters substituted
            var parameterTypes = inferredMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            // The inferer may have changed an implicit lambda to a LINQ expression, so return the new version of _arguments
            return new CandidateInfo<T>((T) (MemberInfo) inferredMethod,
                candidateInfo.Parameters.Select((p, i) => new CandidateParameterInfo
                {
                    ParameterType = parameterTypes[i],
                    Mode = p.Mode,
                    Argument = inferer._arguments[i],
                    ArgumentIndex = p.ArgumentIndex,
                    UninstantiatedParameterType = p.ParameterType
                }).ToArray(), inferredMethod.GetParameters().Length, isLiftedOperator: false, isExpandedForm: candidateInfo.IsExpandedForm);
        }

        private enum boundKind { Exact, Lower, Upper }
        private class boundInfo
        {
            public boundKind Kind;
            public Type Type;
            public override string ToString() { return "{0} ({1})".Fmt(Type.FullName, Kind); }
        }

        private CandidateParameterInfo[] _parameters;
        private ResolveContext[] _arguments;
        private MethodInfo _method;
        private Type[] _inferred;
        private Type[] _genericParameters;
        private bool[] _fixed;
        private List<boundInfo>[] _bounds;
        private NameResolver _resolver;

        private TypeInferer(CandidateParameterInfo[] parameters, MethodInfo method, NameResolver resolver)
        {
            Ut.Assert(method.IsGenericMethodDefinition);

            _parameters = parameters;
            _method = method;

            // take a copy of this because it may be modified to change implicitly-typed lambdas into “translated” LINQ expressions with the right explicit types
            _arguments = parameters.Select(p => p.Argument).ToArray();

            _genericParameters = method.GetGenericArguments();
            _fixed = new bool[_genericParameters.Length];
            _inferred = new Type[_genericParameters.Length];
            _bounds = new List<boundInfo>[_genericParameters.Length];
            _resolver = resolver;
        }

        private void addBound(int index, Type typeToAdd, boundKind kind)
        {
            if (_bounds[index] == null)
                _bounds[index] = new List<boundInfo>();
            _bounds[index].Add(new boundInfo { Type = typeToAdd, Kind = kind });
        }

        private bool infer()
        {
            // First of all, make sure that all the lambda expressions have the right number of parameters,
            // and that we don’t have lambdas where there is no delegate or expression-tree type
            for (int i = 0; i < _arguments.Length; i++)
            {
                var lambda = _arguments[i] as ResolveContextLambda;
                if (lambda == null)
                    continue;
                var dlgParam = ParserUtil.GetDelegateParameterTypes(_parameters[i].ParameterType);
                if (dlgParam == null || dlgParam.Count() != lambda.Lambda.Parameters.Count)
                    return false;
            }

            // §7.5.2.1 The first phase
            for (int p = 0; p < _arguments.Length; p++)
            {
                // If Ei is an anonymous function, an explicit parameter type inference (§7.5.2.7) is made from Ei to Ti.
                // This applies only if the anonymous function is explicitly-typed, in which case it will already have been resolved to an expression tree
                var expr = _arguments[p] as ResolveContextExpression;
                if (expr != null && expr.Expression is LambdaExpression)
                {
                    if (!explicitParameterTypeInference((LambdaExpression) expr.Expression, _parameters[p].ParameterType))
                        return false;
                }
                else if (_arguments[p] is ResolveContextLambda)
                {
                    // Ignore implicitly-typed lambdas for now
                }
                // Otherwise, if Ei has a type U and xi is a value parameter then a lower-bound inference is made from U to Ti.
                else if (_parameters[p].Mode == ArgumentMode.In)
                {
                    if (!lowerBoundInference(_arguments[p].ExpressionType, _parameters[p].ParameterType))
                        return false;
                }
                else
                {
                    if (!exactInference(_arguments[p].ExpressionType, _parameters[p].ParameterType))
                        return false;
                }
            }

            // §7.5.2.2 The second phase
            while (true)
            {
                // If no unfixed type parameters exist then type inference succeeds.
                if (_fixed.All(b => b))
                {
                    // Change all the implicitly-typed lambda expressions to explicit ones
                    for (int p = 0; p < _parameters.Length; p++)
                    {
                        var lambda = _arguments[p] as ResolveContextLambda;
                        if (lambda == null)
                            continue;
                        var dlgParameterTypes = ParserUtil.GetDelegateParameterTypes(_parameters[p].ParameterType);
                        var linqExpr = lambda.Lambda.ToLinqExpression(_resolver, dlgParameterTypes.Select(dlgp => substituteFixed(dlgp)).ToArray(), false);
                        _arguments[p] = new ResolveContextExpression(linqExpr, wasAnonymousFunction: true);
                    }
                    return true;
                }

                // If there exists one or more arguments Ei with corresponding parameter type Ti such that the output type of Ei with type Ti
                // contains at least one unfixed type parameter Xj, and none of the input types of Ei with type Ti contains any unfixed type
                // parameter Xj, then an output type inference is made from all such Ei to Ti.
                // (Plain English: infer the return types of all the lambda expressions where all the input types to the lambda are fixed.)
                // (While we’re at it, we will also collection information about which parameter types are still dependent on each other.)
                var isDependent = new bool[_genericParameters.Length];
                for (int p = 0; p < _parameters.Length; p++)
                {
                    // Is it a lambda?
                    var arg = _arguments[p] as ResolveContextLambda;
                    if (arg != null)
                    {
                        var dlgParameterTypes = ParserUtil.GetDelegateParameterTypes(_parameters[p].ParameterType);
                        if (dlgParameterTypes == null)
                            throw new InvalidOperationException("Cannot apply lambda expression argument “{0}” to a parameter that is not of a delegate or expression-tree type.".Fmt(arg.Lambda.ToString()));
                        var dlgReturnType = ParserUtil.GetDelegateReturnType(_parameters[p].ParameterType);
                        var occurInReturnType = getContainedIn(dlgReturnType).ToArray();

                        // Any unfixed parameters ⇒ not interested
                        if (dlgParameterTypes.SelectMany(t => getContainedIn(t)).Any(i => !_fixed[i]))
                        {
                            // An unfixed type parameter occurs in the delegate parameter types, so the
                            // type parameters that occur in the delegate return type depend on it
                            foreach (var inRet in occurInReturnType)
                                isDependent[inRet] = true;
                            continue;
                        }

                        // Generate the lambda expression so it becomes explicitly typed and determine its return type
                        var linqExpr = arg.Lambda.ToLinqExpression(_resolver, dlgParameterTypes.Select(dlgp => substituteFixed(dlgp)).ToArray(), false);
                        var argReturnType = ((LambdaExpression) linqExpr).ReturnType;
                        _arguments[p] = new ResolveContextExpression(linqExpr, wasAnonymousFunction: true);
                        if (!lowerBoundInference(argReturnType, dlgReturnType))
                            return false;
                    }
                    else
                    {
                        // Is it a method group?
                        var mg = _arguments[p] as ResolveContextMethodGroup;
                        if (mg != null)
                            throw new NotImplementedException();
                    }
                }

                // Whether or not the previous step actually made an inference, we must now fix at least one type parameter, as follows:

                // If there exists one or more type parameters Xi such that Xi is unfixed, and Xi has a non-empty set of bounds, and Xi
                // does not depend on any Xj then each such Xi is fixed. If any fixing operation fails then type inference fails.
                bool any = false;
                for (int i = 0; i < _genericParameters.Length; i++)
                {
                    if (!_fixed[i] && _bounds[i] != null && !isDependent[i])
                    {
                        if (!fix(i))
                            return false;
                        any = true;
                    }
                }

                if (!any)
                {
                    // Otherwise, if there exists one or more type parameters Xi such that Xi is unfixed, and Xi has a non-empty set of bounds,
                    // and there is at least one type parameter Xj that depends on Xi then each such Xi is fixed. If any fixing operation fails
                    // then type inference fails.
                    for (int i = 0; i < _genericParameters.Length; i++)
                    {
                        if (!_fixed[i] && _bounds[i] != null)
                        {
                            if (!fix(i))
                                return false;
                        }
                        any = true;
                    }
                }

                if (!any)
                {
                    // Otherwise, we are unable to make progress and there are unfixed parameters. Type inference fails.
                    return false;
                }
            }
        }

        private Type substituteFixed(Type type)
        {
            var idx = Array.IndexOf(_genericParameters, type);
            if (idx >= 0)
            {
                if (!_fixed[idx])
                    throw new InternalErrorException("Attempt to substitute a type parameter that is not fixed");
                return _inferred[idx];
            }

            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(t => substituteFixed(t)).ToArray());

            return type;
        }

        private bool fixIndependentTypeArguments(out bool any)
        {
            // •	All unfixed type variables Xi which do not depend on (§7.5.2.5) any Xj are fixed (§7.5.2.10).
            any = false;
            var needToFix = new bool[_genericParameters.Length];
            for (int i = 0; i < needToFix.Length; i++)
                needToFix[i] = !_fixed[i];

            for (int p = 0; p < _parameters.Length; p++)
                foreach (var idx in getContainedIn(ParserUtil.GetDelegateReturnType(_parameters[p].ParameterType)))
                    needToFix[idx] = false;

            for (int i = 0; i < needToFix.Length; i++)
            {
                if (!needToFix[i])
                    continue;
                if (!fix(i))
                    return false;
                any = true;
            }

            return true;
        }

        private bool fixDependentTypeArguments(out bool any)
        {
            // •	[...] all unfixed type variables Xi are fixed for which all of the following hold:
            //     a) There is at least one type variable Xj that depends on Xi
            //     b) Xi has a non-empty set of bounds

            any = false;
            for (int i = 0; i < _fixed.Length; i++)
            {
                if (!_fixed[i] && _bounds[i] != null)
                {
                    if (!fix(i))
                        return false;
                    any = true;
                }
            }
            return true;
        }

        private bool fix(int index)
        {
            // The set of candidate types Uj starts out as the set of all types in the set of bounds for Xi.
            if (_bounds[index] == null)
                throw new InternalErrorException("Bounds shouldn’t be null");
            if (_bounds[index].Count == 1)
            {
                _inferred[index] = _bounds[index][0].Type;
                _fixed[index] = true;
                return true;
            }

            var bounds = _bounds[index].Select(b => b.Type).Distinct().ToList();   // sift out duplicates, then create a copy of the list

            // Examine the three types of bounds in turn
            foreach (var bound in _bounds[index])
                for (int i = bounds.Count - 1; i >= 0; i--)
                    if ((bound.Kind == boundKind.Exact && bounds[i] != bound.Type) ||
                        (bound.Kind == boundKind.Lower && !bounds[i].IsAssignableFrom(bound.Type)) ||
                        (bound.Kind == boundKind.Upper && !bound.Type.IsAssignableFrom(bounds[i])))
                        bounds.RemoveAt(i);

            Type candidate = null;
            for (int i = 0; i < bounds.Count; i++)
            {
                bool good = true;
                for (int j = i + 1; good && j < bounds.Count; j++)
                    if (Conversion.Implicit(bounds[i], bounds[j]) == null)
                        good = false;

                if (good)
                {
                    if (candidate == null)
                        candidate = bounds[i];
                    else
                        // There is more than one candidate type ⇒ type inference fails
                        return false;
                }
            }

            if (candidate == null)
                // There is no viable candidate ⇒ type inference fails
                return false;

            _inferred[index] = candidate;
            _fixed[index] = true;
            return true;
        }

        // Determines which of the generic type arguments are unfixed and are mentioned in the specified delegate parameter type list
        private IEnumerable<int> getContainedIn(Type delegateParameterType)
        {
            if (delegateParameterType == null)
                yield break;
            var idx = Array.IndexOf(_genericParameters, delegateParameterType);
            if (idx >= 0)
                yield return idx;
            if (delegateParameterType.IsGenericType && !delegateParameterType.IsGenericParameter)
                foreach (var unfixed in delegateParameterType.GetGenericArguments().SelectMany(arg => getContainedIn(arg)))
                    yield return unfixed;
        }

        private bool lowerBoundInference(Type u, Type v)
        {
            // 7.5.2.9 Lower-bound inferences
            // A lower-bound inference from a type U to a type V is made as follows:
            // • If V is one of the unfixed Xi then U is added to the set of lower bounds for Xi.
            // • Otherwise, sets U1…Uk and V1…Vk are determined by checking if any of the following cases apply:
            //     • V is an array type V1[…]and U is an array type U1[…] (or a type parameter whose effective base type is U1[…]) of the same rank
            //     • V is one of IEnumerable<V1>, ICollection<V1> or IList<V1> and U is a one-dimensional array type U1[](or a type parameter whose effective base type is U1[]) 
            //     • V is the type V1? and U is the type U1? 
            //     • V is a constructed class, struct, interface or delegate type C<V1…Vk> and there is a unique type C<U1…Uk> such that U (or, if U is a type parameter, its effective base class or any member of its effective interface set)
            //        is identical to, inherits from (directly or indirectly), or implements (directly or indirectly) C<U1…Uk>.
            //        (The “uniqueness” restriction means that in the case interface C<T>{} class U: C<X>, C<Y>{}, then no inference is made when inferring from U to C<T> because U1 could be X or Y.)
            //     If any of these cases apply then an inference is made from each Ui to the corresponding Vi as follows:
            //     • If  Ui is not known to be a reference type then an exact inference is made
            //     • Otherwise, if U is an array type then a lower-bound inference is made
            //     • Otherwise, if V is C<V1…Vk> then inference depends on the i-th type parameter of C:
            //         • If it is covariant then a lower-bound inference is made.
            //         • If it is contravariant then an upper-bound inference is made.
            //         • If it is invariant then an exact inference is made.
            // • Otherwise, no inferences are made.

            // If V is one of the unfixed Xi then U is added to the set of lower bounds for Xi.
            var vIndex = Array.IndexOf(_genericParameters, v);
            if (vIndex != -1 && !_fixed[vIndex])
            {
                addBound(vIndex, u, boundKind.Lower);
                return true;
            }

            var uList = new List<Type>();
            var vList = new List<Type>();
            Type[] typeArgs;
            if (v.IsArray && u.IsArray && v.GetArrayRank() == u.GetArrayRank())
            {
                uList.Add(u.GetElementType());
                vList.Add(v.GetElementType());
            }
            else if ((v.TryGetInterfaceGenericParameters(typeof(IEnumerable<>), out typeArgs) ||
                          v.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeArgs) ||
                          v.TryGetInterfaceGenericParameters(typeof(IList<>), out typeArgs))
                    && u.IsArray && u.GetArrayRank() == 1)
            {
                uList.Add(u.GetElementType());
                vList.Add(typeArgs[0]);
            }
            else if (v.IsGenericType)
            {
                var c = v.GetGenericTypeDefinition();
                vList.AddRange(v.GetGenericArguments());
                bool found = false;
                var q = new Queue<Type>(u.IsGenericParameter ? u.GetGenericParameterConstraints() : new[] { u });

                while (q.Count > 0)
                {
                    var uCandidate = q.Dequeue();
                    if (uCandidate.BaseType != null && uCandidate.BaseType != typeof(object))
                        q.Enqueue(uCandidate.BaseType);
                    q.EnqueueRange(uCandidate.GetInterfaces());

                    if (uCandidate.IsGenericType && uCandidate.GetGenericTypeDefinition() == c)
                    {
                        if (found)
                            return false;
                        found = true;
                        uList.AddRange(uCandidate.GetGenericArguments());
                    }
                }

                if (!found)
                    return true;
            }

            var vGenParams = (v.IsGenericType && !v.IsGenericTypeDefinition) ? v.GetGenericTypeDefinition().GetGenericArguments() : null;

            for (int i = 0; i < uList.Count; i++)
            {
                if (!isDefinitelyReference(uList[i]))
                {
                    if (!exactInference(uList[i], vList[i]))
                        return false;
                }
                else if (u.IsArray)
                {
                    if (!lowerBoundInference(uList[i], vList[i]))
                        return false;
                }
                else
                {
                    if (vGenParams == null)
                        throw new InternalErrorException("v is expected to be a constructed generic type");
                    if ((vGenParams[i].GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
                    {
                        if (!lowerBoundInference(uList[i], vList[i]))
                            return false;
                    }
                    else if ((vGenParams[i].GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
                    {
                        if (!upperBoundInference(uList[i], vList[i]))
                            return false;
                    }
                    else
                    {
                        if (!exactInference(uList[i], vList[i]))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool isDefinitelyReference(Type type)
        {
            if (!type.IsGenericParameter)
                return !type.IsValueType;
            if ((type.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                return true;
            if ((type.GetGenericParameterConstraints().Any(t => t != typeof(object) && !t.IsValueType)))
                return true;
            return false;
        }

        private bool upperBoundInference(Type u, Type v)
        {
            // 7.5.2.10 Upper-bound inferences
            // An upper-bound inference from a type U to a type V is made as follows:
            // • If V is one of the unfixed Xi then U is added to the set of upper bounds for Xi.
            // • Otherwise, sets V1…Vk and U1…Uk are determined by checking if any of the following cases apply:
            //     • U is an array type U1[…]and V is an array type V1[…]of the same rank
            //     • U is one of IEnumerable<Ue>, ICollection<Ue> or IList<Ue> and V is a one-dimensional array type Ve[]
            //     • U is the type U1? and V is the type V1?
            //     • U is constructed class, struct, interface or delegate type C<U1…Uk> and V is a class, struct, interface or delegate type which is identical to, inherits from (directly or indirectly), or implements (directly or indirectly) a unique type C<V1…Vk>
            //        (The “uniqueness” restriction means that if we have interface C<T>{} class V<Z>: C<X<Z>>, C<Y<Z>>{}, then no inference is made when inferring from C<U1> to V<Q>. Inferences are not made from U1 to either X<Q> or Y<Q>.)
            //     If any of these cases apply then an inference is made from each Ui to the corresponding Vi as follows:
            //     • If  Ui is not known to be a reference type then an exact inference is made
            //     • Otherwise, if V is an array type then an upper-bound inference is made
            //     • Otherwise, if U is C<U1…Uk> then inference depends on the i-th type parameter of C:
            //         • If it is covariant then an upper-bound inference is made.
            //         • If it is contravariant then a lower-bound inference is made.
            //         • If it is invariant then an exact inference is made.
            // • Otherwise, no inferences are made.	

            // If V is one of the unfixed Xi then U is added to the set of upper bounds for Xi.
            var vIndex = Array.IndexOf(_genericParameters, v);
            if (vIndex != -1 && !_fixed[vIndex])
            {
                addBound(vIndex, u, boundKind.Upper);
                return true;
            }

            var uList = new List<Type>();
            var vList = new List<Type>();
            Type[] typeArgs;
            if (v.IsArray && u.IsArray && v.GetArrayRank() == u.GetArrayRank())
            {
                uList.Add(u.GetElementType());
                vList.Add(v.GetElementType());
            }
            else if ((u.TryGetInterfaceGenericParameters(typeof(IEnumerable<>), out typeArgs) ||
                          u.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeArgs) ||
                          u.TryGetInterfaceGenericParameters(typeof(IList<>), out typeArgs))
                    && v.IsArray && v.GetArrayRank() == 1)
            {
                uList.Add(u.GetElementType());
                vList.Add(typeArgs[0]);
            }
            else if (u.IsGenericType)
            {
                var c = u.GetGenericTypeDefinition();
                uList.AddRange(u.GetGenericArguments());
                bool found = false;
                var q = new Queue<Type>(v.IsGenericParameter ? v.GetGenericParameterConstraints() : new[] { v });

                while (q.Count > 0)
                {
                    var vCandidate = q.Dequeue();
                    if (vCandidate.BaseType != null && vCandidate.BaseType != typeof(object))
                        q.Enqueue(vCandidate.BaseType);
                    q.EnqueueRange(vCandidate.GetInterfaces());

                    if (vCandidate.IsGenericType && vCandidate.GetGenericTypeDefinition() == c)
                    {
                        if (found)
                            return false;
                        found = true;
                        vList.AddRange(vCandidate.GetGenericArguments());
                    }
                }

                if (!found)
                    return true;
            }

            for (int i = 0; i < uList.Count; i++)
            {
                var isDefinitelyReference = uList[i].IsGenericParameter ? ((uList[i].GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0) : !uList[i].IsValueType;
                if (!isDefinitelyReference)
                    if (!exactInference(uList[i], vList[i]))
                        return false;
                if (v.IsArray)
                    if (!upperBoundInference(uList[i], vList[i]))
                        return false;
                if (!u.IsGenericType || u.IsGenericTypeDefinition)
                    throw new InternalErrorException("v is expected to be a constructed generic type");
                var ugParams = u.GetGenericTypeDefinition().GetGenericArguments();
                if ((ugParams[i].GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
                    if (!upperBoundInference(uList[i], vList[i]))
                        return false;
                if ((ugParams[i].GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
                    if (!lowerBoundInference(uList[i], vList[i]))
                        return false;
                if (!exactInference(uList[i], vList[i]))
                    return false;
            }

            return true;
        }

        private bool explicitParameterTypeInference(LambdaExpression e, Type t)
        {
            // 7.5.2.7 Explicit parameter type inferences
            // An explicit parameter type inference is made from an expression E to a type T in the following way:
            // • If E is an explicitly typed anonymous function with parameter types U1…Uk and T is a delegate type
            //    or expression tree type with parameter types V1…Vk then for each Ui an exact inference (§7.5.2.8)
            //    is made from Ui to the corresponding Vi.

            // Parameter must be of a delegate or expression type
            Type delegateType;
            if (typeof(Delegate).IsAssignableFrom(t))
                delegateType = t;
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Expression<>))
                delegateType = t.GetGenericArguments()[0];
            else
                return false;

            // Number of parameters must match
            var delegateParameters = delegateType.GetMethod("Invoke").GetParameters();
            if (delegateParameters.Length != e.Parameters.Count)
                return false;

            for (int i = 0; i < e.Parameters.Count; i++)
                exactInference(e.Parameters[i].Type, delegateParameters[i].ParameterType);

            return true;
        }

        private bool exactInference(Type u, Type v)
        {
            // 7.5.2.8 Exact inferences
            // An exact inference from a type U to a type V is made as follows:
            // • If V is one of the unfixed Xi then U is added to the set of exact bounds for Xi.
            // • Otherwise, sets V1…Vk and U1…Uk are determined by checking if any of the following cases apply:
            //     • V is an array type V1[…] and U is an array type U1[…]  of the same rank
            //     • V is the type V1? and U is the type U1?
            //     • V is a constructed type C<V1…Vk> and U is a constructed type C<U1…Uk> 
            //      If any of these cases apply then an exact inference is made from each Ui to the corresponding Vi.
            // • Otherwise no inferences are made.

            // If V is one of the unfixed Xi then U is added to the set of exact bounds for Xi.
            var vIndex = Array.IndexOf(_genericParameters, v);
            if (vIndex != -1 && !_fixed[vIndex])
            {
                addBound(vIndex, u, boundKind.Exact);
                return true;
            }

            // Arrays
            if (u.IsArray && v.IsArray && u.GetArrayRank() == v.GetArrayRank())
                exactInference(u.GetElementType(), v.GetElementType());

            // Constructed types — takes care of nullable, too
            if (u.IsGenericType && v.IsGenericType && u.GetGenericTypeDefinition() == v.GetGenericTypeDefinition())
            {
                var uArgs = u.GetGenericArguments();
                var vArgs = v.GetGenericArguments();
                for (int i = 0; i < uArgs.Length; i++)
                    exactInference(uArgs[i], vArgs[i]);
            }

            return true;
        }
    }
}
