using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.KitchenSink.ParseCs
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

    [XmlIgnoreIfDefault, XmlIgnoreIfEmpty]
    public abstract class CsNode { }

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
        public CsTypeIdentifier Original;
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
            var sb = new StringBuilder(CustomAttributes.Select(c => c.ToString()).JoinString());
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
        public CsTypeIdentifier Type;
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
        public CsTypeIdentifier ImplementsFrom;
        public bool IsAbstract, IsVirtual, IsOverride, IsSealed, IsStatic, IsExtern;

        public CsTypeIdentifier Type;  // for methods, this is the return type

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
        public List<CsNameAndCustomAttributes> GenericTypeParameters = null;
        public Dictionary<string, List<CsGenericTypeConstraint>> GenericTypeConstraints = null;
        public CsBlock MethodBody;
        public bool IsPartial;

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
    }
    public abstract class CsOperatorOverload : CsMember
    {
        public bool IsStatic;
        public CsTypeIdentifier ReturnType;
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
        public List<CsNameAndCustomAttributes> GenericTypeParameters = null;
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

        public List<CsTypeIdentifier> BaseTypes = null;
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
        public CsTypeIdentifier ReturnType;
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
        public CsTypeIdentifier BaseType;
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

    #region Parameters and type identifiers
    public sealed class CsParameter : CsNode
    {
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public CsTypeIdentifier Type;
        public string Name;
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
            return sb.ToString();
        }
    }
    public abstract class CsTypeIdentifier : CsNode
    {
        public virtual string GetSingleIdentifier() { return null; }
    }
    public sealed class CsEmptyGenericTypeIdentifier : CsTypeIdentifier
    {
        public override string ToString() { return string.Empty; }
    }
    public abstract class CsConcreteTypeIdentifierPart : CsNode { }
    public sealed class CsConcreteTypeIdentifierPartIdentifier : CsConcreteTypeIdentifierPart
    {
        public string Name;
        public List<CsTypeIdentifier> GenericTypeArguments = null;
        public override string ToString() { return GenericTypeArguments == null ? Name.Sanitize() : string.Concat(Name.Sanitize(), '<', GenericTypeArguments.Select(p => p.ToString()).JoinString(", "), '>'); }
    }
    public sealed class CsConcreteTypeIdentifierPartBuiltin : CsConcreteTypeIdentifierPart
    {
        public string Builtin;
        public override string ToString() { return Builtin; }
    }
    public sealed class CsConcreteTypeIdentifier : CsTypeIdentifier
    {
        public bool HasGlobal;
        public List<CsConcreteTypeIdentifierPart> Parts = new List<CsConcreteTypeIdentifierPart>();
        public override string ToString() { return (HasGlobal ? "global::" : string.Empty) + Parts.Select(p => p.ToString()).JoinString("."); }
        public override string GetSingleIdentifier()
        {
            return !HasGlobal && Parts.Count == 1 &&
                    Parts[0] is CsConcreteTypeIdentifierPartIdentifier &&
                    ((CsConcreteTypeIdentifierPartIdentifier) Parts[0]).GenericTypeArguments == null
                ? ((CsConcreteTypeIdentifierPartIdentifier) Parts[0]).Name
                : null;
        }
    }
    public sealed class CsArrayTypeIdentifier : CsTypeIdentifier
    {
        public CsTypeIdentifier InnerType;
        public List<int> ArrayRanks = new List<int> { 1 };
        public override string ToString() { return InnerType.ToString() + ArrayRanks.Select(rank => string.Concat("[", new string(',', rank - 1), "]")).JoinString(); }
    }
    public sealed class CsPointerTypeIdentifier : CsTypeIdentifier
    {
        public CsTypeIdentifier InnerType;
        public override string ToString() { return InnerType.ToString() + "*"; }
    }
    public sealed class CsNullableTypeIdentifier : CsTypeIdentifier
    {
        public CsTypeIdentifier InnerType;
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
        public CsTypeIdentifier BaseClass;
        public override string ToString() { return BaseClass.ToString(); }
    }
    #endregion

    #region Custom attributes
    public sealed class CsCustomAttribute : CsNode
    {
        public CsTypeIdentifier Type;
        public List<CsExpression> Positional = new List<CsExpression>();
        public List<CsNameAndExpression> Named = new List<CsNameAndExpression>();
        public override string ToString()
        {
            if (Positional.Count + Named.Count == 0)
                return Type.ToString();
            return string.Concat(Type.ToString(), '(', Positional.Select(p => p.ToString()).Concat(Named.Select(p => p.ToString())).JoinString(", "), ')');
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
        public CsTypeIdentifier Type;
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
        public CsTypeIdentifier VariableType;
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
        public CsTypeIdentifier Type;
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
    public abstract class CsExpression : CsNode { }
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
    }
    public sealed class CsConditionalExpression : CsExpression
    {
        public CsExpression Left, Middle, Right;
        public override string ToString()
        {
            return string.Concat(Left.ToString(), " ? ", Middle.ToString(), " : ", Right.ToString());
        }
    }
    public enum BinaryOperator { Times, Div, Mod, Plus, Minus, Shl, Shr, Less, Greater, LessEq, GreaterEq, Eq, NotEq, And, Xor, Or, AndAnd, OrOr, Coalesce }
    public sealed class CsBinaryOperatorExpression : CsExpression
    {
        public BinaryOperator Operator;
        public CsExpression Left, Right;
        public override string ToString() { return string.Concat(Left.ToString(), ' ', Operator.ToCs(), ' ', Right.ToString()); }
    }
    public enum BinaryTypeOperator { Is, As }
    public sealed class CsBinaryTypeOperatorExpression : CsExpression
    {
        public BinaryTypeOperator Operator;
        public CsExpression Left;
        public CsTypeIdentifier Right;
        public override string ToString()
        {
            return string.Concat(
                Left.ToString(),
                Operator == BinaryTypeOperator.Is ? " is " :
                Operator == BinaryTypeOperator.As ? " as " : null,
                Right.ToString()
            );
        }
    }
    public enum UnaryOperator { Plus, Minus, Not, Neg, PrefixInc, PrefixDec, PostfixInc, PostfixDec, PointerDeref, AddressOf, True, False }
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
    }
    public sealed class CsCastExpression : CsExpression
    {
        public CsTypeIdentifier Type;
        public CsExpression Operand;
        public override string ToString() { return string.Concat('(', Type.ToString(), ") ", Operand.ToString()); }
    }
    public enum MemberAccessType { Regular, PointerDeref };
    public sealed class CsMemberAccessExpression : CsExpression
    {
        public MemberAccessType AccessType;
        public CsExpression Left, Right;
        public override string ToString() { return string.Concat(Left.ToString(), AccessType == MemberAccessType.PointerDeref ? "->" : ".", Right.ToString()); }
    }
    public sealed class CsFunctionCallExpression : CsExpression
    {
        public bool IsIndexer;
        public CsExpression Left;
        public List<CsArgument> Arguments = new List<CsArgument>();
        public override string ToString() { return string.Concat(Left.ToString(), IsIndexer ? '[' : '(', Arguments.Select(p => p.ToString()).JoinString(", "), IsIndexer ? ']' : ')'); }
    }
    public abstract class CsTypeOperatorExpression : CsExpression { public CsTypeIdentifier Type; }
    public sealed class CsTypeofExpression : CsTypeOperatorExpression { public override string ToString() { return string.Concat("typeof(", Type.ToString(), ')'); } }
    public sealed class CsSizeofExpression : CsTypeOperatorExpression { public override string ToString() { return string.Concat("sizeof(", Type.ToString(), ')'); } }
    public sealed class CsDefaultExpression : CsTypeOperatorExpression { public override string ToString() { return string.Concat("default(", Type.ToString(), ')'); } }
    public abstract class CsCheckedUncheckedExpression : CsExpression { public CsExpression Subexpression; }
    public sealed class CsCheckedExpression : CsCheckedUncheckedExpression { public override string ToString() { return string.Concat("checked(", Subexpression.ToString(), ')'); } }
    public sealed class CsUncheckedExpression : CsCheckedUncheckedExpression { public override string ToString() { return string.Concat("unchecked(", Subexpression.ToString(), ')'); } }
    public sealed class CsTypeIdentifierExpression : CsExpression
    {
        public CsTypeIdentifier Type;
        public override string ToString() { return Type.ToString(); }
    }
    public sealed class CsIdentifierExpression : CsExpression
    {
        public string Identifier;
        public override string ToString() { return Identifier.Sanitize(); }
    }
    public sealed class CsParenthesizedExpression : CsExpression
    {
        public CsExpression Subexpression;
        public override string ToString() { return string.Concat('(', Subexpression.ToString(), ')'); }
    }
    public sealed class CsStringLiteralExpression : CsExpression
    {
        private static char[] SpecialCharacters1 = new[] { '\0', '\a', '\b', '\f', '\n', '\r', '\t', '\v' };
        private static char[] SpecialCharacters2 = new[] { '"', '\\' };
        public string Literal;
        public override string ToString()
        {
            bool useVerbatim;

            // If the string contains any of the escapable characters, use those escape sequences.
            if (Literal.Any(ch => SpecialCharacters1.Contains(ch)))
                useVerbatim = false;
            // Otherwise, if the string contains a double-quote or backslash, use verbatim.
            else if (Literal.Any(ch => SpecialCharacters2.Contains(ch)))
                useVerbatim = true;
            // In all other cases, use escape sequences.
            else
                useVerbatim = false;

            if (useVerbatim)
                return string.Concat('@', '"', Literal.Split('"').JoinString("\"\""), '"');
            else
                return string.Concat('"', Literal.Select(ch => ch.CsEscape(false, true)).JoinString(), '"');
        }
    }
    public sealed class CsCharacterLiteralExpression : CsExpression
    {
        public char Literal;
        public override string ToString() { return string.Concat('\'', Literal.CsEscape(true, false), '\''); }
    }
    public sealed class CsNumberLiteralExpression : CsExpression
    {
        public string Literal;  // Could break this down further, but this is the safest
        public override string ToString() { return Literal; }
    }
    public sealed class CsBooleanLiteralExpression : CsExpression { public bool Literal; public override string ToString() { return Literal ? "true" : "false"; } }
    public sealed class CsNullExpression : CsExpression { public override string ToString() { return "null"; } }
    public sealed class CsThisExpression : CsExpression { public override string ToString() { return "this"; } }
    public sealed class CsBaseExpression : CsExpression { public override string ToString() { return "base"; } }
    public sealed class CsNewConstructorExpression : CsExpression
    {
        public CsTypeIdentifier Type;
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
    }
    public sealed class CsNewAnonymousTypeExpression : CsExpression
    {
        public List<CsExpression> Initializers = new List<CsExpression>();
        public override string ToString() { return string.Concat("new { ", Initializers.Select(ini => ini.ToString()).JoinString(", "), " }"); }
    }
    public sealed class CsNewImplicitlyTypedArrayExpression : CsExpression
    {
        public List<CsExpression> Items = new List<CsExpression>();
        public override string ToString() { return Items.Count == 0 ? "new[] { }" : string.Concat("new[] { ", Items.Select(p => p.ToString()).JoinString(", "), " }"); }
    }
    public sealed class CsNewArrayExpression : CsExpression
    {
        public CsTypeIdentifier Type;
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
    }
    public abstract class CsLambaExpression : CsExpression
    {
        public List<CsParameter> ParameterNames = new List<CsParameter>();
        protected StringBuilder parametersCs()
        {
            var sb = new StringBuilder();
            if (ParameterNames.Count == 1 && ParameterNames[0].Type == null)
                sb.Append(ParameterNames[0].ToString());
            else
            {
                sb.Append('(');
                sb.Append(ParameterNames.Select(p => p.ToString()).JoinString(", "));
                sb.Append(')');
            }
            sb.Append(" =>");
            return sb;
        }
    }
    public sealed class CsSimpleLambdaExpression : CsLambaExpression
    {
        public CsExpression Expression;
        public override string ToString() { return string.Concat(parametersCs(), ' ', Expression.ToString()); }
    }
    public sealed class CsBlockLambdaExpression : CsLambaExpression
    {
        public CsBlock Block;
        public override string ToString() { return string.Concat(parametersCs(), '\n', Block.ToString().Trim()); }
    }
    public sealed class CsAnonymousMethodExpression : CsExpression
    {
        public List<CsParameter> Parameters;
        public CsBlock Block;
        public override string ToString() { return string.Concat("delegate(", Parameters.Select(p => p.ToString()).JoinString(", "), ")\n", Block.ToString().Trim()); }
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

    public sealed class CsNameAndCustomAttributes : CsNode
    {
        public string Name;
        public List<CsCustomAttributeGroup> CustomAttributes = new List<CsCustomAttributeGroup>();
        public override string ToString()
        {
            return CustomAttributes.Select(c => c.ToString()).JoinString() + Name.Sanitize();
        }
    }

    public enum ArgumentType { In, Out, Ref }

    public sealed class CsArgument : CsNode
    {
        public ArgumentType ArgumentType;
        public CsExpression ArgumentExpression;
        public override string ToString()
        {
            return (ArgumentType == ArgumentType.Out ? "out " : ArgumentType == ArgumentType.Ref ? "ref " : string.Empty) + ArgumentExpression.ToString();
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

    public static class Extensions
    {
        public static string Indent(this string input)
        {
            return Regex.Replace(input, "^(?!$)", "    ", RegexOptions.Multiline);
        }
        public static string CsEscape(this char ch, bool singleQuote, bool doubleQuote)
        {
            switch (ch)
            {
                case '\\': return "\\\\";
                case '\0': return "\\0";
                case '\a': return "\\a";
                case '\b': return "\\b";
                case '\f': return "\\f";
                case '\n': return "\\n";
                case '\r': return "\\r";
                case '\t': return "\\t";
                case '\v': return "\\v";
                case '\'': return singleQuote ? "\\'" : "'";
                case '"': return doubleQuote ? "\\\"" : "\"";
                default: return ch.ToString();
            }
        }
        public static string ToCs(this BinaryOperator op)
        {
            return
                op == BinaryOperator.Times ? "*" :
                op == BinaryOperator.Div ? "/" :
                op == BinaryOperator.Mod ? "%" :
                op == BinaryOperator.Plus ? "+" :
                op == BinaryOperator.Minus ? "-" :
                op == BinaryOperator.Shl ? "<<" :
                op == BinaryOperator.Shr ? ">>" :
                op == BinaryOperator.Less ? "<" :
                op == BinaryOperator.Greater ? ">" :
                op == BinaryOperator.LessEq ? "<=" :
                op == BinaryOperator.GreaterEq ? ">=" :
                op == BinaryOperator.Eq ? "==" :
                op == BinaryOperator.NotEq ? "!=" :
                op == BinaryOperator.And ? "&" :
                op == BinaryOperator.Xor ? "^" :
                op == BinaryOperator.Or ? "|" :
                op == BinaryOperator.AndAnd ? "&&" :
                op == BinaryOperator.OrOr ? "||" :
                op == BinaryOperator.Coalesce ? "??" : null;
        }
        public static string ToCs(this UnaryOperator op)
        {
            return
                op == UnaryOperator.Plus ? "+" :
                op == UnaryOperator.Minus ? "-" :
                op == UnaryOperator.Not ? "!" :
                op == UnaryOperator.Neg ? "~" :
                op == UnaryOperator.PrefixInc ? "++" :
                op == UnaryOperator.PrefixDec ? "--" :
                op == UnaryOperator.PostfixInc ? "++" :
                op == UnaryOperator.PostfixDec ? "--" :
                op == UnaryOperator.PointerDeref ? "*" :
                op == UnaryOperator.AddressOf ? "&" :
                op == UnaryOperator.True ? "true" :
                op == UnaryOperator.False ? "false" : null;
        }
        public static string Sanitize(this string identifier)
        {
            if (Lexer.Keywords.Contains(identifier))
                return "@" + identifier;
            return identifier;
        }
    }
}
