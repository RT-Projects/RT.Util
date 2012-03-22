using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.ParseCs
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

    public abstract class CsNode
    {
        public int StartIndex;
        public int EndIndex;

        public virtual IEnumerable<CsNode> Subnodes
        {
            get
            {
                yield return this;

                var type = this.GetType();
                var subnodes = new List<CsNode>();
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (typeof(CsNode).IsAssignableFrom(field.FieldType))
                        subnodes.Add(field.GetValue(this) as CsNode);
                    else if (typeof(IEnumerable).IsAssignableFrom(field.FieldType))
                        subnodes.AddRange(recurse(field.GetValue(this) as IEnumerable).SelectMany(ienum => ienum.OfType<CsNode>()));
                }

                foreach (var subnode in subnodes)
                    if (subnode != null)
                        foreach (var node in subnode.Subnodes)
                            yield return node;
            }
        }

        private IEnumerable<IEnumerable> recurse(IEnumerable ienum)
        {
            if (ienum == null)
                yield break;
            yield return ienum;
            foreach (var subEnum in ienum.OfType<IEnumerable>().SelectMany(recurse))
                yield return subEnum;
        }
    }

    #region Document & Namespace
    public sealed class CsDocument : CsNode
    {
        public List<CsUsingNamespace> UsingNamespaces = new List<CsUsingNamespace>();
        public List<CsUsingAlias> UsingAliases = new List<CsUsingAlias>();
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public List<CsNamespace> Namespaces = new List<CsNamespace>();
        public List<CsType> Types = new List<CsType>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var ns in UsingNamespaces)
                sb.Append(ns.ToString());
            if (UsingNamespaces.Any())
                sb.Append('\n');

            foreach (var ns in UsingAliases)
                sb.Append(ns.ToString());
            if (UsingAliases.Any())
                sb.Append('\n');

            foreach (var attr in CustomAttributes)
                sb.Append(attr.ToString());
            if (CustomAttributes.Any())
                sb.Append('\n');

            bool first = true;
            foreach (var ns in Namespaces)
            {
                if (!first)
                    sb.Append("\n");
                first = false;
                sb.Append(ns.ToString());
            }
            foreach (var ty in Types)
            {
                if (!first)
                    sb.Append("\n");
                first = false;
                sb.Append(ty.ToString());
            }

            return sb.ToString();
        }
    }
    public abstract class CsUsing : CsNode { }
    public sealed class CsUsingNamespace : CsUsing
    {
        public string Namespace;
        public override string ToString() { return "using " + Namespace.Sanitize() + ";\n"; }
    }
    public sealed class CsUsingAlias : CsUsing
    {
        public string Alias;
        public CsTypeName Original;
        public override string ToString() { return "using " + Alias.Sanitize() + " = " + Original.ToString() + ";\n"; }
    }
    public sealed class CsNamespace : CsNode
    {
        public string Name;
        public List<CsUsingNamespace> UsingNamespaces = new List<CsUsingNamespace>();
        public List<CsUsingAlias> UsingAliases = new List<CsUsingAlias>();
        public List<CsNamespace> Namespaces = new List<CsNamespace>();
        public List<CsType> Types = new List<CsType>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("namespace ");
            sb.Append(Name.Sanitize());
            sb.Append("\n{\n");

            foreach (var ns in UsingNamespaces)
                sb.Append(ns.ToString());
            if (UsingNamespaces.Any())
                sb.Append('\n');

            foreach (var ns in UsingAliases)
                sb.Append(ns.ToString());
            if (UsingAliases.Any())
                sb.Append('\n');

            bool first = true;
            foreach (var ns in Namespaces)
            {
                if (!first)
                    sb.Append("\n");
                first = false;
                sb.Append(ns.ToString().Indent());
            }
            foreach (var ty in Types)
            {
                if (!first)
                    sb.Append("\n");
                first = false;
                sb.Append(ty.ToString().Indent());
            }

            sb.Append("}\n");
            return sb.ToString();
        }
    }
    #endregion

    #region Members, except types
    public abstract class CsMember : CsNode
    {
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public bool IsInternal, IsPrivate, IsProtected, IsPublic, IsNew, IsUnsafe;
        protected virtual StringBuilder modifiersCs()
        {
            var sb = new StringBuilder();
            foreach (var str in CustomAttributes)
                sb.Append(str);
            if (IsProtected) sb.Append("protected ");
            if (IsInternal) sb.Append("internal ");
            if (IsPrivate) sb.Append("private ");
            if (IsPublic) sb.Append("public ");
            if (IsNew) sb.Append("new ");
            if (IsUnsafe) sb.Append("unsafe ");
            return sb;
        }
    }
    public abstract class CsMultiMember : CsMember
    {
        public CsTypeName Type;
        public List<CsNameAndExpression> NamesAndInitializers = new List<CsNameAndExpression>();

        public bool IsStatic;

        protected override StringBuilder modifiersCs()
        {
            var sb = base.modifiersCs();
            if (IsStatic) sb.Append("static ");
            return sb;
        }
    }
    public abstract class CsMemberLevel2 : CsMember
    {
        public string Name;
        public CsTypeName ImplementsFrom;
        public bool IsAbstract, IsVirtual, IsOverride, IsSealed, IsStatic, IsExtern;

        public CsTypeName Type;  // for methods, this is the return type

        protected override StringBuilder modifiersCs()
        {
            var sb = base.modifiersCs();
            if (IsAbstract) sb.Append("abstract ");
            if (IsVirtual) sb.Append("virtual ");
            if (IsOverride) sb.Append("override ");
            if (IsSealed) sb.Append("sealed ");
            if (IsStatic) sb.Append("static ");
            if (IsExtern) sb.Append("extern ");
            return sb;
        }
    }
    public sealed class CsEvent : CsMultiMember
    {
        public bool IsAbstract, IsVirtual, IsOverride, IsSealed;
        public List<CsSimpleMethod> Methods = null;
        public CsTypeName ImplementsFrom;

        public override string ToString()
        {
            var sb = modifiersCs();
            if (IsAbstract) sb.Append("abstract ");
            if (IsVirtual) sb.Append("virtual ");
            if (IsOverride) sb.Append("override ");
            if (IsSealed) sb.Append("sealed ");
            sb.Append("event ");
            sb.Append(Type.ToString());
            sb.Append(' ');
            if (ImplementsFrom != null)
            {
                sb.Append(ImplementsFrom.ToString());
                sb.Append('.');
            }
            sb.Append(NamesAndInitializers.Select(n => n.ToString()).JoinString(", "));
            if (Methods != null)
            {
                sb.Append("\n{\n");
                sb.Append(Methods.Select(m => m.ToString()).JoinString().Indent());
                sb.Append("}\n");
            }
            else
                sb.Append(";\n");
            return sb.ToString();
        }
    }
    public sealed class CsField : CsMultiMember
    {
        public bool IsReadonly, IsConst, IsVolatile;
        public override string ToString()
        {
            var sb = modifiersCs();
            if (IsReadonly) sb.Append("readonly ");
            if (IsConst) sb.Append("const ");
            if (IsVolatile) sb.Append("volatile ");
            sb.Append(Type.ToString());
            sb.Append(' ');
            sb.Append(NamesAndInitializers.Select(n => n.ToString()).JoinString(", "));
            sb.Append(";\n");
            return sb.ToString();
        }
    }
    public class CsProperty : CsMemberLevel2
    {
        public List<CsSimpleMethod> Methods = new List<CsSimpleMethod>();
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(Type.ToString());
            sb.Append(' ');
            if (ImplementsFrom != null)
            {
                sb.Append(ImplementsFrom.ToString());
                sb.Append('.');
            }
            sb.Append(Name.Sanitize());
            sb.Append("\n{\n");
            sb.Append(Methods.Select(m => m.ToString()).JoinString().Indent());
            sb.Append("}\n");
            return sb.ToString();
        }
    }
    public sealed class CsIndexedProperty : CsProperty
    {
        public List<CsParameter> Parameters = new List<CsParameter>();
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(Type.ToString());
            sb.Append(' ');
            if (ImplementsFrom != null)
            {
                sb.Append(ImplementsFrom.ToString());
                sb.Append('.');
            }
            sb.Append("this[");
            sb.Append(Parameters.Select(p => p.ToString()).JoinString(", "));
            sb.Append("]\n{\n");
            sb.Append(Methods.Select(m => m.ToString()).JoinString().Indent());
            sb.Append("}\n");
            return sb.ToString();
        }
    }
    public enum MethodType { Get, Set, Add, Remove }
    public sealed class CsSimpleMethod : CsMember
    {
        public MethodType Type;
        public CsBlock Body;

        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(Type == MethodType.Get ? "get" : Type == MethodType.Set ? "set" : Type == MethodType.Add ? "add" : Type == MethodType.Remove ? "remove" : null);
            if (Body != null)
            {
                sb.Append('\n');
                sb.Append(Body.ToString());
            }
            else
                sb.Append(";\n");
            return sb.ToString();
        }
    }
    public sealed class CsMethod : CsMemberLevel2
    {
        public List<CsParameter> Parameters = new List<CsParameter>();
        public List<CsGenericParameter> GenericTypeParameters = null;
        public Dictionary<string, List<CsGenericTypeConstraint>> GenericTypeConstraints = null;
        public CsBlock MethodBody;
        public bool IsPartial;

        private string genericTypeParametersCs()
        {
            if (GenericTypeParameters == null)
                return string.Empty;
            return string.Concat("<", GenericTypeParameters.Select(g => g.ToString()).JoinString(", "), ">");
        }

        private string genericTypeConstraintsCs()
        {
            if (GenericTypeConstraints == null)
                return string.Empty;
            return GenericTypeConstraints.Select(kvp => " where " + kvp.Key.Sanitize() + " : " + kvp.Value.Select(c => c.ToString()).JoinString(", ")).JoinString();
        }

        public override string ToString()
        {
            var sb = modifiersCs();
            if (IsPartial) sb.Append("partial ");
            sb.Append(Type.ToString());
            sb.Append(' ');
            if (ImplementsFrom != null)
            {
                sb.Append(ImplementsFrom.ToString());
                sb.Append('.');
            }
            sb.Append(Name.Sanitize());
            sb.Append(genericTypeParametersCs());
            sb.Append('(');
            sb.Append(Parameters.Select(p => p.ToString()).JoinString(", "));
            sb.Append(')');
            sb.Append(genericTypeConstraintsCs());

            if (MethodBody != null)
            {
                sb.Append('\n');
                sb.Append(MethodBody.ToString());
            }
            else
                sb.Append(";\n");
            return sb.ToString();
        }

        public override IEnumerable<CsNode> Subnodes
        {
            get
            {
                yield return this;

                foreach (var subnode in Type.Subnodes)
                    yield return subnode;

                if (ImplementsFrom != null)
                    foreach (var subnode in ImplementsFrom.Subnodes)
                        yield return subnode;

                foreach (var node in CustomAttributes)
                    foreach (var subnode in node.Subnodes)
                        yield return subnode;

                foreach (var node in Parameters)
                    foreach (var subnode in node.Subnodes)
                        yield return subnode;

                if (GenericTypeParameters != null)
                    foreach (var node in GenericTypeParameters)
                        foreach (var subnode in node.Subnodes)
                            yield return subnode;

                if (GenericTypeConstraints != null)
                    foreach (var values in GenericTypeConstraints.Values)
                        foreach (var node in values)
                            foreach (var subnode in node.Subnodes)
                                yield return subnode;

                if (MethodBody != null)
                    foreach (var subnode in MethodBody.Subnodes)
                        yield return subnode;
            }
        }
    }
    public abstract class CsOperatorOverload : CsMember
    {
        public bool IsStatic;
        public CsTypeName ReturnType;
        public CsParameter Parameter;
        public CsBlock MethodBody;
        protected override StringBuilder modifiersCs()
        {
            var sb = base.modifiersCs();
            if (IsStatic) sb.Append("static ");
            return sb;
        }
    }
    public enum CastOperatorType { Implicit, Explicit }
    public sealed class CsCastOperatorOverload : CsOperatorOverload
    {
        public CastOperatorType CastType;
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(CastType == CastOperatorType.Implicit ? "implicit operator " : "explicit operator ");
            sb.Append(ReturnType.ToString());
            sb.Append('(');
            sb.Append(Parameter.ToString());
            sb.Append(")\n");
            sb.Append(MethodBody.ToString());
            return sb.ToString();
        }
    }
    public sealed class CsUnaryOperatorOverload : CsOperatorOverload
    {
        public UnaryOperator Operator;
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(ReturnType.ToString());
            sb.Append(" operator ");
            sb.Append(Operator.ToCs());
            sb.Append('(');
            sb.Append(Parameter.ToString());
            sb.Append(")\n");
            sb.Append(MethodBody.ToString());
            return sb.ToString();
        }
    }
    public sealed class CsBinaryOperatorOverload : CsOperatorOverload
    {
        public BinaryOperator Operator;
        public CsParameter SecondParameter;
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(ReturnType.ToString());
            sb.Append(" operator");
            sb.Append(Operator.ToCs());
            sb.Append('(');
            sb.Append(Parameter.ToString());
            sb.Append(", ");
            sb.Append(SecondParameter.ToString());
            sb.Append(")\n");
            sb.Append(MethodBody.ToString());
            return sb.ToString();
        }
    }
    public enum ConstructorCallType { None, This, Base }
    public sealed class CsConstructor : CsMember
    {
        public string Name;
        public List<CsParameter> Parameters = new List<CsParameter>();
        public CsBlock MethodBody;
        public ConstructorCallType CallType;
        public List<CsArgument> CallArguments = new List<CsArgument>();
        public bool IsStatic;

        public override string ToString()
        {
            var sb = modifiersCs();
            if (IsStatic) sb.Append("static ");
            sb.Append(Name.Sanitize());
            sb.Append('(');
            sb.Append(Parameters.Select(p => p.ToString()).JoinString(", "));
            sb.Append(')');

            if (CallType != ConstructorCallType.None)
            {
                sb.Append(CallType == ConstructorCallType.Base ? " : base(" : CallType == ConstructorCallType.This ? " : this(" : null);
                sb.Append(CallArguments.Select(p => p.ToString()).JoinString(", "));
                sb.Append(')');
            }

            sb.Append('\n');
            sb.Append(MethodBody.ToString());
            return sb.ToString();
        }
    }
    public sealed class CsDestructor : CsMember
    {
        public string Name;
        public CsBlock MethodBody;

        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append('~');
            sb.Append(Name.Sanitize());
            sb.Append("()\n");
            sb.Append(MethodBody.ToString());
            return sb.ToString();
        }
    }
    #endregion

    #region Types
    public abstract class CsType : CsMember
    {
        public string Name;
    }
    public abstract class CsTypeCanBeGeneric : CsType
    {
        public List<CsGenericParameter> GenericTypeParameters = null;
        public Dictionary<string, List<CsGenericTypeConstraint>> GenericTypeConstraints = null;

        protected string genericTypeParametersCs()
        {
            if (GenericTypeParameters == null)
                return string.Empty;
            return string.Concat("<", GenericTypeParameters.Select(g => g.ToString()).JoinString(", "), ">");
        }

        protected string genericTypeConstraintsCs()
        {
            if (GenericTypeConstraints == null)
                return string.Empty;
            return GenericTypeConstraints.Select(kvp => " where " + kvp.Key.Sanitize() + " : " + kvp.Value.Select(c => c.ToString()).JoinString(", ")).JoinString();
        }
    }
    public abstract class CsTypeLevel2 : CsTypeCanBeGeneric
    {
        public bool IsPartial;

        public List<CsTypeName> BaseTypes = null;
        public List<CsMember> Members = new List<CsMember>();

        protected abstract string typeTypeCs { get; }
        protected override StringBuilder modifiersCs()
        {
            var sb = base.modifiersCs();
            if (IsPartial) sb.Append("partial ");
            return sb;
        }
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append(typeTypeCs);
            sb.Append(' ');
            sb.Append(Name.Sanitize());
            sb.Append(genericTypeParametersCs());
            if (BaseTypes != null && BaseTypes.Any())
            {
                sb.Append(" : ");
                sb.Append(BaseTypes.Select(ty => ty.ToString()).JoinString(", "));
            }
            sb.Append(genericTypeConstraintsCs());
            if (Members.Count == 0)
                sb.Append(" { }\n");
            else
            {
                sb.Append("\n{\n");
                CsMember prevMember = null;
                foreach (var member in Members)
                {
                    if (prevMember != null && (!(member is CsField) || !(prevMember is CsField)))
                        sb.Append('\n');
                    sb.Append(member.ToString().Indent());
                    prevMember = member;
                }
                sb.Append("}\n");
            }
            return sb.ToString();
        }
    }
    public sealed class CsInterface : CsTypeLevel2
    {
        protected override string typeTypeCs { get { return "interface"; } }
    }
    public sealed class CsStruct : CsTypeLevel2
    {
        protected override string typeTypeCs { get { return "struct"; } }
    }
    public sealed class CsClass : CsTypeLevel2
    {
        public bool IsAbstract, IsSealed, IsStatic;

        protected override string typeTypeCs { get { return "class"; } }
        protected override StringBuilder modifiersCs()
        {
            var sb = base.modifiersCs();
            if (IsAbstract) sb.Append("abstract ");
            if (IsSealed) sb.Append("sealed ");
            if (IsStatic) sb.Append("static ");
            return sb;
        }
    }
    public sealed class CsDelegate : CsTypeCanBeGeneric
    {
        public CsTypeName ReturnType;
        public List<CsParameter> Parameters = new List<CsParameter>();
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append("delegate ");
            sb.Append(ReturnType.ToString());
            sb.Append(' ');
            sb.Append(Name.Sanitize());
            sb.Append(genericTypeParametersCs());
            sb.Append('(');
            sb.Append(Parameters.Select(p => p.ToString()).JoinString(", "));
            sb.Append(')');
            sb.Append(genericTypeConstraintsCs());
            sb.Append(";\n");
            return sb.ToString();
        }
    }
    public sealed class CsEnum : CsType
    {
        public CsTypeName BaseType;
        public List<CsEnumValue> EnumValues = new List<CsEnumValue>();
        public override string ToString()
        {
            var sb = modifiersCs();
            sb.Append("enum ");
            sb.Append(Name.Sanitize());
            if (!EnumValues.Any())
            {
                sb.Append(" { }\n");
                return sb.ToString();
            }
            sb.Append("\n{\n");
            sb.Append(EnumValues.Select(ev => ev.ToString()).JoinString().Indent());
            sb.Remove(sb.Length - 2, 1);  // remove the last comma from the last enum value
            sb.Append("}\n");
            return sb.ToString();
        }
    }
    public sealed class CsEnumValue : CsNode
    {
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public string Name;
        public CsExpression LiteralValue;
        public override string ToString()
        {
            var sb = new StringBuilder(CustomAttributes.Select(c => c.ToString()).JoinString());
            sb.Append(Name.Sanitize());
            if (LiteralValue != null)
            {
                sb.Append(" = ");
                sb.Append(LiteralValue.ToString());
            }
            sb.Append(",\n");
            return sb.ToString();
        }
    }
    #endregion

    #region Parameters, simple names and type names
    public sealed class CsParameter : CsNode
    {
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public CsTypeName Type;
        public string Name;
        public CsExpression DefaultValue;
        public bool IsThis, IsOut, IsRef, IsParams;
        public override string ToString()
        {
            // If 'Type' is null, this is a parameter to a lambda expression where the type is not specified.
            if (Type == null)
                return Name.Sanitize();

            var sb = new StringBuilder(CustomAttributes.Select(c => c.ToString()).JoinString());
            if (IsThis) sb.Append("this ");
            if (IsParams) sb.Append("params ");
            if (IsOut) sb.Append("out ");
            if (IsRef) sb.Append("ref ");
            sb.Append(Type.ToString());
            sb.Append(' ');
            sb.Append(Name.Sanitize());
            if (DefaultValue != null)
            {
                sb.Append(" = ");
                sb.Append(DefaultValue.ToString());
            }
            return sb.ToString();
        }
    }

    public abstract class CsSimpleName : CsNode
    {
        public abstract bool EndsWithGenerics { get; }
    }
    public sealed class CsSimpleNameIdentifier : CsSimpleName
    {
        public string Name;
        public List<CsTypeName> GenericTypeArguments = null;
        public override string ToString() { return GenericTypeArguments == null ? Name.Sanitize() : string.Concat(Name.Sanitize(), '<', GenericTypeArguments.Select(p => p.ToString()).JoinString(", "), '>'); }
        public override bool EndsWithGenerics { get { return GenericTypeArguments != null && GenericTypeArguments.Count > 0; } }
    }
    public sealed class CsSimpleNameBuiltin : CsSimpleName
    {
        public string Builtin;
        public override string ToString() { return Builtin; }
        public override bool EndsWithGenerics { get { return false; } }
    }

    public abstract class CsTypeName : CsNode
    {
        public virtual string GetSingleIdentifier() { return null; }
    }
    public sealed class CsEmptyGenericParameter : CsTypeName
    {
        public override string ToString() { return string.Empty; }
    }
    public sealed class CsConcreteTypeName : CsTypeName
    {
        public bool HasGlobal;
        public List<CsSimpleName> Parts = new List<CsSimpleName>();
        public override string ToString() { return (HasGlobal ? "global::" : string.Empty) + Parts.Select(p => p.ToString()).JoinString("."); }
        public override string GetSingleIdentifier()
        {
            if (HasGlobal || Parts.Count != 1)
                return null;
            var identifier = Parts[0] as CsSimpleNameIdentifier;
            return identifier != null && identifier.GenericTypeArguments == null ? identifier.Name : null;
        }
    }
    public sealed class CsArrayTypeName : CsTypeName
    {
        public CsTypeName InnerType;
        public List<int> ArrayRanks = new List<int> { 1 };
        public override string ToString() { return InnerType.ToString() + ArrayRanks.Select(rank => string.Concat("[", new string(',', rank - 1), "]")).JoinString(); }
    }
    public sealed class CsPointerTypeName : CsTypeName
    {
        public CsTypeName InnerType;
        public override string ToString() { return InnerType.ToString() + "*"; }
    }
    public sealed class CsNullableTypeName : CsTypeName
    {
        public CsTypeName InnerType;
        public override string ToString() { return InnerType.ToString() + "?"; }
    }
    #endregion

    #region Generics
    public abstract class CsGenericTypeConstraint : CsNode { }
    public sealed class CsGenericTypeConstraintNew : CsGenericTypeConstraint { public override string ToString() { return "new()"; } }
    public sealed class CsGenericTypeConstraintClass : CsGenericTypeConstraint { public override string ToString() { return "class"; } }
    public sealed class CsGenericTypeConstraintStruct : CsGenericTypeConstraint { public override string ToString() { return "struct"; } }
    public sealed class CsGenericTypeConstraintBaseClass : CsGenericTypeConstraint
    {
        public CsTypeName BaseClass;
        public override string ToString() { return BaseClass.ToString(); }
    }
    #endregion

    #region Custom attributes
    public sealed class CsCustomAttribute : CsNode
    {
        public CsTypeName Type;
        public List<CsArgument> Arguments = new List<CsArgument>();
        public List<CsNameAndExpression> PropertySetters = new List<CsNameAndExpression>();
        public override string ToString()
        {
            if (Arguments.Count + PropertySetters.Count == 0)
                return Type.ToString();
            return string.Concat(Type.ToString(), '(', Arguments.Concat<CsNode>(PropertySetters).Select(p => p.ToString()).JoinString(", "), ')');
        }
    }
    public enum CustomAttributeLocation { None, Assembly, Module, Type, Method, Property, Field, Event, Param, Return, Typevar }
    public sealed class CsCustomAttributeGroup : CsNode
    {
        public CustomAttributeLocation Location;
        public List<CsCustomAttribute> CustomAttributes;
        public bool NoNewLine = false;
        public override string ToString()
        {
            var sb = new StringBuilder("[");
            switch (Location)
            {
                case CustomAttributeLocation.Assembly: sb.Append("assembly: "); break;
                case CustomAttributeLocation.Module: sb.Append("module: "); break;
                case CustomAttributeLocation.Type: sb.Append("type: "); break;
                case CustomAttributeLocation.Method: sb.Append("method: "); break;
                case CustomAttributeLocation.Property: sb.Append("property: "); break;
                case CustomAttributeLocation.Field: sb.Append("field: "); break;
                case CustomAttributeLocation.Event: sb.Append("event: "); break;
                case CustomAttributeLocation.Param: sb.Append("param: "); break;
                case CustomAttributeLocation.Return: sb.Append("return: "); break;
                case CustomAttributeLocation.Typevar: sb.Append("typevar: "); break;
            }
            sb.Append(CustomAttributes.Select(c => c.ToString()).JoinString(", "));
            sb.Append(NoNewLine ? "] " : "]\n");
            return sb.ToString();
        }
    }
    #endregion

    #region Statements
    public abstract class CsStatement : CsNode
    {
        public List<string> GotoLabels;
        protected string gotoLabels() { return GotoLabels == null ? string.Empty : GotoLabels.Select(g => g.Sanitize() + ':').JoinString(" ") + (this is CsEmptyStatement ? " " : "\n"); }
    }
    public sealed class CsEmptyStatement : CsStatement { public override string ToString() { return gotoLabels() + ";\n"; } }
    public sealed class CsBlock : CsStatement
    {
        public List<CsStatement> Statements = new List<CsStatement>();
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            sb.Append("{\n");
            foreach (var st in Statements)
                sb.Append(st.ToString().Indent());
            sb.Append("}\n");
            return sb.ToString();
        }
    }
    public abstract class CsOptionalExpressionStatement : CsStatement
    {
        public CsExpression Expression;
        public abstract string Keyword { get; }
        public override string ToString()
        {
            if (Expression == null)
                return string.Concat(gotoLabels(), Keyword, ";\n");
            return string.Concat(gotoLabels(), Keyword, ' ', Expression.ToString(), ";\n");
        }
    }
    public sealed class CsReturnStatement : CsOptionalExpressionStatement { public override string Keyword { get { return "return"; } } }
    public sealed class CsThrowStatement : CsOptionalExpressionStatement { public override string Keyword { get { return "throw"; } } }
    public abstract class CsBlockStatement : CsStatement { public CsBlock Block; }
    public sealed class CsCheckedStatement : CsBlockStatement { public override string ToString() { return string.Concat(gotoLabels(), "checked\n", Block.ToString()); } }
    public sealed class CsUncheckedStatement : CsBlockStatement { public override string ToString() { return string.Concat(gotoLabels(), "unchecked\n", Block.ToString()); } }
    public sealed class CsUnsafeStatement : CsBlockStatement { public override string ToString() { return string.Concat(gotoLabels(), "unsafe\n", Block.ToString()); } }
    public sealed class CsSwitchStatement : CsStatement
    {
        public CsExpression SwitchOn;
        public List<CsCaseLabel> Cases = new List<CsCaseLabel>();
        public override string ToString() { return string.Concat(gotoLabels(), "switch (", SwitchOn.ToString(), ")\n{\n", Cases.Select(c => c.ToString().Indent()).JoinString("\n"), "}\n"); }
    }
    public sealed class CsCaseLabel : CsNode
    {
        public List<CsExpression> CaseValues = new List<CsExpression>();  // use a 'null' expression for the 'default' label
        public List<CsStatement> Statements;
        public override string ToString() { return string.Concat(CaseValues.Select(c => c == null ? "default:\n" : "case " + c.ToString() + ":\n").JoinString(), Statements.Select(s => s.ToString().Indent()).JoinString()); }
    }
    public sealed class CsExpressionStatement : CsStatement
    {
        public CsExpression Expression;
        public override string ToString() { return string.Concat(gotoLabels(), Expression.ToString(), ";\n"); }
    }
    public sealed class CsVariableDeclarationStatement : CsStatement
    {
        public CsTypeName Type;
        public List<CsNameAndExpression> NamesAndInitializers = new List<CsNameAndExpression>();
        public bool IsConst;
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            if (IsConst) sb.Append("const ");
            sb.Append(Type.ToString());
            sb.Append(' ');
            sb.Append(NamesAndInitializers.Select(n => n.ToString()).JoinString(", "));
            sb.Append(";\n");
            return sb.ToString();
        }
    }
    public sealed class CsForeachStatement : CsStatement
    {
        public CsTypeName VariableType;
        public string VariableName;
        public CsExpression LoopExpression;
        public CsStatement Body;
        public override string ToString() { return string.Concat(gotoLabels(), "foreach (", VariableType == null ? string.Empty : VariableType.ToString() + ' ', VariableName.Sanitize(), " in ", LoopExpression.ToString(), ")\n", Body is CsBlock ? Body.ToString() : Body.ToString().Indent()); }
    }
    public sealed class CsForStatement : CsStatement
    {
        public List<CsStatement> InitializationStatements = new List<CsStatement>();
        public CsExpression TerminationCondition;
        public List<CsExpression> LoopExpressions = new List<CsExpression>();
        public CsStatement Body;
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            sb.Append("for (");
            sb.Append(InitializationStatements.Select(i => i.ToString().Trim().TrimEnd(';')).JoinString(", "));
            sb.Append("; ");
            if (TerminationCondition != null)
                sb.Append(TerminationCondition.ToString());
            sb.Append("; ");
            sb.Append(LoopExpressions.Select(l => l.ToString()).JoinString(", "));
            sb.Append(")\n");
            sb.Append(Body is CsBlock ? Body.ToString() : Body.ToString().Indent());
            return sb.ToString();
        }
    }
    public sealed class CsUsingStatement : CsStatement
    {
        public CsStatement InitializationStatement;
        public CsStatement Body;
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            sb.Append("using (");
            sb.Append(InitializationStatement.ToString().Trim().TrimEnd(';'));
            sb.Append(")\n");
            sb.Append(Body is CsBlock || Body is CsUsingStatement ? Body.ToString() : Body.ToString().Indent());
            return sb.ToString();
        }
    }
    public sealed class CsFixedStatement : CsStatement
    {
        public CsStatement InitializationStatement;
        public CsStatement Body;
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            sb.Append("fixed (");
            sb.Append(InitializationStatement.ToString().Trim().TrimEnd(';'));
            sb.Append(")\n");
            sb.Append(Body is CsBlock || Body is CsFixedStatement ? Body.ToString() : Body.ToString().Indent());
            return sb.ToString();
        }
    }
    public sealed class CsIfStatement : CsStatement
    {
        public CsExpression IfExpression;
        public CsStatement Statement;
        public CsStatement ElseStatement;
        public override string ToString()
        {
            if (ElseStatement == null)
                return string.Concat(gotoLabels(), "if (", IfExpression.ToString(), ")\n", Statement is CsBlock ? Statement.ToString() : Statement.ToString().Indent());
            else if (ElseStatement is CsIfStatement)
                return string.Concat(gotoLabels(), "if (", IfExpression.ToString(), ")\n", Statement is CsBlock ? Statement.ToString() : Statement.ToString().Indent(), "else ", ElseStatement.ToString());
            else
                return string.Concat(gotoLabels(), "if (", IfExpression.ToString(), ")\n", Statement is CsBlock ? Statement.ToString() : Statement.ToString().Indent(), "else\n", ElseStatement is CsBlock ? ElseStatement.ToString() : ElseStatement.ToString().Indent());
        }
    }
    public abstract class CsExpressionBlockStatement : CsStatement
    {
        public CsExpression Expression;
        public CsStatement Statement;
        public abstract string Keyword { get; }
        public override string ToString() { return string.Concat(gotoLabels(), Keyword, " (", Expression.ToString(), ")\n", Statement is CsBlock ? Statement.ToString() : Statement.ToString().Indent()); }
    }
    public sealed class CsWhileStatement : CsExpressionBlockStatement { public override string Keyword { get { return "while"; } } }
    public sealed class CsLockStatement : CsExpressionBlockStatement { public override string Keyword { get { return "lock"; } } }
    public sealed class CsDoWhileStatement : CsStatement
    {
        public CsExpression WhileExpression;
        public CsStatement Statement;
        public override string ToString() { return string.Concat(gotoLabels(), "do\n", Statement is CsBlock ? Statement.ToString() : Statement.ToString().Indent(), "while (", WhileExpression.ToString(), ");\n"); }
    }
    public sealed class CsTryStatement : CsStatement
    {
        public CsBlock Block;
        public List<CsCatchClause> Catches = new List<CsCatchClause>();
        public CsBlock Finally;
        public override string ToString()
        {
            var sb = new StringBuilder(gotoLabels());
            sb.Append("try\n");
            sb.Append(Block.ToString());
            foreach (var ctch in Catches)
                sb.Append(ctch.ToString());
            if (Finally != null)
            {
                sb.Append("finally\n");
                sb.Append(Finally.ToString());
            }
            return sb.ToString();
        }
    }
    public sealed class CsCatchClause : CsNode
    {
        public CsTypeName Type;
        public string Name;
        public CsBlock Block;
        public override string ToString()
        {
            var sb = new StringBuilder("catch");
            if (Type != null)
            {
                sb.Append(" (");
                sb.Append(Type.ToString());
                if (Name != null)
                {
                    sb.Append(' ');
                    sb.Append(Name.Sanitize());
                }
                sb.Append(")");
            }
            sb.Append("\n");
            sb.Append(Block is CsBlock ? Block.ToString() : Block.ToString().Indent());
            return sb.ToString();
        }
    }
    public sealed class CsGotoStatement : CsStatement { public string Label; public override string ToString() { return string.Concat(gotoLabels(), "goto ", Label.Sanitize(), ";\n"); } }
    public sealed class CsContinueStatement : CsStatement { public override string ToString() { return string.Concat(gotoLabels(), "continue;\n"); } }
    public sealed class CsBreakStatement : CsStatement { public override string ToString() { return string.Concat(gotoLabels(), "break;\n"); } }
    public sealed class CsGotoDefaultStatement : CsStatement { public override string ToString() { return string.Concat(gotoLabels(), "goto default;\n"); } }
    public sealed class CsGotoCaseStatement : CsStatement { public CsExpression Expression; public override string ToString() { return string.Concat(gotoLabels(), "goto case ", Expression.ToString(), ";\n"); } }
    public sealed class CsYieldBreakStatement : CsStatement { public override string ToString() { return string.Concat(gotoLabels(), "yield break;\n"); } }
    public sealed class CsYieldReturnStatement : CsStatement { public CsExpression Expression; public override string ToString() { return string.Concat(gotoLabels(), "yield return ", Expression.ToString(), ";\n"); } }
    #endregion

    #region Expressions
    public abstract class CsExpression : CsNode
    {
        internal virtual ResolveContext ToResolveContext(NameResolver resolver, bool isChecked) { return new ResolveContextExpression(ToLinqExpression(resolver, isChecked)); }
        internal static ResolveContext ToResolveContext(CsExpression expr, NameResolver resolver, bool isChecked) { return expr.ToResolveContext(resolver, isChecked); }
        public abstract Expression ToLinqExpression(NameResolver resolver, bool isChecked);
    }
    public enum AssignmentOperator { Eq, TimesEq, DivEq, ModEq, PlusEq, MinusEq, ShlEq, ShrEq, AndEq, XorEq, OrEq }
    public sealed class CsAssignmentExpression : CsExpression
    {
        public AssignmentOperator Operator;
        public CsExpression Left, Right;
        public override string ToString()
        {
            return string.Concat(
                Left.ToString(),
                Operator == AssignmentOperator.Eq ? " = " :
                Operator == AssignmentOperator.TimesEq ? " *= " :
                Operator == AssignmentOperator.DivEq ? " /= " :
                Operator == AssignmentOperator.ModEq ? " %= " :
                Operator == AssignmentOperator.PlusEq ? " += " :
                Operator == AssignmentOperator.MinusEq ? " -= " :
                Operator == AssignmentOperator.ShlEq ? " <<= " :
                Operator == AssignmentOperator.ShrEq ? " >>= " :
                Operator == AssignmentOperator.AndEq ? " &= " :
                Operator == AssignmentOperator.XorEq ? " ^= " :
                Operator == AssignmentOperator.OrEq ? " |= " : null,
                Right.ToString());
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsConditionalExpression : CsExpression
    {
        public CsExpression Condition, TruePart, FalsePart;
        public override string ToString()
        {
            return string.Concat(Condition.ToString(), " ? ", TruePart.ToString(), " : ", FalsePart.ToString());
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public enum BinaryOperator
    {
        // The following operators are used both in CsBinaryOperatorExpression and in CsBinaryOperatorOverload
        Times, Div, Mod, Plus, Minus, Shl, Shr, Less, Greater, LessEq, GreaterEq, Eq, NotEq, And, Xor, Or,

        // The following operators are used only in CsBinaryOperatorExpression
        AndAnd, OrOr, Coalesce
    }
    public sealed class CsBinaryOperatorExpression : CsExpression
    {
        public BinaryOperator Operator;
        public CsExpression Left, Right;
        public override string ToString() { return string.Concat(Left.ToString(), ' ', Operator.ToCs(), ' ', Right.ToString()); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked)
        {
            var left = Left.ToLinqExpression(resolver, isChecked);
            var right = Right.ToLinqExpression(resolver, isChecked);
            if (left.Type != right.Type)
            {
                var conv = Conversion.Implicit(left.Type, right.Type);
                if (conv != null)
                    left = Expression.Convert(left, right.Type);
                else if ((conv = Conversion.Implicit(right.Type, left.Type)) != null)
                    right = Expression.Convert(right, left.Type);
            }

            switch (Operator)
            {
                case BinaryOperator.Times:
                    return isChecked ? Expression.MultiplyChecked(left, right) : Expression.Multiply(left, right);
                case BinaryOperator.Div:
                    return Expression.Divide(left, right);
                case BinaryOperator.Mod:
                    return Expression.Modulo(left, right);
                case BinaryOperator.Plus:
                    return isChecked ? Expression.AddChecked(left, right) : Expression.Add(left, right);
                case BinaryOperator.Minus:
                    return isChecked ? Expression.SubtractChecked(left, right) : Expression.Subtract(left, right);
                case BinaryOperator.Shl:
                    return Expression.LeftShift(left, right);
                case BinaryOperator.Shr:
                    return Expression.RightShift(left, right);
                case BinaryOperator.Less:
                    return Expression.LessThan(left, right);
                case BinaryOperator.Greater:
                    return Expression.GreaterThan(left, right);
                case BinaryOperator.LessEq:
                    return Expression.LessThanOrEqual(left, right);
                case BinaryOperator.GreaterEq:
                    return Expression.GreaterThanOrEqual(left, right);
                case BinaryOperator.Eq:
                    return Expression.Equal(left, right);
                case BinaryOperator.NotEq:
                    return Expression.NotEqual(left, right);
                case BinaryOperator.And:
                    return Expression.And(left, right);
                case BinaryOperator.Xor:
                    return Expression.ExclusiveOr(left, right);
                case BinaryOperator.Or:
                    return Expression.Or(left, right);
                case BinaryOperator.AndAnd:
                    return Expression.AndAlso(left, right);
                case BinaryOperator.OrOr:
                    return Expression.OrElse(left, right);
                case BinaryOperator.Coalesce:
                    return Expression.Coalesce(left, right);
                default:
                    throw new InvalidOperationException("Unexpected binary operator: " + Operator);
            }
        }
    }
    public enum BinaryTypeOperator { Is, As }
    public sealed class CsBinaryTypeOperatorExpression : CsExpression
    {
        public BinaryTypeOperator Operator;
        public CsExpression Left;
        public CsTypeName Right;
        public override string ToString()
        {
            return string.Concat(
                Left.ToString(),
                Operator == BinaryTypeOperator.Is ? " is " :
                Operator == BinaryTypeOperator.As ? " as " : null,
                Right.ToString()
            );
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public enum UnaryOperator
    {
        // The following unary operators are used both in CsUnaryOperatorExpression and in CsOperatorOverload
        Plus, Minus, Not, Neg, PrefixInc, PrefixDec,

        // The following unary operators are used only in CsUnaryOperatorExpression
        PostfixInc, PostfixDec, PointerDeref, AddressOf,

        // The following unary operators are used only in CsOperatorOverload
        True, False
    }
    public sealed class CsUnaryOperatorExpression : CsExpression
    {
        public UnaryOperator Operator;
        public CsExpression Operand;
        public override string ToString()
        {
            if (Operator == UnaryOperator.PostfixInc)
                return Operand.ToString() + "++";
            if (Operator == UnaryOperator.PostfixDec)
                return Operand.ToString() + "--";
            return Operator.ToCs() + Operand.ToString();
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked)
        {
            // Special case: if this is a unary minus operator that operates on a CsNumberLiteralExpression,
            // simply add the "-" and parse the number directly
            if (Operator == UnaryOperator.Minus && Operand is CsNumberLiteralExpression)
                return Expression.Constant(ParserUtil.ParseNumericLiteral("-" + ((CsNumberLiteralExpression) Operand).Literal));
            throw new NotImplementedException();
        }
    }
    public sealed class CsCastExpression : CsExpression
    {
        public CsTypeName Type;
        public CsExpression Operand;
        public override string ToString() { return string.Concat('(', Type.ToString(), ") ", Operand.ToString()); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked)
        {
            var operand = ToResolveContext(Operand, resolver, isChecked);
            var type = resolver.ResolveType(Type);
            if (operand is ResolveContextLambda)
                throw new NotImplementedException();
            return Expression.Convert(operand.ToExpression(), type);
        }
    }
    public enum MemberAccessType { Regular, PointerDeref };
    public sealed class CsMemberAccessExpression : CsExpression
    {
        public MemberAccessType AccessType;
        public CsExpression Left;
        public CsSimpleName Right;
        public override string ToString() { return string.Concat(Left.ToString(), AccessType == MemberAccessType.PointerDeref ? "->" : ".", Right.ToString()); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return ToResolveContext(resolver, isChecked).ToExpression(); }
        internal override ResolveContext ToResolveContext(NameResolver resolver, bool isChecked)
        {
            if (AccessType == MemberAccessType.PointerDeref)
                throw new InvalidOperationException("Pointer dereference is not supported in LINQ expressions.");
            return resolver.ResolveSimpleName(Right, ToResolveContext(Left, resolver, isChecked));
        }
    }
    public sealed class CsFunctionCallExpression : CsExpression
    {
        public bool IsIndexer;
        public CsExpression Left;
        public List<CsArgument> Arguments = new List<CsArgument>();
        public override string ToString() { return string.Concat(Left.ToString(), IsIndexer ? '[' : '(', Arguments.Select(p => p.ToString()).JoinString(", "), IsIndexer ? ']' : ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return ToResolveContext(resolver, isChecked).ToExpression(); }
        internal override ResolveContext ToResolveContext(NameResolver resolver, bool isChecked)
        {
            if (Arguments.Any(a => a.ArgumentMode != ArgumentMode.In))
                throw new NotImplementedException("out and ref parameters are not implemented.");

            var left = ToResolveContext(Left, resolver, isChecked);
            var resolvedArguments = Arguments.Select(a => new ArgumentInfo(a.ArgumentName, ToResolveContext(a.ArgumentExpression, resolver, isChecked), a.ArgumentMode));

            if (IsIndexer)
            {
                var property = ParserUtil.ResolveOverloads(
                    left.ExpressionType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(p => Tuple.Create(p, p.GetIndexParameters()))
                        .ToList(),
                    resolvedArguments,
                    resolver);
                throw new NotImplementedException();
            }

            var leftMg = left as ResolveContextMethodGroup;
            if (leftMg != null)
            {
                // Try non-extension methods first, then extension methods
                for (int i = 0; i < 2; i++)
                {
                    var method = ParserUtil.ResolveOverloads(
                        leftMg.MethodGroup.Where(mg => mg.IsExtensionMethod == (i == 1)).Select(mg => Tuple.Create(mg.Method, mg.Method.GetParameters())).ToList(),
                        // For extension methods, add the expression that pretends to be the “this” instance as the first argument
                        i == 0 ? resolvedArguments : new[] { new ArgumentInfo(null, leftMg.Parent, ArgumentMode.In) }.Concat(resolvedArguments),
                        resolver);
                    if (method != null)
                        return new ResolveContextExpression(Expression.Call(method.Member.IsStatic ? null : leftMg.Parent.ToExpression(), method.Member, method.Parameters.Select(arg =>
                        {
                            var argExpr = arg.Argument.ToExpression();
                            return (argExpr.Type != arg.ParameterType) ? Expression.Convert(argExpr, arg.ParameterType) : argExpr;
                        })), wasAnonymousFunction: false);
                }
                throw new InvalidOperationException("Cannot determine which method overload to use for “{0}”. Either type inference failed or the call is ambiguous.".Fmt(leftMg.MethodName));
            }

            throw new NotImplementedException();
        }
    }
    public abstract class CsTypeOperatorExpression : CsExpression { public CsTypeName Type; }
    public sealed class CsTypeofExpression : CsTypeOperatorExpression
    {
        public override string ToString() { return string.Concat("typeof(", Type.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsSizeofExpression : CsTypeOperatorExpression
    {
        public override string ToString() { return string.Concat("sizeof(", Type.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsDefaultExpression : CsTypeOperatorExpression
    {
        public override string ToString() { return string.Concat("default(", Type.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public abstract class CsCheckedUncheckedExpression : CsExpression { public CsExpression Subexpression; }
    public sealed class CsCheckedExpression : CsCheckedUncheckedExpression
    {
        public override string ToString() { return string.Concat("checked(", Subexpression.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsUncheckedExpression : CsCheckedUncheckedExpression
    {
        public override string ToString() { return string.Concat("unchecked(", Subexpression.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsSimpleNameExpression : CsExpression
    {
        public CsSimpleName SimpleName;
        public override string ToString() { return SimpleName.ToString(); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return ToResolveContext(resolver, isChecked).ToExpression(); }
        internal override ResolveContext ToResolveContext(NameResolver resolver, bool isChecked) { return resolver.ResolveSimpleName(SimpleName); }
    }
    public sealed class CsParenthesizedExpression : CsExpression
    {
        public CsExpression Subexpression;
        public override string ToString() { return string.Concat('(', Subexpression.ToString(), ')'); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return Subexpression.ToLinqExpression(resolver, isChecked); }
    }
    public sealed class CsStringLiteralExpression : CsExpression
    {
        public string Literal;
        public override string ToString()
        {
            bool useVerbatim;

            // If the string contains any of the escapable characters, use those escape sequences.
            if (Literal.Any(ch => "\0\a\b\f\n\r\t\v".Contains(ch)))
                useVerbatim = false;
            // Otherwise, if the string contains a double-quote or backslash, use verbatim.
            else if (Literal.Any(ch => ch == '"' || ch == '\\'))
                useVerbatim = true;
            // In all other cases, use escape sequences.
            else
                useVerbatim = false;

            if (useVerbatim)
                return string.Concat('@', '"', Literal.Split('"').JoinString("\"\""), '"');
            else
                return string.Concat('"', Literal.Select(ch => ch.CsEscape(false, true)).JoinString(), '"');
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return Expression.Constant(Literal); }
    }
    public sealed class CsCharacterLiteralExpression : CsExpression
    {
        public char Literal;
        public override string ToString() { return string.Concat('\'', Literal.CsEscape(true, false), '\''); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNumberLiteralExpression : CsExpression
    {
        public string Literal;  // Could break this down further, but this is the safest
        public override string ToString() { return Literal; }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { return Expression.Constant(ParserUtil.ParseNumericLiteral(Literal)); }
    }
    public sealed class CsBooleanLiteralExpression : CsExpression
    {
        public bool Literal;
        public override string ToString() { return Literal ? "true" : "false"; }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNullExpression : CsExpression
    {
        public override string ToString() { return "null"; }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsThisExpression : CsExpression
    {
        public override string ToString() { return "this"; }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsBaseExpression : CsExpression
    {
        public override string ToString() { return "base"; }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNewConstructorExpression : CsExpression
    {
        public CsTypeName Type;
        public List<CsArgument> Arguments = new List<CsArgument>();
        public List<CsExpression> Initializers;
        public override string ToString()
        {
            var sb = new StringBuilder("new ");
            sb.Append(Type.ToString());
            if (Arguments.Any() || Initializers == null)
            {
                sb.Append('(');
                sb.Append(Arguments.Select(p => p.ToString()).JoinString(", "));
                sb.Append(')');
            }
            if (Initializers != null)
            {
                sb.Append(" { ");
                sb.Append(Initializers.Select(ini => ini.ToString()).JoinString(", "));
                sb.Append(" }");
            }
            return sb.ToString();
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNewAnonymousTypeExpression : CsExpression
    {
        public List<CsExpression> Initializers = new List<CsExpression>();
        public override string ToString() { return string.Concat("new { ", Initializers.Select(ini => ini.ToString()).JoinString(", "), " }"); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNewImplicitlyTypedArrayExpression : CsExpression
    {
        public List<CsExpression> Items = new List<CsExpression>();
        public override string ToString() { return Items.Count == 0 ? "new[] { }" : string.Concat("new[] { ", Items.Select(p => p.ToString()).JoinString(", "), " }"); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsNewArrayExpression : CsExpression
    {
        public CsTypeName Type;
        public List<CsExpression> SizeExpressions = new List<CsExpression>();
        public List<int> AdditionalRanks = new List<int>();
        public List<CsExpression> Items;
        public override string ToString()
        {
            var sb = new StringBuilder("new ");
            sb.Append(Type.ToString());
            sb.Append('[');
            sb.Append(SizeExpressions.Select(s => s.ToString()).JoinString(", "));
            sb.Append(']');
            sb.Append(AdditionalRanks.Select(a => "[" + new string(',', a - 1) + ']').JoinString());
            if (Items != null)
            {
                if (Items.Count == 0)
                    sb.Append(" { }");
                else
                {
                    sb.Append(" { ");
                    sb.Append(Items.Select(p => p.ToString()).JoinString(", "));
                    sb.Append(" }");
                }
            }
            return sb.ToString();
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public abstract class CsLambdaExpression : CsExpression
    {
        public List<CsParameter> Parameters = new List<CsParameter>();
        protected StringBuilder parametersCs()
        {
            var sb = new StringBuilder();
            if (Parameters.Count == 1 && Parameters[0].Type == null)
                sb.Append(Parameters[0].ToString());
            else
            {
                sb.Append('(');
                sb.Append(Parameters.Select(p => p.ToString()).JoinString(", "));
                sb.Append(')');
            }
            sb.Append(" =>");
            return sb;
        }
    }
    public sealed class CsSimpleLambdaExpression : CsLambdaExpression
    {
        public CsExpression Body;
        public override string ToString() { return string.Concat(parametersCs(), ' ', Body.ToString()); }
        private bool isImplicit()
        {
            return Parameters.Count > 0 && Parameters[0].Type == null;
        }
        internal override ResolveContext ToResolveContext(NameResolver resolver, bool isChecked)
        {
            // If parameter types are not specified, cannot turn into expression yet
            if (isImplicit())
                return new ResolveContextLambda(this);
            return new ResolveContextExpression(ToLinqExpression(resolver, isChecked), wasAnonymousFunction: true);
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked)
        {
            if (isImplicit())
                throw new InvalidOperationException("The implicitly-typed lambda expression “{0}” cannot be translated to a LINQ expression without knowing the types of its parameters. Use the ToLinqExpression(NameResolver,Type[]) overload to specify the parameter types.".Fmt(ToString()));

            var prmTypes = new Type[Parameters.Count];
            for (int i = 0; i < Parameters.Count; i++)
                prmTypes[i] = resolver.ResolveType(Parameters[i].Type);
            return ToLinqExpression(resolver, prmTypes, isChecked);
        }
        public Expression ToLinqExpression(NameResolver resolver, Type[] parameterTypes, bool isChecked)
        {
            if (parameterTypes.Length != Parameters.Count)
                throw new ArgumentException("Number of supplied parameter types does not match number of parameters on the lambda.");

            var prmExprs = new ParameterExpression[Parameters.Count];
            for (int i = 0; i < Parameters.Count; i++)
            {
                prmExprs[i] = Expression.Parameter(parameterTypes[i], Parameters[i].Name);
                resolver.AddLocalName(Parameters[i].Name, prmExprs[i]);
            }

            var body = Body.ToLinqExpression(resolver, isChecked);
            var lambda = Expression.Lambda(body, prmExprs);

            for (int i = 0; i < Parameters.Count; i++)
                resolver.ForgetLocalName(Parameters[i].Name);

            return lambda;
        }
    }
    public sealed class CsBlockLambdaExpression : CsLambdaExpression
    {
        public CsBlock Block;
        public override string ToString() { return string.Concat(parametersCs(), '\n', Block.ToString().Trim()); }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsAnonymousMethodExpression : CsExpression
    {
        public List<CsParameter> Parameters;
        public CsBlock Block;
        public override string ToString()
        {
            if (Parameters == null)
                return "delegate\n" + Block.ToString().Trim();
            else
                return string.Concat("delegate(", Parameters.Select(p => p.ToString()).JoinString(", "), ")\n", Block.ToString().Trim());
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsArrayLiteralExpression : CsExpression
    {
        public List<CsExpression> Expressions = new List<CsExpression>();
        public override string ToString()
        {
            if (!Expressions.Any())
                return "{ }";
            var sb = new StringBuilder();
            sb.Append("{ ");
            bool first = true;
            foreach (var expr in Expressions)
            {
                if (!first)
                    sb.Append(", ");
                sb.Append(expr.ToString());
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public sealed class CsLinqExpression : CsExpression
    {
        public List<CsLinqElement> Elements = new List<CsLinqElement>();
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Elements[0]);
            foreach (var elem in Elements.Skip(1))
            {
                sb.Append('\n');
                sb.Append(elem.ToString().Indent());
            }
            return sb.ToString();
        }
        public override Expression ToLinqExpression(NameResolver resolver, bool isChecked) { throw new NotImplementedException(); }
    }
    public abstract class CsLinqElement : CsNode { }
    public sealed class CsLinqFromClause : CsLinqElement
    {
        public string ItemName;
        public CsExpression SourceExpression;
        public override string ToString() { return string.Concat("from ", ItemName.Sanitize(), " in ", SourceExpression.ToString()); }
    }
    public sealed class CsLinqJoinClause : CsLinqElement
    {
        public string ItemName;
        public CsExpression SourceExpression, KeyExpression1, KeyExpression2;
        public string IntoName;
        public override string ToString()
        {
            var s = string.Concat("join ", ItemName.Sanitize(), " in ", SourceExpression, " on ", KeyExpression1, " equals ", KeyExpression2);
            return IntoName == null ? s : string.Concat(s, " into ", IntoName.Sanitize());
        }
    }
    public sealed class CsLinqLetClause : CsLinqElement
    {
        public string ItemName;
        public CsExpression Expression;
        public override string ToString() { return string.Concat("let ", ItemName.Sanitize(), " = ", Expression.ToString()); }
    }
    public sealed class CsLinqWhereClause : CsLinqElement
    {
        public CsExpression WhereExpression;
        public override string ToString() { return string.Concat("where ", WhereExpression.ToString()); }
    }
    public sealed class CsLinqOrderByClause : CsLinqElement
    {
        public List<CsLinqOrderBy> KeyExpressions = new List<CsLinqOrderBy>();
        public override string ToString()
        {
            return "orderby " + KeyExpressions.Select(k => k.ToString()).JoinString(", ");
        }
    }
    public sealed class CsLinqSelectClause : CsLinqElement
    {
        public CsExpression SelectExpression;
        public override string ToString() { return string.Concat("select ", SelectExpression.ToString()); }
    }
    public sealed class CsLinqGroupByClause : CsLinqElement
    {
        public CsExpression SelectionExpression;
        public CsExpression KeyExpression;
        public override string ToString() { return string.Concat("group ", SelectionExpression.ToString(), " by ", KeyExpression.ToString()); }
    }
    public sealed class CsLinqIntoClause : CsLinqElement
    {
        public string ItemName;
        public override string ToString() { return string.Concat("into ", ItemName.Sanitize()); }
    }

    #endregion

    #region Miscellaneous
    public sealed class CsNameAndExpression : CsNode
    {
        public string Name;
        public CsExpression Expression;
        public override string ToString()
        {
            if (Expression == null)
                return Name.Sanitize();
            return string.Concat(Name.Sanitize(), " = ", Expression.ToString());
        }
    }

    public enum VarianceMode { Invariant, Covariant, Contravariant }

    public sealed class CsGenericParameter : CsNode
    {
        public string Name;
        public VarianceMode Variance;
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public override string ToString()
        {
            return CustomAttributes.Select(c => c.ToString()).JoinString() + (Variance == VarianceMode.Covariant ? "out " : Variance == VarianceMode.Contravariant ? "in " : "") + Name.Sanitize();
        }
    }

    public enum ArgumentMode { In, Out, Ref }

    public sealed class CsArgument : CsNode
    {
        public string ArgumentName;
        public ArgumentMode ArgumentMode;
        public CsExpression ArgumentExpression;
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (ArgumentName != null)
            {
                sb.Append(ArgumentName);
                sb.Append(": ");
            }
            if (ArgumentMode == ArgumentMode.Out)
                sb.Append("out ");
            else if (ArgumentMode == ArgumentMode.Ref)
                sb.Append("ref ");
            sb.Append(ArgumentExpression.ToString());
            return sb.ToString();
        }
    }

    public enum LinqOrderByType { None, Ascending, Descending }

    public sealed class CsLinqOrderBy : CsNode
    {
        public CsExpression OrderByExpression;
        public LinqOrderByType OrderByType;
        public override string ToString()
        {
            return OrderByExpression.ToString() + (OrderByType == LinqOrderByType.Ascending ? " ascending" : OrderByType == LinqOrderByType.Descending ? " descending" : string.Empty);
        }
    }
    #endregion
}
