using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RT.Util.ExtensionMethods;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

namespace RT.KitchenSink.ParseCs
{
    abstract class ResolveContext
    {
        public virtual Type ExpressionType { get { throw new InvalidOperationException("{0} does not have a type.".Fmt(this.GetType())); } }
        public virtual Expression ToExpression() { throw new InvalidOperationException("{0} does not have a translation to LINQ expressions.".Fmt(this.GetType())); }
    }

    class ResolveContextGlobal : ResolveContext { }
    class ResolveContextNamespace : ResolveContext
    {
        public string Namespace { get; private set; }
        public ResolveContextNamespace(string namespacе) { Namespace = namespacе; }
    }
    class ResolveContextType : ResolveContext
    {
        public Type Type { get; private set; }
        public ResolveContextType(Type type) { Type = type; }
        public override Type ExpressionType { get { return Type; } }
    }
    class ResolveContextConstant : ResolveContext
    {
        public object Constant { get; private set; }
        public Type Type { get; private set; }
        public ResolveContextConstant(object constant, Type type = null)
        {
            if (constant == null && type == null)
                throw new ArgumentNullException("constant", "constant and type cannot both be null.");
            Constant = constant;
            Type = type ?? constant.GetType();
        }
        public override Type ExpressionType { get { return Type; } }
        public override Expression ToExpression() { return Expression.Constant(Constant, Type); }
    }
    class ResolveContextExpression : ResolveContext
    {
        public Expression Expression { get; private set; }
        public bool WasAnonymousFunction { get; private set; }
        public ResolveContextExpression(Expression expression, bool wasAnonymousFunction = false) { Expression = expression; WasAnonymousFunction = wasAnonymousFunction; }
        public override Type ExpressionType { get { return Expression.Type; } }
        public override Expression ToExpression() { return Expression; }
    }
    class ResolveContextLambda : ResolveContext
    {
        public CsSimpleLambdaExpression Lambda { get; private set; }
        public ResolveContextLambda(CsSimpleLambdaExpression lambda) { Lambda = lambda; }
    }
    class ResolveContextNullLiteral : ResolveContext { }
    class MethodGroupMember
    {
        public MethodInfo Method;
        public bool IsExtensionMethod;
        public MethodGroupMember(MethodInfo method, bool isExtensionMethod)
        {
            Method = method;
            IsExtensionMethod = isExtensionMethod;
        }
    }
    class ResolveContextMethodGroup : ResolveContext
    {
        public ResolveContext Parent { get; private set; }
        public List<MethodGroupMember> MethodGroup { get; private set; }
        public string MethodName { get; private set; }
        public ResolveContextMethodGroup(ResolveContext parent, List<MethodGroupMember> methodGroup, string name) : base() { Parent = parent; MethodGroup = methodGroup; MethodName = name; }
        public override Type ExpressionType { get { throw new InvalidOperationException("Method overloads have not been resolved yet."); } }
        public override Expression ToExpression() { throw new InvalidOperationException("Method overloads have not been resolved yet."); }
    }
}
