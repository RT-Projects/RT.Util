using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.ParseCs
{
    /// <summary>Provides functionality to parse C# code into a parse-tree representation.</summary>
    public static class Parser
    {
        /// <summary>
        /// Parses the specified C# source code into a parse tree.
        /// </summary>
        /// <param name="source">C# source code to parse.</param>
        /// <exception cref="ParseException">The specified C# source code could not be parsed.</exception>
        public static CsDocument ParseDocument(string source)
        {
            var tokens = Lexer.Lex(source, Lexer.LexOptions.IgnoreComments);
            int tokenIndex = 0;
            return parseDocument(tokens, ref tokenIndex);
        }

        /// <summary>
        /// Parses the specified C# expression into a parse tree.
        /// </summary>
        /// <param name="source">C# source code to parse.</param>
        /// <exception cref="ParseException">The specified C# source code could not be parsed.</exception>
        public static CsExpression ParseExpression(string source)
        {
            var tokens = Lexer.Lex(source, Lexer.LexOptions.IgnoreComments);
            int tokenIndex = 0;
            return parseExpression(tokens, ref tokenIndex);
        }

        [Flags]
        private enum typeIdentifierFlags
        {
            AllowKeywords = 1,
            AllowEmptyGenerics = 2,
            AllowNullablesAndPointers = 4,
            AllowArrays = 8,
            AllowShr = 16,
            DontAllowDot = 32,
            Lenient = 64,
        }

        private static string[] assignmentOperators = new[] { "=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", "&=", "^=", "|=" };

        private static string[] builtinTypes = new[] { "bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "void" };

        #region Misc
        private static CsDocument parseDocument(TokenJar tok, ref int i)
        {
            var doc = new CsDocument { StartIndex = tok[i].StartIndex };
            try
            {
                while (tok.IndexExists(i))
                {
                    var j = i;
                    object result = parseMemberDeclaration(tok, ref i, true);
                    if (result is CsUsingAlias)
                        doc.UsingAliases.Add((CsUsingAlias) result);
                    else if (result is CsUsingNamespace)
                        doc.UsingNamespaces.Add((CsUsingNamespace) result);
                    else if (result is CsNamespace)
                        doc.Namespaces.Add((CsNamespace) result);
                    else if (result is CsType)
                        doc.Types.Add((CsType) result);
                    else if (result is CsCustomAttributeGroup)
                        doc.CustomAttributes.Add((CsCustomAttributeGroup) result);
                    else
                        throw new ParseException("Members cannot be declared directly at the top level. Expected 'using', 'namespace', or a type declaration.", tok[j].StartIndex, doc);
                }
            }
            catch (LexException e)
            {
                throw new ParseException(e.Message, e.Index, doc, e);
            }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsUsingAlias)
                    doc.UsingAliases.Add((CsUsingAlias) e.IncompleteResult);
                else if (e.IncompleteResult is CsUsingNamespace)
                    doc.UsingNamespaces.Add((CsUsingNamespace) e.IncompleteResult);
                else if (e.IncompleteResult is CsNamespace)
                    doc.Namespaces.Add((CsNamespace) e.IncompleteResult);
                else if (e.IncompleteResult is CsType)
                    doc.Types.Add((CsType) e.IncompleteResult);
                throw new ParseException(e.Message, e.Index, doc, e);
            }
            doc.EndIndex = tok[i - 1].EndIndex;
            return doc;
        }
        private static CsUsing parseUsingDeclaration(TokenJar tok, ref int i)
        {
            var startIndex = tok[i].StartIndex;
            tok[i].Assert("using");
            i++;

            var firstIdent = tok[i].Identifier();
            i++;
            if (tok[i].IsBuiltin("="))
            {
                i++;
                var typeIdent = parseTypeName(tok, ref i, 0).Item1;
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected. (1)", tok[i].StartIndex);
                i++;
                return new CsUsingAlias { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Alias = firstIdent, Original = typeIdent };
            }
            else if (tok[i].IsBuiltin("."))
            {
                i++;
                var sb = new StringBuilder(firstIdent);
                sb.Append('.');
                sb.Append(tok[i].Identifier());
                i++;
                while (tok[i].IsBuiltin("."))
                {
                    sb.Append('.');
                    i++;
                    sb.Append(tok[i].Identifier());
                    i++;
                }
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("'.' or ';' expected.", tok[i].StartIndex);
                i++;
                return new CsUsingNamespace { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Namespace = sb.ToString() };
            }
            else if (tok[i].IsBuiltin(";"))
            {
                i++;
                return new CsUsingNamespace { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Namespace = firstIdent };
            }

            throw new ParseException("'=', '.' or ';' expected.", tok[i].StartIndex);
        }
        private static CsNamespace parseNamespace(TokenJar tok, ref int i)
        {
            var startIndex = tok[i].StartIndex;
            tok[i].Assert("namespace");
            i++;

            var sb = new StringBuilder(tok[i].Identifier());
            i++;
            while (tok[i].IsBuiltin("."))
            {
                i++;
                sb.Append('.');
                sb.Append(tok[i].Identifier());
                i++;
            }
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'.' or '{' expected.", tok[i].StartIndex);
            i++;

            var ns = new CsNamespace { StartIndex = startIndex, Name = sb.ToString() };
            try
            {
                while (!tok[i].IsBuiltin("}"))
                {
                    var j = i;
                    object result = parseMemberDeclaration(tok, ref i, false);
                    if (result is CsUsingAlias)
                        ns.UsingAliases.Add((CsUsingAlias) result);
                    else if (result is CsUsingNamespace)
                        ns.UsingNamespaces.Add((CsUsingNamespace) result);
                    else if (result is CsNamespace)
                        ns.Namespaces.Add((CsNamespace) result);
                    else if (result is CsType)
                        ns.Types.Add((CsType) result);
                    else
                        throw new ParseException("Members cannot be declared directly in a namespace. Expected 'using', 'namespace', or a type declaration.", tok[j].StartIndex);
                }
                i++;
            }
            catch (LexException e)
            {
                throw new ParseException(e.Message, e.Index, ns, e);
            }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsUsingAlias)
                    ns.UsingAliases.Add((CsUsingAlias) e.IncompleteResult);
                else if (e.IncompleteResult is CsUsingNamespace)
                    ns.UsingNamespaces.Add((CsUsingNamespace) e.IncompleteResult);
                else if (e.IncompleteResult is CsNamespace)
                    ns.Namespaces.Add((CsNamespace) e.IncompleteResult);
                else if (e.IncompleteResult is CsType)
                    ns.Types.Add((CsType) e.IncompleteResult);
                throw new ParseException(e.Message, e.Index, ns, e);
            }
            ns.EndIndex = tok[i - 1].EndIndex;
            return ns;
        }
        private static CsCustomAttributeGroup parseCustomAttributeGroup(TokenJar tok, ref int i, bool noNewLine)
        {
            var startIndex = tok[i].StartIndex;
            tok[i].Assert("[");
            i++;

            var loc = CustomAttributeLocation.None;
            if ((tok[i].Type == TokenType.Identifier || tok[i].Type == TokenType.Builtin) && tok[i + 1].IsBuiltin(":"))
            {
                switch (tok[i].TokenStr)
                {
                    case "assembly": loc = CustomAttributeLocation.Assembly; break;
                    case "event": loc = CustomAttributeLocation.Event; break;
                    case "field": loc = CustomAttributeLocation.Field; break;
                    case "method": loc = CustomAttributeLocation.Method; break;
                    case "module": loc = CustomAttributeLocation.Module; break;
                    case "param": loc = CustomAttributeLocation.Param; break;
                    case "property": loc = CustomAttributeLocation.Property; break;
                    case "return": loc = CustomAttributeLocation.Return; break;
                    case "type": loc = CustomAttributeLocation.Type; break;
                    case "typevar": loc = CustomAttributeLocation.Typevar; break;
                    default:
                        throw new ParseException("Unrecognized custom attribute location. Valid locations are: 'assembly', 'event', 'field', 'method', 'module', 'param', 'property', 'return', 'type', 'typevar'.", tok[i].StartIndex);
                }
                i += 2;
            }

            List<CsCustomAttribute> group = new List<CsCustomAttribute>();
            while (true)
            {
                var attributeStartIndex = tok[i].StartIndex;
                var type = parseTypeName(tok, ref i, 0).Item1;
                var attr = new CsCustomAttribute { StartIndex = attributeStartIndex, EndIndex = tok[i - 1].EndIndex, Type = type };
                group.Add(attr);
                if (tok[i].IsBuiltin("]"))
                {
                    i++;
                    break;
                }
                else if (tok[i].IsBuiltin(","))
                {
                    i++;
                    continue;
                }
                else if (!tok[i].IsBuiltin("("))
                    throw new ParseException("'(', ',' or ']' expected.", tok[i].StartIndex);
                i++;
                bool acceptPositionalArgs = true;
                bool acceptOptionalArgs = true;
                bool expectComma = false;
                while (!tok[i].IsBuiltin(")"))
                {
                    if (expectComma)
                    {
                        if (!tok[i].IsBuiltin(","))
                            throw new ParseException("'',' or ')' expected. (1)", tok[i].StartIndex);
                        i++;
                    }
                    expectComma = true;
                    var argStartIndex = tok[i].StartIndex;
                    if (tok[i].Type == TokenType.Identifier && tok[i + 1].IsBuiltin("="))
                    {
                        acceptPositionalArgs = false;
                        acceptOptionalArgs = false;
                        var posName = tok[i].TokenStr;
                        i += 2;
                        var expr = parseExpression(tok, ref i);
                        attr.PropertySetters.Add(new CsNameAndExpression { StartIndex = argStartIndex, EndIndex = tok[i - 1].EndIndex, Name = posName, Expression = expr });
                    }
                    else if (acceptOptionalArgs && tok[i].Type == TokenType.Identifier && tok[i + 1].IsBuiltin(":"))
                    {
                        acceptPositionalArgs = false;
                        var posName = tok[i].TokenStr;
                        i += 2;
                        var expr = parseExpression(tok, ref i);
                        attr.Arguments.Add(new CsArgument { StartIndex = argStartIndex, EndIndex = tok[i - 1].EndIndex, ArgumentName = posName, ArgumentExpression = expr });
                    }
                    else if (acceptPositionalArgs)
                    {
                        var expr = parseExpression(tok, ref i);
                        attr.Arguments.Add(new CsArgument { StartIndex = argStartIndex, EndIndex = tok[i - 1].EndIndex, ArgumentExpression = expr });
                    }
                    else
                        throw new ParseException("Identifier '=' <expression>, or ')' expected.", tok[i].StartIndex);
                }
                i++;
                if (tok[i].IsBuiltin("]"))
                {
                    i++;
                    break;
                }
                else if (tok[i].IsBuiltin(","))
                {
                    i++;
                    continue;
                }
                else
                    throw new ParseException("']' or ',' expected. (1)", tok[i].StartIndex);
            }
            return new CsCustomAttributeGroup { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, CustomAttributes = group, Location = loc, NoNewLine = noNewLine };
        }
        private static List<CsGenericParameter> parseGenericTypeParameterList(TokenJar tok, ref int i)
        {
            tok[i].Assert("<");
            var genericTypeParameters = new List<CsGenericParameter>();
            while (true)
            {
                i++;
                var startIndex = tok[i].StartIndex;
                var customAttribs = new List<CsCustomAttributeGroup>();
                while (tok[i].IsBuiltin("["))
                    customAttribs.Add(parseCustomAttributeGroup(tok, ref i, true));

                var variance = VarianceMode.Invariant;
                if (tok[i].IsBuiltin("in") || tok[i].IsBuiltin("out"))
                {
                    variance = tok[i].IsBuiltin("out") ? VarianceMode.Covariant : VarianceMode.Contravariant;
                    i++;
                }

                var name = tok[i].Identifier();
                genericTypeParameters.Add(new CsGenericParameter { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Name = name, CustomAttributes = customAttribs });
                i++;
                if (tok[i].IsBuiltin(","))
                    continue;
                else if (tok[i].IsBuiltin(">"))
                    break;
                throw new ParseException("',' or '>' expected. (1)", tok[i].StartIndex, genericTypeParameters);
            }
            i++;
            return genericTypeParameters;
        }
        private static Dictionary<string, List<CsGenericTypeConstraint>> parseGenericTypeConstraints(TokenJar tok, ref int i)
        {
            var ret = new Dictionary<string, List<CsGenericTypeConstraint>>();
            while (tok[i].IsIdentifier("where"))
            {
                var startIndex = tok[i].StartIndex;
                i++;
                string genericParameter;
                try { genericParameter = tok[i].Identifier(); }
                catch (ParseException e) { throw new ParseException(e.Message, e.Index, ret, e); }
                if (ret.ContainsKey(genericParameter))
                    throw new ParseException("A constraint clause has already been specified for type parameter '{0}'. All of the constraints for a type parameter must be specified in a single where clause.".Fmt(genericParameter), tok[i].StartIndex, ret);
                i++;
                if (!tok[i].IsBuiltin(":"))
                    throw new ParseException("':' expected.", tok[i].StartIndex, ret);

                do
                {
                    i++;
                    if (tok[i].IsBuiltin("new") && tok.IndexExists(i + 2) && tok[i + 1].IsBuiltin("(") && tok[i + 2].IsBuiltin(")"))
                    {
                        i += 3;
                        ret.AddSafe(genericParameter, new CsGenericTypeConstraintNew { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex });
                    }
                    else if (tok[i].IsBuiltin("class"))
                    {
                        ret.AddSafe(genericParameter, new CsGenericTypeConstraintClass { StartIndex = startIndex, EndIndex = tok[i].EndIndex });
                        i++;
                    }
                    else if (tok[i].IsBuiltin("struct"))
                    {
                        ret.AddSafe(genericParameter, new CsGenericTypeConstraintStruct { StartIndex = startIndex, EndIndex = tok[i].EndIndex });
                        i++;
                    }
                    else if (tok[i].Type != TokenType.Identifier)
                        throw new ParseException("Generic type constraint ('new()', 'class', 'struct', or type identifier) expected.", tok[i].StartIndex, ret);
                    else
                    {
                        try
                        {
                            var typeName = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords).Item1;
                            ret.AddSafe(genericParameter, new CsGenericTypeConstraintBaseClass { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, BaseClass = typeName });
                        }
                        catch (ParseException e)
                        {
                            if (e.IncompleteResult is CsTypeName)
                                ret.AddSafe(genericParameter, new CsGenericTypeConstraintBaseClass { StartIndex = tok[i].StartIndex, BaseClass = (CsTypeName) e.IncompleteResult });
                            throw new ParseException(e.Message, e.Index, ret, e);
                        }
                    }
                }
                while (tok[i].IsBuiltin(","));
            }
            return ret;
        }
        private static List<CsParameter> parseParameterList(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            bool square = tok[i].IsBuiltin("[");

            if (!square && !tok[i].IsBuiltin("("))
            {
                if (tryNotToThrow)
                    return null;
                else
                    throw new ParseException("'(' + parameter list + ')' expected.", tok[i].StartIndex);
            }

            List<CsParameter> ret = new List<CsParameter>();
            if (tok[i + 1].IsBuiltin(square ? "]" : ")"))
            {
                i += 2;
                return ret;
            }

            do
            {
                i++;
                try
                {
                    var parsed = parseParameter(tok, ref i, tryNotToThrow);
                    if (parsed == null)
                        return null;
                    ret.Add(parsed);
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsParameter)
                        ret.Add((CsParameter) e.IncompleteResult);
                    throw new ParseException(e.Message, e.Index, ret, e);
                }
            }
            while (tok[i].IsBuiltin(","));

            if (!tok[i].IsBuiltin(square ? "]" : ")"))
                throw new ParseException(square ? "']' or ',' expected. (2)" : "')' or ',' expected.", tok[i].StartIndex, ret);
            i++;

            return ret;
        }
        private static CsParameter parseParameter(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var startIndex = tok[i].StartIndex;
            var customAttribs = new List<CsCustomAttributeGroup>();
            while (tok[i].IsBuiltin("["))
                customAttribs.Add(parseCustomAttributeGroup(tok, ref i, true));

            bool isThis = false, isOut = false, isRef = false, isParams = false;
            if (tok[i].IsBuiltin("this"))
            {
                isThis = true;
                i++;
            }
            if (tok[i].IsBuiltin("out"))
            {
                isOut = true;
                i++;
            }
            if (tok[i].IsBuiltin("ref"))
            {
                isRef = true;
                i++;
            }
            if (tok[i].IsBuiltin("params"))
            {
                isParams = true;
                i++;
            }
            var result = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays, tryNotToThrow);
            if (result == null || (tryNotToThrow && tok[i].Type != TokenType.Identifier))
                return null;
            var name = tok[i].Identifier();
            i++;
            CsExpression defaultValue = null;
            if (tok[i].IsBuiltin("="))
            {
                i++;
                defaultValue = parseExpression(tok, ref i, tryNotToThrow);
                if (defaultValue == null)
                    return null;
            }
            return new CsParameter
            {
                StartIndex = startIndex,
                EndIndex = tok[i - 1].EndIndex,
                Type = result.Item1,
                Name = name,
                IsThis = isThis,
                IsOut = isOut,
                IsRef = isRef,
                IsParams = isParams,
                CustomAttributes = customAttribs,
                DefaultValue = defaultValue
            };
        }
        private static Tuple<CsTypeName, bool> parseTypeName(TokenJar tok, ref int i, typeIdentifierFlags flags, bool tryNotToThrow = false)
        {
            var ty = new CsConcreteTypeName { StartIndex = tok[i].StartIndex };
            if (tok[i].IsIdentifier("global") && tok[i + 1].IsBuiltin("::"))
            {
                ty.HasGlobal = true;
                i += 2;
            }

            var j = i;
            bool onShr = false;
            while (true)
            {
                CsSimpleName partAbstract;
                if (tok[j].Type == TokenType.Builtin && (flags & typeIdentifierFlags.AllowKeywords) == typeIdentifierFlags.AllowKeywords && builtinTypes.Contains(tok[j].TokenStr))
                    partAbstract = new CsSimpleNameBuiltin { StartIndex = tok[j].StartIndex, EndIndex = tok[j].EndIndex, Builtin = tok[j].TokenStr };
                else if (ty.Parts.Count > 0 && (flags & typeIdentifierFlags.Lenient) != 0 && tok[j].Type != TokenType.Identifier)
                {
                    ty.EndIndex = tok[j].EndIndex;
                    return Tuple.Create((CsTypeName) ty, false);
                }
                else if (tok[j].Type != TokenType.Identifier && tryNotToThrow)
                    return null;
                else
                    partAbstract = new CsSimpleNameIdentifier { StartIndex = tok[j].StartIndex, EndIndex = tok[j].EndIndex, Name = tok[j].Identifier("Type expected.") };
                j++;
                ty.Parts.Add(partAbstract);
                i = j;

                if (tok[j].IsBuiltin("<"))
                {
                    if (!(partAbstract is CsSimpleNameIdentifier))
                        throw new ParseException("'{0}' cannot have generic arguments.".Fmt(partAbstract.ToString()), tok[j].StartIndex, ty);
                    var part = (CsSimpleNameIdentifier) partAbstract;
                    part.GenericTypeArguments = new List<CsTypeName>();
                    j++;
                    if ((flags & typeIdentifierFlags.AllowEmptyGenerics) != 0 && (tok[j].IsBuiltin(",") || tok[j].IsBuiltin(">")))
                    {
                        part.GenericTypeArguments.Add(new CsEmptyGenericParameter { StartIndex = tok[j].StartIndex, EndIndex = tok[j].StartIndex });
                        while (tok[j].IsBuiltin(","))
                        {
                            j++;
                            part.GenericTypeArguments.Add(new CsEmptyGenericParameter { StartIndex = tok[j].StartIndex, EndIndex = tok[j].StartIndex });
                        }
                        if (!tok[j].IsBuiltin(">"))
                            throw new ParseException("',' or '>' expected.", tok[j].StartIndex, ty);
                        j++;
                    }
                    else
                    {
                        try
                        {
                            var result = parseTypeName(tok, ref j, (flags & typeIdentifierFlags.Lenient) | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowShr);
                            part.GenericTypeArguments.Add(result.Item1);
                            while (tok[j].IsBuiltin(","))
                            {
                                j++;
                                result = parseTypeName(tok, ref j, (flags & typeIdentifierFlags.Lenient) | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowShr);
                                part.GenericTypeArguments.Add(result.Item1);
                            }
                            if (result.Item2)
                                // We're definitely on a ">>" token
                                j++;
                            else
                            {
                                if (tok[j].IsBuiltin(">"))
                                    j++;
                                else if (tok[j].IsBuiltin(">>") && (flags & typeIdentifierFlags.AllowShr) != 0)
                                    onShr = true;
                                else
                                    throw new ParseException("',' or '>' expected. (3)", tok[j].StartIndex, ty);
                            }
                        }
                        catch (ParseException)
                        {
                            if ((flags & typeIdentifierFlags.Lenient) != 0)
                            {
                                part.GenericTypeArguments = null;
                                return Tuple.Create((CsTypeName) ty, false);
                            }
                            throw;
                        }
                    }
                }
                i = j;
                if (!onShr && (flags & typeIdentifierFlags.DontAllowDot) == 0 && tok[j].IsBuiltin("."))
                    j++;
                else
                    break;
            }

            ty.EndIndex = tok[j - 1].EndIndex + (onShr ? 1 : 0);
            CsTypeName ret = ty;

            try
            {
                if ((flags & typeIdentifierFlags.AllowNullablesAndPointers) != 0 && !onShr)
                {
                    while (tok[j].IsBuiltin("*"))
                    {
                        ret = new CsPointerTypeName { StartIndex = ret.StartIndex, EndIndex = tok[j].EndIndex, InnerType = ret };
                        j++;
                    }
                    if (tok[j].IsBuiltin("?"))
                    {
                        ret = new CsNullableTypeName { StartIndex = ret.StartIndex, EndIndex = tok[j].EndIndex, InnerType = ret };
                        j++;
                    }
                }
                i = j;
                if ((flags & typeIdentifierFlags.AllowArrays) != 0 && !onShr)
                {
                    var arrayRanks = new List<int>();
                    while (tok[j].IsBuiltin("[") && (tok[j + 1].IsBuiltin("]") || tok[j + 1].IsBuiltin(",")))
                    {
                        j++;
                        int num = 1;
                        while (tok[j].IsBuiltin(","))
                        {
                            num++;
                            j++;
                        }
                        if (!tok[j].IsBuiltin("]"))
                        {
                            if ((flags & typeIdentifierFlags.Lenient) == 0)
                                throw new ParseException("',' or ']' expected.", tok[j].StartIndex, ret);
                            if (arrayRanks.Count > 0)
                                ret = new CsArrayTypeName { StartIndex = ret.StartIndex, EndIndex = tok[j - 1].EndIndex, ArrayRanks = arrayRanks, InnerType = ret };
                            return Tuple.Create(ret, false);
                        }
                        j++;
                        i = j;
                        arrayRanks.Add(num);
                    }
                    if (arrayRanks.Count > 0)
                        ret = new CsArrayTypeName { StartIndex = ret.StartIndex, EndIndex = tok[j - 1].EndIndex, ArrayRanks = arrayRanks, InnerType = ret };
                }
            }
            catch (ParseException e)
            {
                if ((flags & typeIdentifierFlags.Lenient) != 0)
                    return Tuple.Create(ret, false);
                if (e.IncompleteResult is CsTypeName)
                    throw;
                throw new ParseException(e.Message, e.Index, ret, e);
            }
            i = j;
            return Tuple.Create(ret, onShr);
        }
        private static void parseModifiers(CsMember mem, TokenJar tok, ref int i)
        {
            while (true)
            {
                // All members
                if (tok[i].IsBuiltin("public"))
                    mem.IsPublic = true;
                else if (tok[i].IsBuiltin("protected"))
                    mem.IsProtected = true;
                else if (tok[i].IsBuiltin("private"))
                    mem.IsPrivate = true;
                else if (tok[i].IsBuiltin("internal"))
                    mem.IsInternal = true;
                else if (tok[i].IsBuiltin("new"))
                    mem.IsNew = true;
                else if (tok[i].IsBuiltin("unsafe"))
                    mem.IsUnsafe = true;

                // CsMemberLevel2 (CsProperty, CsMethod)
                else if (tok[i].IsBuiltin("static") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsStatic = true;
                else if (tok[i].IsBuiltin("abstract") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsAbstract = true;
                else if (tok[i].IsBuiltin("sealed") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsSealed = true;
                else if (tok[i].IsBuiltin("virtual") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsVirtual = true;
                else if (tok[i].IsBuiltin("override") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsOverride = true;
                else if (tok[i].IsBuiltin("extern") && mem is CsMemberLevel2)
                    ((CsMemberLevel2) mem).IsExtern = true;
                else if (tok[i].IsIdentifier("partial") && mem is CsMethod)
                    ((CsMethod) mem).IsPartial = true;

                // CsMultiMember (CsEvent, CsField)
                else if (tok[i].IsBuiltin("static") && mem is CsMultiMember)
                    ((CsMultiMember) mem).IsStatic = true;
                else if (tok[i].IsBuiltin("abstract") && mem is CsEvent)
                    ((CsEvent) mem).IsAbstract = true;
                else if (tok[i].IsBuiltin("sealed") && mem is CsEvent)
                    ((CsEvent) mem).IsSealed = true;
                else if (tok[i].IsBuiltin("virtual") && mem is CsEvent)
                    ((CsEvent) mem).IsVirtual = true;
                else if (tok[i].IsBuiltin("override") && mem is CsEvent)
                    ((CsEvent) mem).IsOverride = true;
                else if (tok[i].IsBuiltin("readonly") && mem is CsField)
                    ((CsField) mem).IsReadonly = true;
                else if (tok[i].IsBuiltin("const") && mem is CsField)
                    ((CsField) mem).IsConst = true;
                else if (tok[i].IsBuiltin("volatile") && mem is CsField)
                    ((CsField) mem).IsVolatile = true;

                // CsTypeLevel2 (class, struct, interface)
                else if (tok[i].IsIdentifier("partial") && mem is CsTypeLevel2)
                    ((CsTypeLevel2) mem).IsPartial = true;
                else if (tok[i].IsBuiltin("static") && mem is CsClass)
                    ((CsClass) mem).IsStatic = true;
                else if (tok[i].IsBuiltin("sealed") && mem is CsClass)
                    ((CsClass) mem).IsSealed = true;
                else if (tok[i].IsBuiltin("abstract") && mem is CsClass)
                    ((CsClass) mem).IsAbstract = true;

                // Other special
                else if (tok[i].IsBuiltin("static") && mem is CsOperatorOverload)
                    ((CsOperatorOverload) mem).IsStatic = true;
                else if (tok[i].IsBuiltin("static") && mem is CsConstructor)
                    ((CsConstructor) mem).IsStatic = true;

                else
                    break;
                i++;
            }
        }
        #endregion

        #region Members (incl. types)
        private static object parseMemberDeclaration(TokenJar tok, ref int i, bool returnAssemblyAndModuleCustomAttributes)
        {
            var startIndex = tok[i].StartIndex;
            var customAttribs = new List<CsCustomAttributeGroup>();
            while (tok[i].IsBuiltin("["))
            {
                var k = i;
                var attr = parseCustomAttributeGroup(tok, ref i, false);
                if (returnAssemblyAndModuleCustomAttributes && (attr.Location == CustomAttributeLocation.Assembly || attr.Location == CustomAttributeLocation.Module))
                {
                    if (customAttribs.Count > 0)
                        throw new ParseException(@"Assembly or module custom attribute not allowed after other custom attributes.", tok[k].StartIndex);
                    return attr;
                }
                customAttribs.Add(attr);
            }

            if (tok[i].IsBuiltin("using"))
            {
                if (customAttribs.Count > 0)
                    throw new ParseException("'using' directives cannot have custom attributes.", startIndex);
                return parseUsingDeclaration(tok, ref i);
            }
            if (tok[i].IsBuiltin("namespace"))
            {
                if (customAttribs.Count > 0)
                    throw new ParseException("Namespaces cannot have custom attributes.", startIndex);
                return parseNamespace(tok, ref i);
            }

            var j = i;
            var modifiers = new[] { "abstract", "const", "extern", "internal", "new", "override", "partial", "private", "protected", "public", "readonly", "sealed", "static", "unsafe", "virtual", "volatile" };
            while ((tok[j].Type == TokenType.Builtin || tok[j].Type == TokenType.Identifier) && modifiers.Contains(tok[j].TokenStr))
                j++;
            if (tok[j].IsBuiltin("class") || tok[j].IsBuiltin("struct") || tok[j].IsBuiltin("interface"))
            {
                return parseClassStructOrInterfaceDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].IsBuiltin("delegate"))
            {
                return parseDelegateDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].IsBuiltin("enum"))
            {
                return parseEnumDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].Type == TokenType.Identifier && tok[j + 1].IsBuiltin("("))
            {
                return parseConstructorDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].IsBuiltin("~") && tok[j + 1].Type == TokenType.Identifier && tok[j + 2].IsBuiltin("("))
            {
                return parseDestructorDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].IsBuiltin("implicit") || tok[j].IsBuiltin("explicit"))
            {
                return parseCastOperatorOverloadDeclaration(tok, ref i, customAttribs, j);
            }
            else if (tok[j].IsBuiltin("operator"))
            {
                throw new ParseException("You must specify either 'implicit operator', 'explicit operator', or a return type before the 'operator' keyword.", tok[j].StartIndex);
            }
            else
            {
                var afterModifiers = j;

                // It could be a field, a method, an operator overload, a property or an event
                var isEvent = false;
                if (tok[j].IsBuiltin("event"))
                {
                    isEvent = true;
                    j++;
                }

                var prevIndex = tok[j].StartIndex;
                CsTypeName type;
                try
                {
                    type = parseTypeName(tok, ref j, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays).Item1;
                }
                catch (ParseException e)
                {
                    if (e.Index == prevIndex)
                        throw new ParseException("'class', 'struct', 'interface', 'enum', 'delegate', 'event', constructor or destructor declaration, 'implicit operator', 'explicit operator', or type expected.", tok[j].StartIndex);
                    throw;
                }

                if (tok[j].IsBuiltin("this") && !isEvent)
                {
                    // Indexed property
                    var prop = new CsIndexedProperty { StartIndex = startIndex, Type = type, CustomAttributes = customAttribs };
                    parseModifiers(prop, tok, ref i);
                    if (i != afterModifiers)
                        throw new ParseException("The modifier '{0}' is not valid for indexed properties.".Fmt(tok[i].TokenStr), startIndex);
                    i = j + 1;
                    if (!tok[i].IsBuiltin("["))
                        throw new ParseException("'[' expected.", tok[j].StartIndex);
                    prop.Parameters = parseParameterList(tok, ref i);
                    parsePropertyBody(prop, tok, ref i);
                    prop.EndIndex = tok[i - 1].EndIndex;
                    return prop;
                }

                if (tok[j].IsBuiltin("operator") && !isEvent)
                    return parseOperatorOverloadDeclaration(tok, ref i, customAttribs, j, afterModifiers, type);

                string name = tok[j].Identifier(isEvent ? "Identifier expected." : "Identifier, 'this' or 'operator' expected.");
                j++;

                if (tok[j].IsBuiltin("{"))
                {
                    if (isEvent)
                    {
                        var evnt = new CsEvent
                        {
                            StartIndex = startIndex,
                            Type = type,
                            NamesAndInitializers = new List<CsNameAndExpression> { new CsNameAndExpression { StartIndex = tok[afterModifiers].StartIndex, EndIndex = tok[afterModifiers].EndIndex, Name = name } },
                            CustomAttributes = customAttribs
                        };
                        parseModifiers(evnt, tok, ref i);
                        if (i != afterModifiers)
                            throw new ParseException("The modifier '{0}' is not valid for events.".Fmt(tok[i].TokenStr), startIndex);
                        i = j;
                        parseEventBody(evnt, tok, ref i);
                        evnt.EndIndex = tok[i - 1].EndIndex;
                        return evnt;
                    }
                    else
                    {
                        // It’s a property
                        var prop = new CsProperty { StartIndex = startIndex, Type = type, Name = name, CustomAttributes = customAttribs };
                        parseModifiers(prop, tok, ref i);
                        if (i != afterModifiers)
                            throw new ParseException("The modifier '{0}' is not valid for properties.".Fmt(tok[i].TokenStr), startIndex);
                        i = j;
                        parsePropertyBody(prop, tok, ref i);
                        prop.EndIndex = tok[i - 1].EndIndex;
                        return prop;
                    }
                }
                else if (tok[j].IsBuiltin("=") || tok[j].IsBuiltin(";") || tok[j].IsBuiltin(","))
                {
                    return parseFieldOrEventDeclaration(isEvent, tok, ref i, customAttribs, afterModifiers, j, type, null, name);
                }
                else if (tok[j].IsBuiltin("(") || tok[j].IsBuiltin("<") || tok[j].IsBuiltin("."))
                {
                    // Could still be a method, event, property or indexer
                    return parseMemberDeclarationComplexCase(tok, ref i, customAttribs, j, afterModifiers, isEvent, type, name, startIndex);
                }
                else
                {
                    if (isEvent)
                        throw new ParseException("'=', ',', ';' or '{' expected.", tok[j].StartIndex);
                    else
                        throw new ParseException("For a field, '=', ',' or ';' expected. For a method, '(' or '<' expected. For a property, '{' expected.", tok[j].StartIndex);
                }
            }
        }
        private static CsMember parseMemberDeclarationComplexCase(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j, int afterModifiers, bool isEvent, CsTypeName type, string name, int startIndex)
        {
            // If it's "(", it's a method.
            // If it's "<", it may be a generic method, or it may be a property, event or (possibly generic) method that explicitly implements an interface member from a generic interface.
            // If it's ".", it is a property, event or (possibly generic) method that explicitly implements an interface member.

            List<CsGenericParameter> genericTypeParameters = null;
            CsConcreteTypeName implementsFrom = null;

            if (tok[j].IsBuiltin("<") || tok[j].IsBuiltin("."))
            {
                // In the case that this might be an explicit interface implementation, try to parse the interface type.
                j--;
                var ty = (CsConcreteTypeName) parseTypeName(tok, ref j, typeIdentifierFlags.Lenient).Item1;

                if (tok[j].IsBuiltin(".") && tok[j + 1].IsBuiltin("this"))
                {
                    // It's an explicit implementation for an indexed property
                    var prop = new CsIndexedProperty { StartIndex = startIndex, Type = type, CustomAttributes = customAttribs, ImplementsFrom = ty };
                    parseModifiers(prop, tok, ref i);
                    if (i != afterModifiers)
                        throw new ParseException("The modifier '{0}' is not valid for indexed properties.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
                    i = j + 2;
                    if (!tok[i].IsBuiltin("["))
                        throw new ParseException("'[' expected.", tok[j].StartIndex);
                    prop.Parameters = parseParameterList(tok, ref i);
                    parsePropertyBody(prop, tok, ref i);
                    prop.EndIndex = tok[i - 1].EndIndex;
                    return prop;
                }

                // Apart from 'this', no builtins (keywords) are allowed
                if (!(ty.Parts[ty.Parts.Count - 1] is CsSimpleNameIdentifier))
                    throw new ParseException("Identifier expected instead of '{0}'.".Fmt(ty.Parts[ty.Parts.Count - 1]), tok[j - 1].StartIndex);

                // The following cases are still possible:
                // ① a generic method declaration without custom attributes, e.g.
                //              void MyMethod<T>() { }
                //              In this case, ‘ty’ contains ‘MyMethod<T>’ as if T were a generic type argument instead of a generic type parameter declaration, and j points at ‘(’.
                // ② a generic method declaration with custom attributes, e.g.
                //              void MyMethod<[Foo] T>() { }
                //              In this case, ‘ty’ contains just ‘MyMethod’ and j points at ‘<’.
                // ③ an explicit interface implementation, e.g.
                //              a. void IMyInterface.MyMethod() { }                         // non-generic method
                //              b. void IMyInterface.MyMethod<T>() { }                  // generic method without custom attributes
                //              c. void IMyInterface.MyMethod<[Foo] T>() { }        // generic method with custom attributes
                //              d. int IMyInterface.MyProperty { get { ... } }                                                         // property
                //              e. event EventHandler IMyInterface.MyEvent { add { ... } remove { ... } }       // event
                //              The interface could be anything, including namespaces, nested types and generic type arguments 
                //              (e.g. MyNamespace.IMyInterface<List<int>.Enumerator>).

                // In all cases, ‘ty’ contains not just the interface name, but also *at least* the member name and *at most* the member name plus its generic parameters.
                // Therefore, either way, remove it from ‘ty’ to get the genuine interface name.
                var lastPart = (CsSimpleNameIdentifier) ty.Parts[ty.Parts.Count - 1];
                ty.Parts.RemoveAt(ty.Parts.Count - 1);

                if (lastPart.GenericTypeArguments != null)
                {
                    // If the last part has generic type “arguments”, then we are in case ① or ③b.
                    // In that case, we need to convert those “arguments” into generic parameters.
                    genericTypeParameters = new List<CsGenericParameter>();
                    foreach (var g in lastPart.GenericTypeArguments)
                    {
                        var single = g.GetSingleIdentifier();
                        if (single == null)
                            throw new ParseException("Invalid generic type parameter declaration.", tok[j].StartIndex);
                        genericTypeParameters.Add(new CsGenericParameter { StartIndex = g.StartIndex, EndIndex = g.EndIndex, Name = single });
                    }
                }
                else if (tok[j].IsBuiltin("<"))
                {
                    // The last part didn’t have generic type arguments, and we are on a “<” token, so we are in case ② or ③c.
                    genericTypeParameters = parseGenericTypeParameterList(tok, ref j);
                }

                // The “name” that was passed in was the first token after the return type, so it would have been
                // set to the first identifier in the interface name, which is of course wrong. Fix it now that we know the real name.
                name = lastPart.Name;

                // If “ty” has any parts left, then that’s the interface name, otherwise this wasn’t an explicit interface declaration in the first place.
                implementsFrom = ty.Parts.Count == 0 ? null : ty;

                if (tok[j].IsBuiltin("{"))
                {
                    // Case ③d and ③ e: an explicitly-implemented property or an event.
                    // It must be an explicit implementation because normal properties/events are already handled by parseMemberDeclaration().
                    // Explicitly-implemented events must have a body with add/remove methods; they can’t be events with the fields syntax, e.g.
                    //      event EventHandler IMyInterface.MyEvent = null;   // ✗ not allowed
                    if (genericTypeParameters != null)
                        throw new ParseException("Properties and events cannot be generic.", tok[j].StartIndex);
                    if (isEvent)
                    {
                        var nameExpr = new CsNameAndExpression
                        {
                            // We are assuming that after the modifiers comes the “event” keyword and after that, the name
                            StartIndex = tok[afterModifiers + 1].StartIndex,
                            EndIndex = tok[afterModifiers + 1].EndIndex,
                            Name = name
                        };
                        var evnt = new CsEvent
                        {
                            StartIndex = startIndex,
                            Type = type,
                            NamesAndInitializers = new List<CsNameAndExpression> { nameExpr },
                            CustomAttributes = customAttribs,
                            ImplementsFrom = implementsFrom
                        };
                        parseModifiers(evnt, tok, ref i);
                        if (i != afterModifiers)
                            throw new ParseException("The modifier '{0}' is not valid for events.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
                        i = j;
                        parseEventBody(evnt, tok, ref i);
                        evnt.EndIndex = tok[i - 1].EndIndex;
                        return evnt;
                    }
                    else
                    {
                        var prop = new CsProperty { StartIndex = startIndex, Type = type, Name = name, CustomAttributes = customAttribs, ImplementsFrom = implementsFrom };
                        parseModifiers(prop, tok, ref i);
                        if (i != afterModifiers)
                            throw new ParseException("The modifier '{0}' is not valid for properties.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
                        i = j;
                        parsePropertyBody(prop, tok, ref i);
                        prop.EndIndex = tok[i - 1].EndIndex;
                        return prop;
                    }
                }
                else if (isEvent && (tok[j].IsBuiltin(";") || tok[j].IsBuiltin("=")))
                {
                    // It’s an event without a body — not allowed for explicit interface implementations
                    throw new ParseException("Events that explicitly implement an interface event must have add/remove methods.", tok[i].StartIndex);
                }
            }

            // We’ve taken care of explicitly-implemented properties, indexers and events. The only case left is that it must be a method.
            // implementsFrom and genericTypeParameters are fully populated.
            CsMethod meth = new CsMethod
            {
                StartIndex = startIndex,
                Type = type,
                Name = name,
                CustomAttributes = customAttribs,
                ImplementsFrom = implementsFrom,
                GenericTypeParameters = genericTypeParameters
            };
            parseModifiers(meth, tok, ref i);
            if (i != afterModifiers)
                throw new ParseException("The modifier '{0}' is not valid for methods.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex, meth);
            try { meth.Parameters = parseParameterList(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is List<CsParameter>)
                    meth.Parameters = (List<CsParameter>) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, meth, e);
            }
            if (tok[i].IsIdentifier("where"))
            {
                try { meth.GenericTypeConstraints = parseGenericTypeConstraints(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is Dictionary<string, List<CsGenericTypeConstraint>>)
                        meth.GenericTypeConstraints = (Dictionary<string, List<CsGenericTypeConstraint>>) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, meth, e);
                }
            }
            if (tok[i].IsBuiltin(";"))
                i++;
            else if (tok[i].IsBuiltin("{"))
            {
                try { meth.MethodBody = parseBlock(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsBlock)
                        meth.MethodBody = (CsBlock) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, meth, e);
                }
            }
            else
                throw new ParseException(@"';', '{' or 'where' expected.", tok[i].StartIndex, meth);
            meth.EndIndex = tok[i - 1].EndIndex;
            return meth;
        }
        private static CsMultiMember parseFieldOrEventDeclaration(bool isEvent, TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int afterModifiers, int afterName, CsTypeName type, CsTypeName implementsFrom, string name)
        {
            var startIndex = tok[i].StartIndex;
            CsMultiMember ret;
            if (isEvent)
                ret = new CsEvent { StartIndex = startIndex, Type = type, CustomAttributes = customAttribs, ImplementsFrom = implementsFrom };
            else
                ret = new CsField { StartIndex = startIndex, Type = type, CustomAttributes = customAttribs };
            parseModifiers(ret, tok, ref i);
            if (i != afterModifiers)
                throw new ParseException("The modifier '{0}' is not valid for {1}.".Fmt(tok[i].TokenStr, isEvent ? "events" : "fields"), tok[i].StartIndex);
            i = afterName;
            CsExpression initializer = null;
            try
            {
                if (tok[i].IsBuiltin("="))
                {
                    i++;
                    initializer = parseExpression(tok, ref i);
                }
                ret.NamesAndInitializers.Add(new CsNameAndExpression { StartIndex = tok[afterName - 1].StartIndex, EndIndex = tok[i - 1].EndIndex, Name = name, Expression = initializer });
                while (tok[i].IsBuiltin(","))
                {
                    i++;
                    var nameStartIndex = tok[i].StartIndex;
                    name = tok[i].Identifier();
                    initializer = null;
                    i++;
                    if (tok[i].IsBuiltin("="))
                    {
                        i++;
                        initializer = parseExpression(tok, ref i);
                    }
                    ret.NamesAndInitializers.Add(new CsNameAndExpression { StartIndex = nameStartIndex, EndIndex = tok[i - 1].EndIndex, Name = name, Expression = initializer });
                }
            }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsExpression)
                {
                    ret.NamesAndInitializers.Add(new CsNameAndExpression { StartIndex = tok[i].StartIndex, Name = name, Expression = (CsExpression) e.IncompleteResult });
                    throw new ParseException(e.Message, e.Index, ret, e);
                }
                throw;
            }
            if (!tok[i].IsBuiltin(";"))
                throw new ParseException("'=', ',' or ';' expected.", tok[i].StartIndex, ret);
            ret.EndIndex = tok[i].EndIndex;
            i++;
            return ret;
        }

        private static CsOperatorOverload parseOperatorOverloadDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int atOperator, int afterModifiers, CsTypeName type)
        {
            var j = atOperator + 1;
            var overloadableOperators = new[] { "+", "-", "!", "~", "++", "--", "true", "false", "*", "/", "%", "&", "|", "^", "<<", ">>", "==", "!=", "<", ">", "<=", ">=" };
            if (tok[j].Type != TokenType.Builtin || !overloadableOperators.Contains(tok[j].TokenStr))
                throw new ParseException("Overloadable operator ({0}) expected.".Fmt(overloadableOperators.Select(o => "'" + o + "'").JoinString(", ")), tok[j].StartIndex);
            string opStr = tok[j].TokenStr;
            j++;
            var parameters = parseParameterList(tok, ref j);
            CsOperatorOverload op;
            switch (parameters.Count)
            {
                case 1:
                    UnaryOperator unop;
                    switch (opStr)
                    {
                        case "+": unop = UnaryOperator.Plus; break;
                        case "-": unop = UnaryOperator.Minus; break;
                        case "!": unop = UnaryOperator.Not; break;
                        case "~": unop = UnaryOperator.Neg; break;
                        case "++": unop = UnaryOperator.PrefixInc; break;
                        case "--": unop = UnaryOperator.PrefixDec; break;
                        case "true": unop = UnaryOperator.True; break;
                        case "false": unop = UnaryOperator.False; break;
                        default: throw new ParseException("Binary operator must have two parameters. Overloadable unary operators are '+', '-', '!', '~', '++', '--', 'true' and 'false'.", tok[j].StartIndex);
                    }
                    op = new CsUnaryOperatorOverload { StartIndex = tok[i].StartIndex, CustomAttributes = customAttribs, Parameter = parameters[0], ReturnType = type, Operator = unop };
                    break;

                case 2:
                    BinaryOperator binop;
                    switch (opStr)
                    {
                        case "+": binop = BinaryOperator.Plus; break;
                        case "-": binop = BinaryOperator.Minus; break;
                        case "*": binop = BinaryOperator.Times; break;
                        case "/": binop = BinaryOperator.Div; break;
                        case "%": binop = BinaryOperator.Mod; break;
                        case "&": binop = BinaryOperator.And; break;
                        case "|": binop = BinaryOperator.Or; break;
                        case "^": binop = BinaryOperator.Xor; break;
                        case "<<": binop = BinaryOperator.Shl; break;
                        case ">>": binop = BinaryOperator.Shr; break;
                        case "==": binop = BinaryOperator.Eq; break;
                        case "!=": binop = BinaryOperator.NotEq; break;
                        case "<": binop = BinaryOperator.Less; break;
                        case ">": binop = BinaryOperator.Greater; break;
                        case "<=": binop = BinaryOperator.LessEq; break;
                        case ">=": binop = BinaryOperator.GreaterEq; break;
                        default: throw new ParseException("Unary operator must have only one parameter. Overloadable binary operators are '+', '-', '*', '/', '%', '&', '|', '^', '<<', '>>', '==', '!=', '<', '>', '<=', and '>='.", tok[j].StartIndex);
                    }
                    op = new CsBinaryOperatorOverload { StartIndex = tok[i].StartIndex, CustomAttributes = customAttribs, Parameter = parameters[0], SecondParameter = parameters[1], ReturnType = type, Operator = binop };
                    break;

                default:
                    throw new ParseException("Overloadable operators must have exactly one or two parameters. Use one parameter for unary operators, two for binary operators.", tok[j].StartIndex);
            }
            parseModifiers(op, tok, ref i);
            if (i != afterModifiers)
                throw new ParseException("The modifier '{0}' is not valid for operator overloads.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j;
            try { op.MethodBody = parseBlock(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsBlock)
                    op.MethodBody = (CsBlock) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, op, e);
            }
            return op;
        }
        private static CsCastOperatorOverload parseCastOperatorOverloadDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            var op = new CsCastOperatorOverload { StartIndex = tok[i].StartIndex, CastType = tok[j].IsBuiltin("implicit") ? CastOperatorType.Implicit : CastOperatorType.Explicit, CustomAttributes = customAttribs };
            parseModifiers(op, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for {1} operator declarations.".Fmt(tok[i].TokenStr, tok[j].TokenStr), tok[i].StartIndex);
            i = j + 1;
            if (!tok[i].IsBuiltin("operator"))
                throw new ParseException("'operator' expected.", tok[i].StartIndex);
            i++;
            op.ReturnType = parseTypeName(tok, ref i, typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers).Item1;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            var parameters = parseParameterList(tok, ref i);
            if (parameters.Count != 1)
                throw new ParseException("Implicit/explicit operators must have exactly one parameter.", tok[i].StartIndex, op);
            op.Parameter = parameters[0];
            try { op.MethodBody = parseBlock(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsBlock)
                    op.MethodBody = (CsBlock) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, op, e);
            }
            return op;
        }
        private static CsDestructor parseDestructorDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            CsDestructor des = new CsDestructor { StartIndex = tok[i].StartIndex, Name = tok[j + 1].TokenStr, CustomAttributes = customAttribs };
            parseModifiers(des, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for destructors.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j + 3;
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException(@"Destructors cannot have any parameters.", tok[i].StartIndex);
            i++;
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected.", tok[i].StartIndex);
            try { des.MethodBody = parseBlock(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsBlock)
                {
                    des.MethodBody = (CsBlock) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, des, e);
                }
                throw;
            }
            return des;
        }
        private static CsConstructor parseConstructorDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            CsConstructor con = new CsConstructor { StartIndex = tok[i].StartIndex, Name = tok[j].TokenStr, CustomAttributes = customAttribs };
            parseModifiers(con, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for constructors.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j + 1;
            con.Parameters = parseParameterList(tok, ref i);
            bool canHaveColon = true;
            if (tok[i].IsBuiltin(":"))
            {
                canHaveColon = false;
                i++;
                if (tok[i].IsBuiltin("this"))
                    con.CallType = ConstructorCallType.This;
                else if (tok[i].IsBuiltin("base"))
                    con.CallType = ConstructorCallType.Base;
                else
                    throw new ParseException("'this' or 'base' expected.", tok[i].StartIndex);
                i++;
                if (!tok[i].IsBuiltin("("))
                    throw new ParseException("'(' expected.", tok[i].StartIndex);
                con.MethodBody = new CsBlock { StartIndex = tok[i].StartIndex };  // temporary; just so we can throw a valid constructor as an incomplete result
                try
                {
                    bool dummy;
                    con.CallArguments = parseArgumentList(tok, ref i, out dummy);
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is List<CsArgument>)
                        con.CallArguments = (List<CsArgument>) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, con, e);
                }
            }
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException(canHaveColon ? "':' or '{' expected." : "'{' expected.", tok[i].StartIndex);
            try { con.MethodBody = parseBlock(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsBlock)
                {
                    con.MethodBody = (CsBlock) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, con, e);
                }
                throw;
            }
            con.EndIndex = tok[i - 1].EndIndex;
            return con;
        }
        private static void parsePropertyBody(CsProperty prop, TokenJar tok, ref int i)
        {
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException(@"'{' expected.", tok[i].StartIndex);
            i++;

            while (!tok[i].IsBuiltin("}"))
            {
                var startIndex = tok[i].StartIndex;
                var cAttribs = new List<CsCustomAttributeGroup>();
                while (tok[i].IsBuiltin("["))
                    cAttribs.Add(parseCustomAttributeGroup(tok, ref i, false));
                var m = new CsSimpleMethod { StartIndex = startIndex, CustomAttributes = cAttribs };
                parseModifiers(m, tok, ref i);
                if (!tok[i].IsIdentifier("get") && !tok[i].IsIdentifier("set"))
                    throw new ParseException("'get' or 'set' expected.", tok[i].StartIndex, prop);
                m.Type = tok[i].TokenStr == "get" ? MethodType.Get : MethodType.Set;
                if (prop.Methods.Any(me => me.Type == m.Type))
                    throw new ParseException("A '{0}' method has already been defined for this property.".Fmt(m.Type), tok[i].StartIndex, prop);
                prop.Methods.Add(m);
                i++;
                if (tok[i].IsBuiltin("{"))
                {
                    try { m.Body = parseBlock(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsBlock)
                            m.Body = (CsBlock) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, prop, e);
                    }
                }
                else if (tok[i].IsBuiltin(";"))
                    i++;
                else
                    throw new ParseException("'{' or ';' expected.", tok[i].StartIndex, prop);
                m.EndIndex = tok[i - 1].EndIndex;
            }
            i++;
        }
        private static void parseEventBody(CsEvent ev, TokenJar tok, ref int i)
        {
            tok[i].Assert("{");
            i++;
            ev.Methods = new List<CsSimpleMethod>();

            while (!tok[i].IsBuiltin("}"))
            {
                var startIndex = tok[i].StartIndex;
                var cAttribs = new List<CsCustomAttributeGroup>();
                while (tok[i].IsBuiltin("["))
                    cAttribs.Add(parseCustomAttributeGroup(tok, ref i, false));
                var m = new CsSimpleMethod { StartIndex = startIndex, CustomAttributes = cAttribs };
                parseModifiers(m, tok, ref i);
                if (!tok[i].IsIdentifier("add") && !tok[i].IsIdentifier("remove"))
                    throw new ParseException("'add' or 'remove' expected.", tok[i].StartIndex, ev);
                m.Type = tok[i].TokenStr == "add" ? MethodType.Add : MethodType.Remove;
                if (ev.Methods.Any(me => me.Type == m.Type))
                {
                    if (m.Type == MethodType.Add)
                        throw new ParseException("An 'add' method has already been defined for this event.", tok[i].StartIndex, ev);
                    else
                        throw new ParseException("A 'remove' method has already been defined for this event.", tok[i].StartIndex, ev);
                }
                ev.Methods.Add(m);
                i++;
                if (tok[i].IsBuiltin("{"))
                {
                    try { m.Body = parseBlock(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsBlock)
                            m.Body = (CsBlock) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, ev, e);
                    }
                }
                else if (tok[i].IsBuiltin(";"))
                    i++;
                else
                    throw new ParseException("'{' or ';' expected.", tok[i].StartIndex, ev);
                m.EndIndex = tok[i - 1].EndIndex;
            }
            i++;
        }
        private static CsEnum parseEnumDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            var en = new CsEnum { StartIndex = tok[i].StartIndex, CustomAttributes = customAttribs };
            parseModifiers(en, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for enums.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j + 1;
            en.Name = tok[i].Identifier();
            i++;

            if (tok[i].IsBuiltin(":"))
            {
                i++;
                en.BaseType = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords).Item1;
            }

            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected.", tok[i].StartIndex, en);
            i++;

            while (true)
            {
                var startIndex = tok[i].StartIndex;
                string ident;
                var cAttribs = new List<CsCustomAttributeGroup>();
                try
                {
                    while (tok[i].IsBuiltin("["))
                        cAttribs.Add(parseCustomAttributeGroup(tok, ref i, false));
                    ident = tok[i].Identifier("Enum value expected.");
                }
                catch (ParseException e)
                {
                    throw new ParseException(e.Message, e.Index, en, e);
                }
                var val = new CsEnumValue { StartIndex = startIndex, Name = ident, CustomAttributes = cAttribs };
                en.EnumValues.Add(val);
                i++;

                if (tok[i].IsBuiltin("="))
                {
                    i++;
                    try { val.LiteralValue = parseExpression(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsExpression)
                            val.LiteralValue = (CsExpression) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, en, e);
                    }
                }
                if (!tok[i].IsBuiltin(",") && !tok[i].IsBuiltin("}"))
                    throw new ParseException(val.LiteralValue == null ? "'=', ',' or '}' expected." : "',' or '}' expected.", tok[i].StartIndex, en);
                val.EndIndex = tok[i - 1].EndIndex;
                if (tok[i].IsBuiltin(","))
                    i++;

                if (tok[i].IsBuiltin("}"))
                {
                    en.EndIndex = tok[i].EndIndex;
                    i++;
                    // Skip optional ';' after type declaration
                    if (tok[i].IsBuiltin(";"))
                        i++;
                    return en;
                }
            }
        }
        private static CsDelegate parseDelegateDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            var deleg = new CsDelegate { StartIndex = tok[i].StartIndex, CustomAttributes = customAttribs };
            parseModifiers(deleg, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for delegates.".Fmt(tok[i].TokenStr), tok[i].StartIndex);
            i = j + 1;
            deleg.ReturnType = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays).Item1;
            deleg.Name = tok[i].Identifier();
            i++;
            try
            {
                if (tok[i].IsBuiltin("<"))
                    deleg.GenericTypeParameters = parseGenericTypeParameterList(tok, ref i);

                deleg.Parameters = parseParameterList(tok, ref i);

                if (tok[i].IsIdentifier("where"))
                    deleg.GenericTypeConstraints = parseGenericTypeConstraints(tok, ref i);
            }
            catch (ParseException e)
            {
                if (e.IncompleteResult is List<CsGenericParameter>)
                    deleg.GenericTypeParameters = (List<CsGenericParameter>) e.IncompleteResult;
                else if (e.IncompleteResult is List<CsParameter>)
                    deleg.Parameters = (List<CsParameter>) e.IncompleteResult;
                else if (e.IncompleteResult is Dictionary<string, List<CsGenericTypeConstraint>>)
                    deleg.GenericTypeConstraints = (Dictionary<string, List<CsGenericTypeConstraint>>) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, deleg, e);
            }

            if (!tok[i].IsBuiltin(";"))
                throw new ParseException("';' expected.", tok[i].StartIndex, deleg);
            deleg.EndIndex = tok[i].EndIndex;
            i++;
            return deleg;
        }
        private static CsTypeLevel2 parseClassStructOrInterfaceDeclaration(TokenJar tok, ref int i, List<CsCustomAttributeGroup> customAttribs, int j)
        {
            var type = tok[j].IsBuiltin("class") ? new CsClass() : tok[j].IsBuiltin("struct") ? (CsTypeLevel2) new CsStruct() : new CsInterface();
            type.StartIndex = tok[i].StartIndex;
            type.CustomAttributes = customAttribs;
            parseModifiers(type, tok, ref i);
            if (i != j)
                throw new ParseException("The modifier '{0}' is not valid for {1}.".Fmt(tok[i].TokenStr, tok[j].IsBuiltin("class") ? "classes" : tok[j].IsBuiltin("struct") ? "structs" : "interfaces"), tok[i].StartIndex);
            i = j + 1;

            type.Name = tok[i].Identifier();
            i++;

            if (tok[i].IsBuiltin("<"))
            {
                try { type.GenericTypeParameters = parseGenericTypeParameterList(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is List<CsGenericParameter>)
                        type.GenericTypeParameters = (List<CsGenericParameter>) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, type, e);
                }
            }

            if (tok[i].IsBuiltin(":"))
            {
                i++;
                try
                {
                    type.BaseTypes = new List<CsTypeName>();
                    type.BaseTypes.Add(parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords).Item1);
                    while (tok[i].IsBuiltin(","))
                    {
                        i++;
                        type.BaseTypes.Add(parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords).Item1);
                    }
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsTypeName && type is CsTypeLevel2)
                        type.BaseTypes.Add((CsTypeName) e.IncompleteResult);
                    throw new ParseException(e.Message, e.Index, type, e);
                }
            }

            if (tok[i].IsIdentifier("where"))
            {
                try { type.GenericTypeConstraints = parseGenericTypeConstraints(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is Dictionary<string, List<CsGenericTypeConstraint>>)
                        type.GenericTypeConstraints = (Dictionary<string, List<CsGenericTypeConstraint>>) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, type, e);
                }
            }

            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected.", tok[i].StartIndex, type);
            i++;

            while (!tok[i].IsBuiltin("}"))
            {
                try
                {
                    var obj = parseMemberDeclaration(tok, ref i, false);
                    if (obj is CsMember)
                        type.Members.Add((CsMember) obj);
                    else
                        throw new ParseException("Method, constructor, destructor, property, field, event or nested type expected.", tok[i].StartIndex);
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsMember)
                        type.Members.Add((CsMember) e.IncompleteResult);
                    throw new ParseException(e.Message, e.Index, type, e);
                }
            }
            if (!tok[i].IsBuiltin("}"))
                throw new ParseException("Method, property, field, event, nested type or '}' expected.", tok[i].StartIndex, type);

            type.EndIndex = tok[i].EndIndex;
            i++;

            // Skip optional ';' after type declaration
            if (tok[i].IsBuiltin(";"))
                i++;

            return type;
        }
        #endregion

        #region Statements
        private static CsStatement parseStatement(TokenJar tok, ref int i)
        {
            if (tok[i].IsBuiltin("{"))
                return parseBlock(tok, ref i);

            if (tok[i].IsBuiltin("if"))
                return parseIfStatement(tok, ref i);

            if (tok[i].IsBuiltin("while") || tok[i].IsBuiltin("lock"))
                return parseWhileOrLockStatement(tok, ref i);

            if (tok[i].IsBuiltin("return") || tok[i].IsBuiltin("throw"))
                return parseReturnOrThrowStatement(tok, ref i);

            if (tok[i].IsBuiltin("foreach"))
                return parseForeachStatement(tok, ref i);

            if (tok[i].IsBuiltin("try"))
                return parseTryStatement(tok, ref i);

            if (tok[i].IsBuiltin("using"))
                return parseUsingStatement(tok, ref i);

            if (tok[i].IsBuiltin("switch"))
                return parseSwitchStatement(tok, ref i);

            if (tok[i].IsBuiltin("for"))
                return parseForStatement(tok, ref i);

            if (tok[i].IsBuiltin("do"))
            {
                var dow = new CsDoWhileStatement { StartIndex = tok[i].StartIndex };
                i++;
                dow.Statement = parseStatement(tok, ref i);
                if (!tok[i].IsBuiltin("while") || !tok[i + 1].IsBuiltin("("))
                    throw new ParseException("'while' followed by '(' expected.", tok[i].StartIndex);
                i += 2;
                dow.WhileExpression = parseExpression(tok, ref i);
                if (!tok[i].IsBuiltin(")") || !tok[i + 1].IsBuiltin(";"))
                    throw new ParseException("')' followed by ';' expected.", tok[i].StartIndex, dow);
                i += 2;
                dow.EndIndex = tok[i - 1].EndIndex;
                return dow;
            }

            if (tok[i].IsBuiltin("continue") || tok[i].IsBuiltin("break"))
            {
                var stat = tok[i].IsBuiltin("continue") ? (CsStatement) new CsContinueStatement() : new CsBreakStatement();
                stat.StartIndex = tok[i].StartIndex;
                i++;
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected.", tok[i].StartIndex, stat);
                stat.EndIndex = tok[i].EndIndex;
                i++;
                return stat;
            }

            if (tok[i].IsIdentifier("yield") && tok[i + 1].IsBuiltin("return"))
            {
                i += 2;
                CsExpression expr;
                try { expr = parseExpression(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsYieldReturnStatement { StartIndex = tok[i].StartIndex, Expression = (CsExpression) e.IncompleteResult }, e);
                    throw;
                }
                var yieldreturn = new CsYieldReturnStatement { StartIndex = tok[i].StartIndex, Expression = expr };
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected.", tok[i].StartIndex, yieldreturn);
                yieldreturn.EndIndex = tok[i].EndIndex;
                i++;
                return yieldreturn;
            }

            if (tok[i].IsIdentifier("yield") && tok[i + 1].IsBuiltin("break"))
            {
                var yieldbreak = new CsYieldBreakStatement { StartIndex = tok[i].StartIndex };
                i += 2;
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected.", tok[i].StartIndex, yieldbreak);
                yieldbreak.EndIndex = tok[i].EndIndex;
                i++;
                return yieldbreak;
            }

            if (tok[i].IsBuiltin("goto"))
            {
                i++;
                if (tok[i].IsBuiltin("default"))
                {
                    var stat = new CsGotoDefaultStatement { StartIndex = tok[i].StartIndex };
                    i++;
                    if (!tok[i].IsBuiltin(";"))
                        throw new ParseException("';' expected.", tok[i].StartIndex, stat);
                    stat.EndIndex = tok[i].EndIndex;
                    i++;
                    return stat;
                }
                if (tok[i].IsBuiltin("case"))
                {
                    var stat = new CsGotoCaseStatement { StartIndex = tok[i].StartIndex };
                    i++;
                    try { stat.Expression = parseExpression(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsExpression)
                            stat.Expression = (CsExpression) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, stat, e);
                    }
                    if (!tok[i].IsBuiltin(";"))
                        throw new ParseException("';' expected.", tok[i].StartIndex, stat);
                    stat.EndIndex = tok[i].EndIndex;
                    i++;
                    return stat;
                }
                var name = tok[i].Identifier();
                i++;
                var got = new CsGotoStatement { StartIndex = tok[i].StartIndex, Label = name };
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected.", tok[i].StartIndex, got);
                got.EndIndex = tok[i].EndIndex;
                i++;
                return got;
            }

            if (tok[i].Type == TokenType.Identifier && tok[i + 1].IsBuiltin(":"))
            {
                string name = tok[i].TokenStr;
                i += 2;
                CsStatement inner = parseStatement(tok, ref i);
                if (inner.GotoLabels == null)
                    inner.GotoLabels = new List<string> { name };
                else
                    inner.GotoLabels.Insert(0, name);
                return inner;
            }

            if (tok[i].IsBuiltin(";"))
            {
                i++;
                return new CsEmptyStatement { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex };
            }

            if (tok[i].IsBuiltin("checked") || tok[i].IsBuiltin("unchecked") || tok[i].IsBuiltin("unsafe"))
            {
                CsBlockStatement blockStat = tok[i].IsBuiltin("checked") ? new CsCheckedStatement() : tok[i].IsBuiltin("unchecked") ? (CsBlockStatement) new CsUncheckedStatement() : new CsUnsafeStatement();
                blockStat.StartIndex = tok[i].StartIndex;
                i++;
                blockStat.Block = parseBlock(tok, ref i);
                blockStat.EndIndex = tok[i - 1].EndIndex;
                return blockStat;
            }

            if (tok[i].IsBuiltin("fixed"))
            {
                var fixd = new CsFixedStatement { StartIndex = tok[i].StartIndex };
                i++;
                if (!tok[i].IsBuiltin("("))
                    throw new ParseException("'(' expected.", tok[i].StartIndex);
                i++;
                var k = i;
                fixd.InitializationStatement = parseVariableDeclarationOrExpressionStatement(tok, ref i);
                if (!tok[i].IsBuiltin(")"))
                    throw new ParseException("')' expected.", tok[i].StartIndex);
                i++;

                if (!(fixd.InitializationStatement is CsVariableDeclarationStatement) || !(((CsVariableDeclarationStatement) fixd.InitializationStatement).Type is CsPointerTypeName))
                    throw new ParseException("'fixed' statement requires a variable declaration for a pointer-typed variable.", tok[k].StartIndex);

                try { fixd.Body = parseStatement(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsStatement)
                    {
                        fixd.Body = (CsStatement) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, fixd, e);
                    }
                    throw;
                }
                fixd.EndIndex = tok[i - 1].EndIndex;
                return fixd;
            }

            // Slight hack: parseVariableDeclarationOrExpressionStatement() does not consume the trailing ';' of the statement.
            // This is so that the same method can be used in parsing 'using' and 'for' statements.
            var stmt = parseVariableDeclarationOrExpressionStatement(tok, ref i);
            if (!tok[i].IsBuiltin(";"))
                throw new ParseException("';' expected.", tok[i].StartIndex, stmt);
            stmt.EndIndex = tok[i].EndIndex;
            i++;
            return stmt;
        }
        private static CsBlock parseBlock(TokenJar tok, ref int i)
        {
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected.", tok[i].StartIndex);

            CsBlock block = new CsBlock { StartIndex = tok[i].StartIndex };
            i++;

            while (!tok[i].IsBuiltin("}"))
            {
                try { block.Statements.Add(parseStatement(tok, ref i)); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsStatement)
                        block.Statements.Add((CsStatement) e.IncompleteResult);
                    throw new ParseException(e.Message, e.Index, block, e);
                }
            }

            block.EndIndex = tok[i].EndIndex;
            i++;
            return block;
        }
        private static CsTryStatement parseTryStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("try");
            var tr = new CsTryStatement { StartIndex = tok[i].StartIndex };

            i++;
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected after 'try'.", tok[i].StartIndex);
            tr.Block = parseBlock(tok, ref i);
            if (!tok[i].IsBuiltin("catch") && !tok[i].IsBuiltin("finally"))
                throw new ParseException("'catch' or 'finally' expected after 'try' block.", tok[i].StartIndex);
            while (tok[i].IsBuiltin("catch"))
            {
                var ctch = new CsCatchClause { StartIndex = tok[i].StartIndex };
                i++;
                tr.Catches.Add(ctch);
                if (tok[i].IsBuiltin("("))
                {
                    i++;
                    ctch.Type = parseTypeName(tok, ref i, typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers).Item1;
                    if (tok[i].Type == TokenType.Identifier)
                    {
                        ctch.Name = tok[i].TokenStr;
                        i++;
                    }
                    if (!tok[i].IsBuiltin(")"))
                        throw new ParseException("')' expected.", tok[i].StartIndex);
                    i++;
                }
                if (!tok[i].IsBuiltin("{"))
                    throw new ParseException("'{' expected.", tok[i].StartIndex);
                try { ctch.Block = parseBlock(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsBlock)
                        ctch.Block = (CsBlock) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, tr, e);
                }
                ctch.EndIndex = tok[i - 1].EndIndex;
            }
            if (tok[i].IsBuiltin("finally"))
            {
                i++;
                if (!tok[i].IsBuiltin("{"))
                    throw new ParseException("'{' expected after 'finally'.", tok[i].StartIndex);
                try { tr.Finally = parseBlock(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsBlock)
                        tr.Finally = (CsBlock) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, tr, e);
                }
            }
            tr.EndIndex = tok[i - 1].EndIndex;
            return tr;
        }
        private static CsExpressionBlockStatement parseWhileOrLockStatement(TokenJar tok, ref int i)
        {
            if (!tok[i].IsBuiltin("while") && !tok[i].IsBuiltin("lock"))
                tok[i].Assert("false");

            var stat = tok[i].IsBuiltin("while") ? (CsExpressionBlockStatement) new CsWhileStatement() : new CsLockStatement();
            stat.StartIndex = tok[i].StartIndex;
            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            i++;
            stat.Expression = parseExpression(tok, ref i);
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException("')' expected.", tok[i].StartIndex);
            i++;
            try { stat.Statement = parseStatement(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsStatement)
                {
                    stat.Statement = (CsStatement) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, stat, e);
                }
                throw;
            }
            stat.EndIndex = tok[i - 1].EndIndex;
            return stat;
        }
        private static CsIfStatement parseIfStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("if");

            var ifs = new CsIfStatement { StartIndex = tok[i].StartIndex };
            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            i++;
            ifs.IfExpression = parseExpression(tok, ref i);
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException("')' expected.", tok[i].StartIndex);
            i++;
            try { ifs.Statement = parseStatement(tok, ref i); }
            catch (ParseException e)
            {
                ifs.Statement = (e.IncompleteResult as CsStatement) ?? new CsEmptyStatement { StartIndex = tok[i].StartIndex };
                throw new ParseException(e.Message, e.Index, ifs, e);
            }
            if (tok[i].IsBuiltin("else"))
            {
                i++;
                try { ifs.ElseStatement = parseStatement(tok, ref i); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsStatement)
                        ifs.ElseStatement = (CsStatement) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, ifs, e);
                }
            }
            ifs.EndIndex = tok[i - 1].EndIndex;
            return ifs;
        }
        private static CsUsingStatement parseUsingStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("using");
            var startIndex = tok[i].StartIndex;

            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            i++;
            var usin = new CsUsingStatement { StartIndex = startIndex, InitializationStatement = parseVariableDeclarationOrExpressionStatement(tok, ref i) };
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException("')' expected.", tok[i].StartIndex);
            i++;

            try { usin.Body = parseStatement(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsStatement)
                {
                    usin.Body = (CsStatement) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, usin, e);
                }
                throw;
            }
            usin.EndIndex = tok[i - 1].EndIndex;
            return usin;
        }
        private static CsForStatement parseForStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("for");
            var startIndex = tok[i].StartIndex;

            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            i++;
            var fore = new CsForStatement { StartIndex = startIndex };
            if (!tok[i].IsBuiltin(";"))
            {
                while (true)
                {
                    // CASE 1: for (int i = 0, j = 0; ...)
                    //      In this case, parseVariableDeclarationOrExpressionStatement() will consume the entire variable declaration statement and leave i pointing at the semicolon.
                    // CASE 2: for (int i = 0, int j = 0; ...)
                    //      This is not allowed. In this case, parseVariableDeclarationOrExpressionStatement() will throw the relevant exception.
                    // CASE 3: for (i = 0, j = 0; ...)
                    //      In this case, parseVariableDeclarationOrExpressionStatement() will parse only the “i = 0” part and leave i pointing at the comma.
                    fore.InitializationStatements.Add(parseVariableDeclarationOrExpressionStatement(tok, ref i));
                    if (tok[i].IsBuiltin(","))
                    {
                        i++;
                        continue;
                    }
                    else if (tok[i].IsBuiltin(";"))
                        break;
                    else
                        throw new ParseException("';' or ',' expected.", tok[i].StartIndex);
                }
            }
            i++;

            if (!tok[i].IsBuiltin(";"))
            {
                fore.TerminationCondition = parseExpression(tok, ref i);
                if (!tok[i].IsBuiltin(";"))
                    throw new ParseException("';' expected.", tok[i].StartIndex);
            }
            i++;

            if (!tok[i].IsBuiltin(")"))
            {
                while (true)
                {
                    fore.LoopExpressions.Add(parseExpression(tok, ref i));
                    if (tok[i].IsBuiltin(","))
                    {
                        i++;
                        continue;
                    }
                    else if (tok[i].IsBuiltin(")"))
                        break;
                    else
                        throw new ParseException("',' or ')' expected.", tok[i].StartIndex);
                }
            }
            i++;

            try { fore.Body = parseStatement(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsStatement)
                {
                    fore.Body = (CsStatement) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, fore, e);
                }
                throw;
            }
            fore.EndIndex = tok[i - 1].EndIndex;
            return fore;
        }
        private static CsForeachStatement parseForeachStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("foreach");
            var startIndex = tok[i].StartIndex;

            var fore = new CsForeachStatement { StartIndex = startIndex };
            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex);
            i++;
            if (tok[i].Type == TokenType.Identifier && tok[i + 1].IsBuiltin("in"))
            {
                fore.VariableName = tok[i].TokenStr;
                i += 2;
            }
            else
            {
                fore.VariableType = parseTypeName(tok, ref i, typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers).Item1;
                fore.VariableName = tok[i].Identifier();
                i++;
                if (!tok[i].IsBuiltin("in"))
                    throw new ParseException("'in' expected.", tok[i].StartIndex);
                i++;
            }
            fore.LoopExpression = parseExpression(tok, ref i);
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException("')' expected.", tok[i].StartIndex);
            i++;
            try { fore.Body = parseStatement(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsStatement)
                {
                    fore.Body = (CsStatement) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, fore, e);
                }
                throw;
            }
            fore.EndIndex = tok[i - 1].EndIndex;
            return fore;
        }
        private static CsSwitchStatement parseSwitchStatement(TokenJar tok, ref int i)
        {
            tok[i].Assert("switch");

            var sw = new CsSwitchStatement { StartIndex = tok[i].StartIndex };
            i++;
            if (!tok[i].IsBuiltin("("))
                throw new ParseException("'(' expected.", tok[i].StartIndex, sw);
            i++;
            try { sw.SwitchOn = parseExpression(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsExpression)
                    sw.SwitchOn = (CsExpression) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, sw, e);
            }
            if (!tok[i].IsBuiltin(")"))
                throw new ParseException("')' expected.", tok[i].StartIndex, sw);
            i++;
            if (!tok[i].IsBuiltin("{"))
                throw new ParseException("'{' expected.", tok[i].StartIndex, sw);
            i++;
            CsCaseLabel cas = null;
            while (!tok[i].IsBuiltin("}"))
            {
                if (tok[i].IsBuiltin("case") || (tok[i].IsBuiltin("default") && !tok[i + 1].IsBuiltin("(")))
                {
                    bool isDef = tok[i].IsBuiltin("default");
                    if (cas == null || cas.Statements != null)
                    {
                        cas = new CsCaseLabel { StartIndex = tok[i].StartIndex };
                        sw.Cases.Add(cas);
                    }
                    i++;
                    cas.CaseValues.Add(isDef ? null : parseExpression(tok, ref i));
                    if (!tok[i].IsBuiltin(":"))
                        throw new ParseException("':' expected.", tok[i].StartIndex, sw);
                    cas.EndIndex = tok[i].EndIndex;
                    i++;
                }
                else if (cas == null)
                    throw new ParseException("'case' <expression> or 'default:' expected.", tok[i].StartIndex, sw);
                else
                {
                    try
                    {
                        if (cas.Statements == null)
                            cas.Statements = new List<CsStatement>();
                        var stat = parseStatement(tok, ref i);
                        cas.Statements.Add(stat);
                        cas.EndIndex = stat.EndIndex;
                    }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsStatement)
                            cas.Statements.Add((CsStatement) e.IncompleteResult);
                        throw new ParseException(e.Message, e.Index, sw, e);
                    }
                }
            }
            sw.EndIndex = tok[i].EndIndex;
            i++;
            return sw;
        }
        private static CsOptionalExpressionStatement parseReturnOrThrowStatement(TokenJar tok, ref int i)
        {
            if (!tok[i].IsBuiltin("return") && !tok[i].IsBuiltin("throw"))
                tok[i].Assert("false");

            var ret = tok[i].IsBuiltin("return") ? (CsOptionalExpressionStatement) new CsReturnStatement() : new CsThrowStatement();
            ret.StartIndex = tok[i].StartIndex;
            i++;
            if (tok[i].IsBuiltin(";"))
            {
                ret.EndIndex = tok[i].EndIndex;
                i++;
                return ret;
            }
            try { ret.Expression = parseExpression(tok, ref i); }
            catch (ParseException e)
            {
                if (e.IncompleteResult is CsExpression)
                    ret.Expression = (CsExpression) e.IncompleteResult;
                throw new ParseException(e.Message, e.Index, ret, e);
            }
            if (!tok[i].IsBuiltin(";"))
                throw new ParseException("';' expected. (3)", tok[i].StartIndex, ret);
            ret.EndIndex = tok[i].EndIndex;
            i++;
            return ret;
        }
        private static CsStatement parseVariableDeclarationOrExpressionStatement(TokenJar tok, ref int i)
        {
            var startIndex = tok[i].StartIndex;
            bool isConst = false;

            if (tok[i].IsBuiltin("const"))
            {
                isConst = true;
                i++;
            }

            // See if the beginning of this statement is a type identifier followed by a variable name, in which case parse it as a variable declaration.
            CsTypeName declType = null;
            var j = i;
            try
            {
                var ty = parseTypeName(tok, ref j, typeIdentifierFlags.AllowArrays | typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers).Item1;
                if (tok[j].Type == TokenType.Identifier)
                    declType = ty;
            }
            catch (ParseException) { }

            // If this looks like a valid variable declaration, continue parsing it.
            if (declType != null)
            {
                i = j - 1;
                var decl = new CsVariableDeclarationStatement { StartIndex = startIndex, Type = declType, IsConst = isConst };
                string name = null;
                try
                {
                    do
                    {
                        i++;
                        var nameStartIndex = tok[i].StartIndex;
                        name = tok[i].Identifier();
                        i++;
                        CsExpression expr = null;
                        if (tok[i].IsBuiltin("="))
                        {
                            i++;
                            expr = parseExpression(tok, ref i);
                        }
                        decl.NamesAndInitializers.Add(new CsNameAndExpression { StartIndex = nameStartIndex, EndIndex = tok[i - 1].EndIndex, Name = name, Expression = expr });
                    }
                    while (tok[i].IsBuiltin(","));
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression && name != null)
                        decl.NamesAndInitializers.Add(new CsNameAndExpression { StartIndex = tok[i].StartIndex, Name = name, Expression = (CsExpression) e.IncompleteResult });
                    throw new ParseException(e.Message, e.Index, decl, e);
                }

                // This function does not consume the trailing ‘;’ of the statement, so that ‘using’ and ‘for’ can use it (where the statement can end with a ‘)’ or ‘,’ instead).
                decl.EndIndex = tok[i - 1].EndIndex;
                return decl;
            }

            if (isConst)
                throw new ParseException("Expected type + variable declaration after 'const'.", tok[i].StartIndex);

            // Finally, the only remaining possible way for it to be a valid statement is by being an expression.
            var exprStat = new CsExpressionStatement { StartIndex = tok[i].StartIndex };
            try { exprStat.Expression = parseExpression(tok, ref i); }
            catch (ParseException e)
            {
                var msg = exprStat.StartIndex == e.Index ? @"Invalid statement." : e.Message;
                if (e.IncompleteResult is CsExpression)
                {
                    exprStat.Expression = (CsExpression) e.IncompleteResult;
                    throw new ParseException(msg, e.Index, exprStat, e);
                }
                throw new ParseException(msg, e.Index, e.IncompleteResult, e);
            }

            // This function does not consume the trailing ‘;’ of the statement, so that ‘using’ and ‘for’ can use it (where the statement can end with a ‘)’ or ‘,’ instead).
            return exprStat;
        }
        #endregion

        #region Expressions (except Linq)
        private static CsExpression parseExpression(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionConditional(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            if (tok[i].Type == TokenType.Builtin && assignmentOperators.Contains(tok[i].TokenStr))
            {
                AssignmentOperator type;
                switch (tok[i].TokenStr)
                {
                    case "=": type = AssignmentOperator.Eq; break;
                    case "*=": type = AssignmentOperator.TimesEq; break;
                    case "/=": type = AssignmentOperator.DivEq; break;
                    case "%=": type = AssignmentOperator.ModEq; break;
                    case "+=": type = AssignmentOperator.PlusEq; break;
                    case "-=": type = AssignmentOperator.MinusEq; break;
                    case "<<=": type = AssignmentOperator.ShlEq; break;
                    case ">>=": type = AssignmentOperator.ShrEq; break;
                    case "&=": type = AssignmentOperator.AndEq; break;
                    case "^=": type = AssignmentOperator.XorEq; break;
                    case "|=": type = AssignmentOperator.OrEq; break;
                    default: throw new ParseException("Unknown assigment operator.", tok[i].StartIndex, left);
                }
                i++;
                var right = parseExpression(tok, ref i);
                return new CsAssignmentExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = type };
            }
            return left;
        }
        private static CsExpression parseExpressionConditional(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionCoalesce(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            bool haveQ = false;
            if (tok[i].IsBuiltin("?"))
            {
                haveQ = true;
                i++;
            }
            else
            {
                // This is very hacky, but I couldn't find a better way. We need to handle the following cases:
                //     myObj is int ? 5 : 1
                //     myObj is int? ? 5 : 1
                // We are fine for the second case, but in order to get that second case to work, we had to parse 'int?' as a type.
                // Therefore, if the input is actually the first case (which is more common), we have to track down that type and remove the question mark from it.
                haveQ = removeNullable(left);
            }

            if (haveQ)
            {
                CsExpression truePart;
                try { truePart = parseExpression(tok, ref i); }
                catch (ParseException e) { throw new ParseException(e.Message, e.Index, left, e); }
                if (!tok[i].IsBuiltin(":"))
                    throw new ParseException("Unterminated conditional operator. ':' expected.", tok[i].StartIndex, left);
                i++;
                try
                {
                    var falsePart = parseExpression(tok, ref i);
                    return new CsConditionalExpression { StartIndex = left.StartIndex, EndIndex = falsePart.EndIndex, Condition = left, TruePart = truePart, FalsePart = falsePart };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsConditionalExpression { StartIndex = tok[i].StartIndex, Condition = left, TruePart = truePart, FalsePart = (CsExpression) e.IncompleteResult }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }

        private static bool removeNullable(CsExpression expr)
        {
            var bin = expr as CsBinaryOperatorExpression;
            var typeOp = expr as CsBinaryTypeOperatorExpression;

            if (bin != null)
            {
                var result = removeNullable(bin.Right);
                if (result)
                    bin.EndIndex = bin.Right.EndIndex;
                return result;
            }
            else if (typeOp != null)
            {
                var nullable = typeOp.Right as CsNullableTypeName;
                if (typeOp.Operator == BinaryTypeOperator.Is && nullable != null)
                {
                    typeOp.Right = nullable.InnerType;
                    typeOp.EndIndex = nullable.InnerType.EndIndex;
                    return true;
                }
            }
            return false;
        }

        private static CsExpression parseExpressionCoalesce(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionBoolOr(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            if (tok[i].IsBuiltin("??"))
            {
                i++;
                try
                {
                    var right = parseExpressionCoalesce(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.Coalesce };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.Coalesce }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionBoolOr(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionBoolAnd(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("||"))
            {
                i++;
                try
                {
                    var right = parseExpressionBoolAnd(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.OrOr };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.OrOr }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionBoolAnd(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionLogicalOr(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("&&"))
            {
                i++;
                try
                {
                    var right = parseExpressionLogicalOr(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.AndAnd };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.AndAnd }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionLogicalOr(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionLogicalXor(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("|"))
            {
                i++;
                try
                {
                    var right = parseExpressionLogicalXor(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.Or };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.Or }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionLogicalXor(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionLogicalAnd(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("^"))
            {
                i++;
                try
                {
                    var right = parseExpressionLogicalAnd(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.Xor };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.Xor }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionLogicalAnd(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionEquality(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("&"))
            {
                i++;
                try
                {
                    var right = parseExpressionEquality(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = BinaryOperator.And };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = BinaryOperator.And }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionEquality(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionRelational(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("==") || tok[i].IsBuiltin("!="))
            {
                BinaryOperator op = tok[i].IsBuiltin("==") ? BinaryOperator.Eq : BinaryOperator.NotEq;
                i++;
                try
                {
                    var right = parseExpressionRelational(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = op }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionRelational(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionShift(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("<") || tok[i].IsBuiltin(">") || tok[i].IsBuiltin("<=") || tok[i].IsBuiltin(">=") || tok[i].IsBuiltin("is") || tok[i].IsBuiltin("as"))
            {
                if (tok[i].IsBuiltin("is") || tok[i].IsBuiltin("as"))
                {
                    var op = tok[i].IsBuiltin("is") ? BinaryTypeOperator.Is : BinaryTypeOperator.As;
                    i++;
                    try
                    {
                        var right = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays).Item1;
                        return new CsBinaryTypeOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                    }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsTypeName)
                            throw new ParseException(e.Message, e.Index, new CsBinaryTypeOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsTypeName) e.IncompleteResult, Operator = op }, e);
                        throw new ParseException(e.Message, e.Index, left, e);
                    }
                }
                else
                {
                    BinaryOperator op = tok[i].IsBuiltin("<") ? BinaryOperator.Less : tok[i].IsBuiltin("<=") ? BinaryOperator.LessEq : tok[i].IsBuiltin(">") ? BinaryOperator.Greater : BinaryOperator.GreaterEq;
                    i++;
                    try
                    {
                        var right = parseExpressionShift(tok, ref i);
                        left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                    }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsExpression)
                            throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = op }, e);
                        throw new ParseException(e.Message, e.Index, left, e);
                    }
                }
            }
            return left;
        }
        private static CsExpression parseExpressionShift(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionAdditive(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("<<") || tok[i].IsBuiltin(">>"))
            {
                BinaryOperator op = tok[i].IsBuiltin("<<") ? BinaryOperator.Shl : BinaryOperator.Shr;
                i++;
                try
                {
                    var right = parseExpressionAdditive(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = op }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionAdditive(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionMultiplicative(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("+") || tok[i].IsBuiltin("-"))
            {
                BinaryOperator op = tok[i].IsBuiltin("+") ? BinaryOperator.Plus : BinaryOperator.Minus;
                i++;
                try
                {
                    var right = parseExpressionMultiplicative(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = op }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionMultiplicative(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionUnary(tok, ref i, tryNotToThrow);
            if (left == null)
                return null;
            while (tok[i].IsBuiltin("*") || tok[i].IsBuiltin("/") || tok[i].IsBuiltin("%"))
            {
                BinaryOperator op = tok[i].IsBuiltin("*") ? BinaryOperator.Times : tok[i].IsBuiltin("/") ? BinaryOperator.Div : BinaryOperator.Mod;
                i++;
                try
                {
                    var right = parseExpressionUnary(tok, ref i);
                    left = new CsBinaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, Left = left, Right = right, Operator = op };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        throw new ParseException(e.Message, e.Index, new CsBinaryOperatorExpression { StartIndex = tok[i].StartIndex, Left = left, Right = (CsExpression) e.IncompleteResult, Operator = op }, e);
                    throw new ParseException(e.Message, e.Index, left, e);
                }
            }
            return left;
        }
        private static CsExpression parseExpressionUnary(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var startIndex = tok[i].StartIndex;
            if (tok[i].Type != TokenType.Builtin)
                return parseExpressionPrimary(tok, ref i);

            UnaryOperator op;
            switch (tok[i].TokenStr)
            {
                case "+": op = UnaryOperator.Plus; break;
                case "-": op = UnaryOperator.Minus; break;
                case "*": op = UnaryOperator.PointerDeref; break;
                case "&": op = UnaryOperator.AddressOf; break;
                case "!": op = UnaryOperator.Not; break;
                case "~": op = UnaryOperator.Neg; break;
                case "++": op = UnaryOperator.PrefixInc; break;
                case "--": op = UnaryOperator.PrefixDec; break;
                default:
                    return parseExpressionPrimary(tok, ref i, tryNotToThrow);
            }
            i++;
            var operand = parseExpressionUnary(tok, ref i);
            return new CsUnaryOperatorExpression { StartIndex = startIndex, EndIndex = operand.EndIndex, Operand = operand, Operator = op };
        }
        private static CsExpression parseExpressionPrimary(TokenJar tok, ref int i, bool tryNotToThrow = false)
        {
            var left = parseExpressionIdentifierOrKeyword(tok, ref i);
            while (tok[i].IsBuiltin(".") || tok[i].IsBuiltin("->") || tok[i].IsBuiltin("(") || tok[i].IsBuiltin("[") || tok[i].IsBuiltin("++") || tok[i].IsBuiltin("--"))
            {
                if (tok[i].IsBuiltin(".") || tok[i].IsBuiltin("->"))
                {
                    MemberAccessType type = tok[i].IsBuiltin(".") ? MemberAccessType.Regular : MemberAccessType.PointerDeref;
                    i++;
                    try
                    {
                        var right = parseExpressionIdentifier(tok, ref i);
                        left = new CsMemberAccessExpression { StartIndex = left.StartIndex, EndIndex = right.EndIndex, AccessType = type, Left = left, Right = right };
                    }
                    catch (ParseException e)
                    {
                        throw new ParseException(e.Message, e.Index, left, e);
                    }
                }
                else if (tok[i].IsBuiltin("(") || tok[i].IsBuiltin("["))
                {
                    var func = new CsFunctionCallExpression { StartIndex = left.StartIndex, Left = left };
                    try { func.Arguments = parseArgumentList(tok, ref i, out func.IsIndexer); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is List<CsArgument>)
                            func.Arguments = (List<CsArgument>) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, func, e);
                    }
                    func.EndIndex = tok[i - 1].EndIndex;
                    left = func;
                }
                else if (tok[i].IsBuiltin("++"))
                {
                    left = new CsUnaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = tok[i].EndIndex, Operand = left, Operator = UnaryOperator.PostfixInc };
                    i++;
                }
                else if (tok[i].IsBuiltin("--"))
                {
                    left = new CsUnaryOperatorExpression { StartIndex = left.StartIndex, EndIndex = tok[i].EndIndex, Operand = left, Operator = UnaryOperator.PostfixDec };
                    i++;
                }
            }
            if (left is CsSimpleNameExpression && ((CsSimpleNameExpression) left).SimpleName is CsSimpleNameBuiltin)
            {
                if (tryNotToThrow)
                    return null;
                else
                    throw new ParseException("'{0}' cannot be an expression by itself.".Fmt(left.ToString()), tok[i - 1].StartIndex);
            }
            return left;
        }
        private static List<CsArgument> parseArgumentList(TokenJar tok, ref int i, out bool isIndexer)
        {
            isIndexer = tok[i].IsBuiltin("[");
            if (!tok[i].IsBuiltin("(") && !tok[i].IsBuiltin("["))
                throw new ParseException("'(' or '[' expected.", tok[i].StartIndex);
            i++;
            if (isIndexer && tok[i].IsBuiltin("]"))
                throw new ParseException("Empty indexing expressions are not allowed.", tok[i].StartIndex);
            var arguments = new List<CsArgument>();
            if (!tok[i].IsBuiltin(isIndexer ? "]" : ")"))
            {
                ArgumentMode inoutref = ArgumentMode.In;
                string argName = null;
                try
                {
                    while (true)
                    {
                        var argStartIndex = tok[i].StartIndex;
                        argName = null;
                        if (tok[i].Type == TokenType.Identifier && tok.IndexExists(i + 1) && tok[i + 1].IsBuiltin(":"))
                        {
                            argName = tok[i].Identifier();
                            i += 2;
                        }
                        inoutref = ArgumentMode.In;
                        if (tok[i].IsBuiltin("ref"))
                        {
                            inoutref = ArgumentMode.Ref;
                            i++;
                        }
                        else if (tok[i].IsBuiltin("out"))
                        {
                            inoutref = ArgumentMode.Out;
                            i++;
                        }
                        var expr = parseExpression(tok, ref i);
                        arguments.Add(new CsArgument { StartIndex = argStartIndex, EndIndex = expr.EndIndex, ArgumentName = argName, ArgumentMode = inoutref, ArgumentExpression = expr });
                        if (tok[i].IsBuiltin(isIndexer ? "]" : ")"))
                            break;
                        else if (!tok[i].IsBuiltin(","))
                            throw new ParseException(isIndexer ? "',' or ']' expected." : "',' or ')' expected.", tok[i].StartIndex);
                        i++;
                    }
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        arguments.Add(new CsArgument { StartIndex = tok[i].StartIndex, ArgumentName = argName, ArgumentMode = inoutref, ArgumentExpression = (CsExpression) e.IncompleteResult });
                    throw new ParseException(e.Message, e.Index, arguments, e);
                }
            }
            i++;
            return arguments;
        }
        private static CsExpression parseExpressionIdentifierOrKeyword(TokenJar tok, ref int i)
        {
            var startIndex = tok[i].StartIndex;

            if (tok[i].IsIdentifier("from") && tok[i + 1].Type == TokenType.Identifier)
            {
                var linq = new CsLinqExpression { StartIndex = startIndex };
                parseLinqQueryExpression(linq, tok, ref i);
                linq.EndIndex = tok[i - 1].EndIndex;
                return linq;
            }

            if (tok[i].IsBuiltin("{"))
            {
                try
                {
                    var exprs = parseArrayLiteral(tok, ref i);
                    return new CsArrayLiteralExpression { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Expressions = exprs };
                }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is List<CsExpression>)
                        throw new ParseException(e.Message, e.Index, new CsArrayLiteralExpression { StartIndex = startIndex, Expressions = (List<CsExpression>) e.IncompleteResult }, e);
                    throw;
                }
            }

            if (tok[i].Type == TokenType.CharacterLiteral)
                return new CsCharacterLiteralExpression { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Literal = tok[i++].TokenStr[0] };

            if (tok[i].Type == TokenType.StringLiteral)
                return new CsStringLiteralExpression { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Literal = tok[i++].TokenStr };

            if (tok[i].Type == TokenType.NumberLiteral)
                return new CsNumberLiteralExpression { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Literal = tok[i++].TokenStr };

            if (tok[i].IsBuiltin("true")) { i++; return new CsBooleanLiteralExpression { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex, Literal = true }; }
            if (tok[i].IsBuiltin("false")) { i++; return new CsBooleanLiteralExpression { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex, Literal = false }; }
            if (tok[i].IsBuiltin("null")) { i++; return new CsNullExpression { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex }; }
            if (tok[i].IsBuiltin("this")) { i++; return new CsThisExpression { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex }; }
            if (tok[i].IsBuiltin("base")) { i++; return new CsBaseExpression { StartIndex = tok[i - 1].StartIndex, EndIndex = tok[i - 1].EndIndex }; }

            if (tok[i].IsBuiltin("typeof") || tok[i].IsBuiltin("default") || tok[i].IsBuiltin("sizeof"))
            {
                var typof = tok[i].IsBuiltin("typeof");
                var expr = typof ? new CsTypeofExpression() : tok[i].IsBuiltin("default") ? (CsTypeOperatorExpression) new CsDefaultExpression() : new CsSizeofExpression();
                expr.StartIndex = startIndex;
                i++;
                if (!tok[i].IsBuiltin("("))
                    throw new ParseException("'(' expected after 'typeof' or 'default'.", tok[i].StartIndex);
                i++;
                CsTypeName ty;
                var tif = typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays;
                try { ty = parseTypeName(tok, ref i, typof ? tif | typeIdentifierFlags.AllowEmptyGenerics : tif).Item1; }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsTypeName)
                        expr.Type = (CsTypeName) e.IncompleteResult;
                    throw new ParseException(e.Message, e.Index, expr, e);
                }
                expr.Type = ty;
                if (!tok[i].IsBuiltin(")"))
                    throw new ParseException("')' expected.", tok[i].StartIndex, expr);
                expr.EndIndex = tok[i].EndIndex;
                i++;
                return expr;
            }

            if (tok[i].IsBuiltin("checked") || tok[i].IsBuiltin("unchecked"))
            {
                var expr = tok[i].IsBuiltin("checked") ? (CsCheckedUncheckedExpression) new CsCheckedExpression() : new CsUncheckedExpression();
                expr.StartIndex = startIndex;
                i++;
                if (!tok[i].IsBuiltin("("))
                    throw new ParseException("'(' expected after 'checked' or 'unchecked'.", tok[i].StartIndex);
                i++;
                expr.Subexpression = parseExpression(tok, ref i);
                if (!tok[i].IsBuiltin(")"))
                    throw new ParseException("')' expected.", tok[i].StartIndex, expr);
                expr.EndIndex = tok[i].EndIndex;
                i++;
                return expr;
            }

            if (tok[i].IsBuiltin("new"))
            {
                i++;
                if (tok[i].IsBuiltin("{"))
                {
                    var anon = new CsNewAnonymousTypeExpression { StartIndex = startIndex };
                    try { anon.Initializers = parseArrayLiteral(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is List<CsExpression>)
                            anon.Initializers = (List<CsExpression>) e.IncompleteResult;
                        throw new ParseException(e.Message, e.Index, anon, e);
                    }
                    anon.EndIndex = tok[i - 1].EndIndex;
                    return anon;
                }
                else if (tok[i].IsBuiltin("[") && tok[i + 1].IsBuiltin("]"))
                {
                    i += 2;
                    // Implicitly-typed array
                    if (!tok[i].IsBuiltin("{"))
                        throw new ParseException("'{' expected.", tok[i].StartIndex);
                    try
                    {
                        var items = parseArrayLiteral(tok, ref i);
                        return new CsNewImplicitlyTypedArrayExpression { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Items = items };
                    }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is List<CsExpression>)
                            throw new ParseException(e.Message, e.Index, new CsNewImplicitlyTypedArrayExpression { StartIndex = startIndex, Items = (List<CsExpression>) e.IncompleteResult }, e);
                        throw;
                    }
                }
                else if (tok[i].Type != TokenType.Identifier && tok[i].Type != TokenType.Builtin)
                    throw new ParseException("'{', '[' or type expected.", tok[i].StartIndex);
                else
                {
                    var ty = parseTypeName(tok, ref i, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays | typeIdentifierFlags.Lenient).Item1;
                    if (tok[i].IsBuiltin("("))
                    {
                        // Constructor call with parameters
                        var constructor = new CsNewConstructorExpression { StartIndex = startIndex, Type = ty };
                        try
                        {
                            bool dummy;
                            constructor.Arguments = parseArgumentList(tok, ref i, out dummy);
                            if (tok[i].IsBuiltin("{"))
                                constructor.Initializers = parseArrayLiteral(tok, ref i);
                        }
                        catch (ParseException e)
                        {
                            if (e.IncompleteResult is List<CsArgument>)
                                constructor.Arguments = (List<CsArgument>) e.IncompleteResult;
                            else if (e.IncompleteResult is List<CsExpression>)
                                constructor.Initializers = (List<CsExpression>) e.IncompleteResult;
                            throw new ParseException(e.Message, e.Index, constructor, e);
                        }
                        constructor.EndIndex = tok[i - 1].EndIndex;
                        return constructor;
                    }
                    else if (tok[i].IsBuiltin("{"))
                    {
                        // Constructor call without parameters
                        var constructor = new CsNewConstructorExpression { StartIndex = startIndex, Type = ty };
                        try { constructor.Initializers = parseArrayLiteral(tok, ref i); }
                        catch (ParseException e)
                        {
                            if (e.IncompleteResult is List<CsExpression>)
                                constructor.Initializers = (List<CsExpression>) e.IncompleteResult;
                            throw new ParseException(e.Message, e.Index, constructor, e);
                        }
                        constructor.EndIndex = tok[i - 1].EndIndex;
                        return constructor;
                    }
                    else if (tok[i].IsBuiltin("["))
                    {
                        // Array construction
                        bool dummy;
                        var k = i;
                        var ret = new CsNewArrayExpression { StartIndex = startIndex, Type = ty };
                        ret.SizeExpressions.AddRange(parseArgumentList(tok, ref i, out dummy).Select(p =>
                        {
                            if (p.ArgumentMode != ArgumentMode.In)
                                throw new ParseException("'out' and 'ref' parameters are not allowed in an array constructor.", tok[k].StartIndex);
                            return p.ArgumentExpression;
                        }));
                        while (tok[i].IsBuiltin("["))
                        {
                            i++;
                            int num = 1;
                            while (tok[i].IsBuiltin(","))
                            {
                                num++;
                                i++;
                            }
                            if (!tok[i].IsBuiltin("]"))
                                throw new ParseException("',' or ']' expected.", tok[i].StartIndex, ret);
                            i++;
                            ret.AdditionalRanks.Add(num);
                        }
                        if (tok[i].IsBuiltin("{"))
                        {
                            try { ret.Items = parseArrayLiteral(tok, ref i); }
                            catch (ParseException e)
                            {
                                if (e.IncompleteResult is List<CsExpression>)
                                    ret.Items = (List<CsExpression>) e.IncompleteResult;
                                throw new ParseException(e.Message, e.Index, ret, e);
                            }
                        }
                        ret.EndIndex = tok[i - 1].EndIndex;
                        return ret;
                    }
                    else
                        throw new ParseException("'(', '[' or '{' expected.", tok[i].StartIndex);
                }
            }

            if (tok[i].IsBuiltin("delegate"))
            {
                i++;
                var delegateParams = tok[i].IsBuiltin("(") ? parseParameterList(tok, ref i) : null;
                var block = parseBlock(tok, ref i);
                return new CsAnonymousMethodExpression { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Block = block, Parameters = delegateParams };
            }

            // See if this could be a lambda expression. If it is, set 'parameters' to something non-null and move 'i' to behind the '=>'. Otherwise, keep 'i' unchanged.
            List<CsParameter> parameters = null;
            if (tok[i].Type == TokenType.Identifier && tok[i + 1].IsBuiltin("=>"))
            {
                // p => ...
                parameters = new List<CsParameter> { new CsParameter { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Name = tok[i].TokenStr } };
                i += 2;
            }
            else if (tok[i].IsBuiltin("("))
            {
                // () => ...
                if (tok[i + 1].IsBuiltin(")") && tok[i + 2].IsBuiltin("=>"))
                {
                    parameters = new List<CsParameter>();
                    i += 3;
                }
                else
                {
                    // Try (Type p1, Type p2) => ...
                    try
                    {
                        var j = i;
                        var tried = parseParameterList(tok, ref j, tryNotToThrow: true);
                        if (tried != null && tok[j].IsBuiltin("=>"))
                        {
                            i = j + 1;
                            parameters = tried;
                        }
                    }
                    catch { }
                }

                // If (Type p1, Type p2) => ... still didn't work, try (p1, p2) => ...
                if (parameters == null && tok[i + 1].Type == TokenType.Identifier)
                {
                    var ps = new List<CsParameter> { new CsParameter { StartIndex = tok[i + 1].StartIndex, EndIndex = tok[i + 1].EndIndex, Name = tok[i + 1].TokenStr } };
                    var j = i + 2;
                    while (tok[j].IsBuiltin(",") && tok[j + 1].Type == TokenType.Identifier)
                    {
                        ps.Add(new CsParameter { StartIndex = tok[j + 1].StartIndex, EndIndex = tok[j + 1].EndIndex, Name = tok[j + 1].TokenStr });
                        j += 2;
                    }
                    if (tok[j].IsBuiltin(")") && tok[j + 1].IsBuiltin("=>"))
                    {
                        parameters = ps;
                        i = j + 2;
                    }
                }
            }

            // If 'parameters' is non-null, we found a lambda expression and 'i' is pointing behind the '=>'.
            if (parameters != null)
            {
                if (tok[i].IsBuiltin("{"))
                {
                    var lambda = new CsBlockLambdaExpression { StartIndex = startIndex, Parameters = parameters };
                    try { lambda.Block = parseBlock(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsBlock)
                        {
                            lambda.Block = (CsBlock) e.IncompleteResult;
                            throw new ParseException(e.Message, e.Index, lambda, e);
                        }
                        throw;
                    }
                    lambda.EndIndex = lambda.Block.EndIndex;
                    return lambda;
                }
                else
                {
                    var lambda = new CsSimpleLambdaExpression { StartIndex = startIndex, Parameters = parameters };
                    try { lambda.Body = parseExpression(tok, ref i); }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsExpression)
                        {
                            lambda.Body = (CsExpression) e.IncompleteResult;
                            throw new ParseException(e.Message, e.Index, lambda, e);
                        }
                        throw;
                    }
                    lambda.EndIndex = lambda.Body.EndIndex;
                    return lambda;
                }
            }

            // If we get to here, then there is an open-parenthesis that is not the beginning of a lambda expression parameter list.
            if (tok[i].IsBuiltin("("))
            {
                i++;

                // Every time we encounter an open-parenthesis, we don't know in advance whether it's a cast, a sub-expression, or a lambda expression.
                // We've already checked for lambda expression above, so we can rule that one out, but to check whether it's a cast, we need to tentatively 
                // parse it, see if it is followed by ')', and then follow a heuristic specified in the C# standard.

                CsTypeName typeName = null;
                CsExpression expression = null;
                int afterType = i;
                int afterExpression = i;

                // Does it parse as a type?
                try
                {
                    var result = parseTypeName(tok, ref afterType, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.AllowNullablesAndPointers | typeIdentifierFlags.AllowArrays, true);
                    if (result != null && tok[afterType].IsBuiltin(")"))
                    {
                        typeName = result.Item1;
                        afterType++;
                    }
                }
                catch { }

                // Does it parse as a parenthesised expression?
                try
                {
                    expression = parseExpression(tok, ref afterExpression, true);
                    if (!tok[afterExpression].IsBuiltin(")"))
                        throw new ParseException("')' expected after parenthesised expression.", tok[afterExpression].StartIndex);
                    afterExpression++;
                }
                catch (ParseException)
                {
                    // It’s neither an expression nor a valid type name.
                    if (typeName == null)
                        throw;
                }

                // According to the Standard, we are supposed to interpret this as a cast if:
                // • it definitely doesn’t parse as an expression; OR
                // • the next token is one of a special set

                if (expression == null || (typeName != null && (
                    tok[afterType].IsBuiltin("~") || tok[afterType].IsBuiltin("!") || tok[afterType].IsBuiltin("(") ||
                    tok[afterType].Type == TokenType.Identifier || tok[afterType].Type == TokenType.CharacterLiteral ||
                    tok[afterType].Type == TokenType.NumberLiteral || tok[afterType].Type == TokenType.StringLiteral ||
                    (tok[afterType].Type == TokenType.Builtin && tok[afterType].TokenStr != "is" && tok[afterType].TokenStr != "as" && Lexer.Keywords.Contains(tok[afterType].TokenStr)))))
                {
                    // It’s a cast!
                    try
                    {
                        i = afterType;
                        var operand = parseExpressionUnary(tok, ref i);
                        return new CsCastExpression { StartIndex = startIndex, EndIndex = operand.EndIndex, Operand = operand, Type = typeName };
                    }
                    catch (ParseException e)
                    {
                        if (e.IncompleteResult is CsExpression)
                            throw new ParseException(e.Message, e.Index, new CsCastExpression { StartIndex = startIndex, Operand = (CsExpression) e.IncompleteResult, Type = typeName }, e);
                        throw;
                    }
                }

                // It’s a subexpression!
                i = afterExpression;
                return new CsParenthesizedExpression { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, Subexpression = expression };
            }

            var simpleName = parseExpressionIdentifier(tok, ref i);
            return new CsSimpleNameExpression { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, SimpleName = simpleName };
        }
        private static string[] _genericMethodGroupMagicalTokens = new[] { "(", ")", "]", ":", ";", ",", ".", "?", "==", "!=" };
        private static CsSimpleName parseExpressionIdentifier(TokenJar tok, ref int i)
        {
            var startIndex = tok[i].StartIndex;

            // Check if this can be parsed as a type identifier. If it can't, don't throw; if it failed because of a malformed generic type parameter, it could still be a less-than operator.
            CsTypeName ty;
            var j = i;
            try
            {
                ty = parseTypeName(tok, ref j, typeIdentifierFlags.AllowKeywords | typeIdentifierFlags.DontAllowDot).Item1;
            }
            catch (ParseException)
            {
                goto afterTy;
            }

            // Since we didn't allow dot and we didn't allow arrays, nullables or pointers, we should get a simple name
            if (!(ty is CsConcreteTypeName) || ((CsConcreteTypeName) ty).Parts.Count != 1)
                throw new ParseException("Unexpected internal error: Expected simple name, received '{0}'.".Fmt(ty), i);
            var simpleName = ((CsConcreteTypeName) ty).Parts[0];

            // Special case: only accept an identifier with generic type parameters if it has one of a special set of tokens following it; otherwise, reject it so that it is parsed as a less-than operator
            // (this is so that a generic method group expression is correctly parsed, but two less-than/greater-than or less-than/shift-right expressions are not mistaken for a generic method group expression)
            if (!simpleName.EndsWithGenerics || (tok.IndexExists(j) && tok[j].Type == TokenType.Builtin && _genericMethodGroupMagicalTokens.Contains(tok[j].TokenStr)))
            {
                i = j;
                return simpleName;
            }

            afterTy:
            var ret = new CsSimpleNameIdentifier { StartIndex = startIndex, EndIndex = tok[i].EndIndex, Name = tok[i].Identifier("Type expected.") };
            i++;
            return ret;
        }
        private static List<CsExpression> parseArrayLiteral(TokenJar tok, ref int i)
        {
            tok[i].Assert("{");
            i++;
            var list = new List<CsExpression>();
            while (!tok[i].IsBuiltin("}"))
            {
                try { list.Add(parseExpression(tok, ref i)); }
                catch (ParseException e)
                {
                    if (e.IncompleteResult is CsExpression)
                        list.Add((CsExpression) e.IncompleteResult);
                    throw new ParseException(e.Message, e.Index, list, e);
                }
                if (!tok[i].IsBuiltin("}") && !tok[i].IsBuiltin(","))
                    throw new ParseException("'}' or ',' expected.", tok[i].StartIndex, list);
                if (tok[i].IsBuiltin("}"))
                    break;
                i++;
            }
            i++;
            return list;
        }
        #endregion

        #region Linq-syntax expressions
        private static void parseLinqQueryExpression(CsLinqExpression linq, TokenJar tok, ref int i)
        {
            linq.Elements.Add(parseLinqFromClause(tok, ref i));
            parseLinqQueryBody(linq, tok, ref i);
        }
        private static void parseLinqQueryBody(CsLinqExpression linq, TokenJar tok, ref int i)
        {
            while (tok[i].IsIdentifier("from") || tok[i].IsIdentifier("let") || tok[i].IsIdentifier("where") || tok[i].IsIdentifier("join") || tok[i].IsIdentifier("orderby"))
            {
                if (tok[i].IsIdentifier("from"))
                    linq.Elements.Add(parseLinqFromClause(tok, ref i));
                else if (tok[i].IsIdentifier("let"))
                    linq.Elements.Add(parseLinqLetClause(tok, ref i));
                else if (tok[i].IsIdentifier("where"))
                    linq.Elements.Add(parseLinqWhereClause(tok, ref i));
                else if (tok[i].IsIdentifier("join"))
                    linq.Elements.Add(parseLinqJoinClause(tok, ref i));
                else if (tok[i].IsIdentifier("orderby"))
                    linq.Elements.Add(parseLinqOrderByClause(tok, ref i));
            }

            if (tok[i].IsIdentifier("select"))
                linq.Elements.Add(parseLinqSelectClause(tok, ref i));
            else if (tok[i].IsIdentifier("group"))
                linq.Elements.Add(parseLinqGroupByClause(tok, ref i));
            else
                throw new ParseException("'select' or 'group' expected.", tok[i].StartIndex);

            if (tok[i].IsIdentifier("into"))
                parseLinqQueryContinuation(linq, tok, ref i);
        }
        private static CsLinqElement parseLinqJoinClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("join");
            var startIndex = tok[i].StartIndex;
            i++;
            var item = tok[i].Identifier();
            i++;
            if (!tok[i].IsBuiltin("in"))
                throw new ParseException("'in' expected.", tok[i].StartIndex);
            i++;
            var srcExpr = parseExpression(tok, ref i);
            if (!tok[i].IsIdentifier("on"))
                throw new ParseException("'on' expected.", tok[i].StartIndex);
            i++;
            var keyExpr1 = parseExpression(tok, ref i);
            if (!tok[i].IsIdentifier("equals"))
                throw new ParseException("'equals' expected.", tok[i].StartIndex);
            i++;
            var keyExpr2 = parseExpression(tok, ref i);
            string into = null;
            if (tok[i].IsIdentifier("into"))
            {
                i++;
                into = tok[i].Identifier();
                i++;
            }
            return new CsLinqJoinClause
            {
                StartIndex = startIndex,
                EndIndex = tok[i - 1].EndIndex,
                ItemName = item,
                SourceExpression = srcExpr,
                KeyExpression1 = keyExpr1,
                KeyExpression2 = keyExpr2,
                IntoName = into
            };
        }
        private static CsLinqElement parseLinqLetClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("let");
            var startIndex = tok[i].StartIndex;
            i++;
            var item = tok[i].Identifier();
            i++;
            if (!tok[i].IsBuiltin("="))
                throw new ParseException("'=' expected.", tok[i].StartIndex);
            i++;
            var expr = parseExpression(tok, ref i);
            return new CsLinqLetClause { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, ItemName = item, Expression = expr };
        }
        private static CsLinqElement parseLinqWhereClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("where");
            var startIndex = tok[i].StartIndex;
            i++;
            var whereExpr = parseExpression(tok, ref i);
            return new CsLinqWhereClause { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, WhereExpression = whereExpr };
        }
        private static CsLinqElement parseLinqOrderByClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("orderby");
            var orderby = new CsLinqOrderByClause { StartIndex = tok[i].StartIndex };
            i++;
            while (true)
            {
                var startIndex = tok[i].StartIndex;
                var expr = parseExpression(tok, ref i);
                var type = LinqOrderByType.None;
                if (tok[i].IsIdentifier("ascending"))
                {
                    type = LinqOrderByType.Ascending;
                    i++;
                }
                else if (tok[i].IsIdentifier("descending"))
                {
                    type = LinqOrderByType.Descending;
                    i++;
                }
                orderby.KeyExpressions.Add(new CsLinqOrderBy { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, OrderByExpression = expr, OrderByType = type });
                if (!tok[i].IsBuiltin(","))
                    break;
                i++;
            }
            orderby.EndIndex = tok[i - 1].EndIndex;
            return orderby;
        }
        private static CsLinqElement parseLinqSelectClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("select");
            var startIndex = tok[i].StartIndex;
            i++;
            var selectExpr = parseExpression(tok, ref i);
            return new CsLinqSelectClause { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, SelectExpression = selectExpr };
        }
        private static CsLinqElement parseLinqGroupByClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("group");
            var startIndex = tok[i].StartIndex;
            i++;
            var selExpr = parseExpression(tok, ref i);
            if (!tok[i].IsIdentifier("by"))
                throw new ParseException("'by' expected.", tok[i].StartIndex);
            i++;
            var keyExpr = parseExpression(tok, ref i);
            return new CsLinqGroupByClause { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, SelectionExpression = selExpr, KeyExpression = keyExpr };
        }
        private static void parseLinqQueryContinuation(CsLinqExpression linq, TokenJar tok, ref int i)
        {
            tok[i].Assert("into");
            var startIndex = tok[i].StartIndex;
            i++;
            linq.Elements.Add(new CsLinqIntoClause { StartIndex = startIndex, EndIndex = tok[i].EndIndex, ItemName = tok[i].Identifier() });
            i++;
            parseLinqQueryBody(linq, tok, ref i);
        }
        private static CsLinqFromClause parseLinqFromClause(TokenJar tok, ref int i)
        {
            tok[i].Assert("from");
            var startIndex = tok[i].StartIndex;
            i++;
            var item = tok[i].Identifier();
            i++;
            if (!tok[i].IsBuiltin("in"))
                throw new ParseException("'in' expected.", tok[i].StartIndex);
            i++;
            var sourceExpr = parseExpression(tok, ref i);
            return new CsLinqFromClause { StartIndex = startIndex, EndIndex = tok[i - 1].EndIndex, ItemName = item, SourceExpression = sourceExpr };
        }
        #endregion
    }
}
