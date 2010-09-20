using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.KitchenSink.ParseCs
{
    public abstract class ResolveContext
    {
        public static implicit operator ResolveContext(Type type) { return type == null ? null : new ResolveContextType(type); }
        public virtual Type ExpressionType { get { throw new InvalidOperationException("{0} does not have a type.".Fmt(this.GetType())); } }
        public virtual Expression ToExpression() { throw new InvalidOperationException("{0} does not have a translation to LINQ expressions.".Fmt(this.GetType())); }
    }

    public class ResolveContextGlobal : ResolveContext { }
    public class ResolveContextNamespace : ResolveContext
    {
        public string Namespace { get; private set; }
        public ResolveContextNamespace(string namespacе) { Namespace = namespacе; }
    }
    public class ResolveContextType : ResolveContext
    {
        public Type Type { get; private set; }
        public ResolveContextType(Type type) { Type = type; }
        public override Type ExpressionType { get { return Type; } }
    }
    public class ResolveContextExpression : ResolveContext
    {
        public Expression Expression { get; private set; }
        public bool WasAnonymousFunction { get; private set; }
        public ResolveContextExpression(Expression expression, bool wasAnonymousFunction) { Expression = expression; WasAnonymousFunction = wasAnonymousFunction; }
        public override Type ExpressionType { get { return Expression.Type; } }
        public override Expression ToExpression() { return Expression; }
    }
    public class ResolveContextLambda : ResolveContext
    {
        public CsSimpleLambdaExpression Lambda { get; private set; }
        public ResolveContextLambda(CsSimpleLambdaExpression lambda) { Lambda = lambda; }
    }
    public class ResolveContextNullLiteral : ResolveContext { }
    public abstract class ChildResolveContext : ResolveContext
    {
        public ResolveContext Parent { get; private set; }
        public ChildResolveContext(ResolveContext parent) { Parent = parent; }
    }
    public class MethodGroupMember
    {
        public MethodInfo Method;
        public bool IsExtensionMethod;
        public MethodGroupMember(MethodInfo method, bool isExtensionMethod)
        {
            Method = method;
            IsExtensionMethod = isExtensionMethod;
        }
    }
    public class ResolveContextMethodGroup : ChildResolveContext
    {
        public List<MethodGroupMember> MethodGroup { get; private set; }
        public string MethodName { get; private set; }
        public ResolveContextMethodGroup(ResolveContext parent, List<MethodGroupMember> methodGroup, string name) : base(parent) { MethodGroup = methodGroup; MethodName = name; }
        public override Type ExpressionType { get { throw new InvalidOperationException("Method overloads have not been resolved yet."); } }
        public override Expression ToExpression() { throw new InvalidOperationException("Method overloads have not been resolved yet."); }
    }
}
