using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

/*
 * Provide a proper way to distinguish exceptions due to the caller breaking some contract from exceptions due to data load failures. Always pass through the former.
 * Can the Follow attribute be implemented separately using XmlClassifyOptions?
 */

namespace RT.Util.Xml
{
    /// <summary>
    /// Provides static methods to save objects of (almost) arbitrary classes into XML files and load them again.
    /// See the remarks section for features and limitations.
    /// </summary>
    /// <remarks>
    /// <para>By default, XmlClassify persists the value of all instance fields, including private, inherited and compiler-generated ones. It does not persist static members or the result of property getters.
    ///    Each field is persisted in an XML tag whose name is the field’s name minus any leading underscores. Compiler-generated fields for automatically-implemented properties are
    ///    instead persisted in an XML tag whose name is the automatically-implemented property’s name minus any leading underscores.</para>
    /// <para>Features:</para>
    /// <list type="bullet">
    /// <item><description>XmlClassify fully supports all the built-in types which are keywords in C# except ‘object’ and ‘dynamic’. It also supports DateTime.</description></item>
    /// <item><description>XmlClassify fully supports classes and structs that contain only fields of the above types as well as fields whose type is itself such a class or struct.</description></item>
    /// <item><description>XmlClassify has special handling for classes that implement IDictionary&lt;K, V&gt;, where V must be a type also supported by XmlClassify. K must be string, an integer type, or an enum type.
    ///    If the field is of a concrete type, that type is maintained.
    ///    If the field is of the interface type IDictionary&lt;K, V&gt; itself, the type Dictionary&lt;K, V&gt; is used to reconstruct the object.</description></item>
    /// <item><description>XmlClassify has special handling for classes that implement ICollection&lt;T&gt;, where T must be a type also supported by XmlClassify.
    ///    If the field is of a concrete type, that type is maintained.
    ///    If the field is of the interface type ICollection&lt;T&gt; itself, the type List&lt;T&gt; is used to reconstruct the object.
    ///    If the type also implements IDictionary&lt;K, V&gt;, the special handling for that takes precedence.</description></item>
    /// <item><description>XmlClassify handles the <see cref="XElement"/> type by persisting the XML directly.</description></item>
    /// <item><description>XmlClassify also supports <see cref="KeyValuePair{TKey,TValue}"/> and all the different Tuple types.</description></item>
    /// <item><description>For classes that don’t implement any of the above-mentioned interfaces, XmlClassify supports polymorphism. The actual type of an instance is persisted if it is different from the declared type.</description></item>
    /// <item><description>XmlClassify supports auto-implemented properties. The XML tag’s name is the name of the property rather than the hidden auto-generated field, although the field’s value is persisted.
    ///    All other properties are ignored.</description></item>
    /// <item><description>XmlClassify ignores the order of XML tags (except when handling collections and dictionaries). It uses tag names to identify which tag belongs to which field.</description></item>
    /// <item><description>XmlClassify silently discards unrecognised XML tags instead of throwing errors. This is by design because it enables the programmer to remove a field from a class without invalidating previously-saved XML files.</description></item>
    /// <item><description>XmlClassify silently ignores missing XML tags. A field whose XML tag is missing retains the value assigned to it by the parameterless constructor.
    ///    This is by design because it enables the programmer to add a new field to a class (and to specify a default initialisation value for it) without invalidating previously-saved XML files.</description></item>
    /// <item><description>The following custom attributes can be used to alter XmlClassify’s behaviour. See the custom attribute class’s documentation for more information:
    ///    <see cref="XmlFollowIdAttribute"/>, <see cref="XmlIdAttribute"/>, <see cref="XmlIgnoreAttribute"/>, <see cref="XmlIgnoreIfAttribute"/>,
    ///    <see cref="XmlIgnoreIfDefaultAttribute"/>, <see cref="XmlIgnoreIfEmptyAttribute"/>, <see cref="XmlParentAttribute"/>. Any attribute that can be used on a field, can equally well be used on an auto-implemented property,
    ///    but not on any other properties.</description></item>
    /// <item><description>XmlClassify maintains object identity and correctly handles cycles in the object graph (by using XML attributes to refer to earlier tags).</description></item>
    /// <item><description>XmlClassify can make use of type substitutions. See <see cref="IXmlClassifySubstitute{TTrue,TSubstitute}"/> for more information.</description></item>
    /// <item><description>XmlClassify allows you to pre-/post-process the objects serialised by it. See <see cref="IXmlClassifyProcess"/> for more information.</description></item>
    /// <item><description>XmlClassify allows you to pre-/post-process the XML generated by it. See <see cref="IXmlClassifyProcessXml"/> for more information.</description></item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item><description>XmlClassify requires that every type involved have a parameterless constructor, although it need not be public. This parameterless constructor is executed with all its side-effects before each object is reconstructed.</description></item>
    /// <item><description>If a field is of type ICollection&lt;T&gt;, IList&lt;T&gt;, IDictionary&lt;K, V&gt;, or any class that implements either of these, polymorphism is not supported, and nor is any information stored in those classes.
    ///    In particular, this means that the comparer used by a SortedDictionary&lt;K, V&gt; is not persisted. A comparer assigned by the class’s parameterless constructor is also not used.</description></item>
    /// </list>
    /// </remarks>
    public static class XmlClassify
    {
        /// <summary>
        /// Options used when null is passed to methods that take options. Make sure not to modify this instance if any thread in the application
        /// might be in the middle of using <see cref="XmlClassify"/>; ideally the options shoud be set once during startup and never changed after that.
        /// </summary>
        public static XmlClassifyOptions DefaultOptions = new XmlClassifyOptions();

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <param name="options">Options.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string filename, XmlClassifyOptions options = null)
        {
            return (T) LoadObjectFromXmlFile(typeof(T), filename, options);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <param name="type">Type of object to read.</param>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <param name="options">Options.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static object LoadObjectFromXmlFile(Type type, string filename, XmlClassifyOptions options = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            XElement elem;
            using (var strRead = new StreamReader(filename, Encoding.UTF8))
                elem = XElement.Load(strRead);
            return new classifier(options, defaultBaseDir).Declassify(type, elem);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <param name="options">Options.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem, XmlClassifyOptions options = null)
        {
            return (T) new classifier(options).Declassify(typeof(T), elem);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <param name="type">Type of object to reconstruct.</param>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <param name="options">Options.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static object ObjectFromXElement(Type type, XElement elem, XmlClassifyOptions options = null)
        {
            return new classifier(options).Declassify(type, elem);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree by applying the values to an existing instance of the type.
        /// Any objects contained within the object are instantiated anew; only the top-level object passed in is re-used.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="xml">XML tree to reconstruct object from.</param>
        /// <param name="intoObject">Object to assign values to in order to reconstruct the original object.</param>
        /// <param name="options">Options.</param>
        public static void XmlIntoObject<T>(XElement xml, T intoObject, XmlClassifyOptions options = null)
        {
            new classifier(options).XmlIntoObject(xml, intoObject, typeof(T), null);
        }

        /// <summary>
        /// Reconstructs an object from the specified XML file by applying the values to an existing instance of the desired type.
        /// Any objects contained within the object are instantiated anew; only the top-level object passed in is re-used.
        /// The type of object is inferred from the object passed in.
        /// </summary>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <param name="intoObject">Object to assign values to in order to reconstruct the original object. Also determines the type of object expected from the XML.</param>
        /// <param name="options">Options.</param>
        public static void ReadXmlFileIntoObject(string filename, object intoObject, XmlClassifyOptions options = null)
        {
            var strRead = new StreamReader(filename, Encoding.UTF8);
            XElement elem = XElement.Load(strRead);
            strRead.Close();
            new classifier(options).XmlIntoObject(elem, intoObject, intoObject.GetType(), null);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="filename">Path and filename of the XML file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">Options.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename, XmlClassifyOptions options = null)
        {
            SaveObjectToXmlFile(saveObject, typeof(T), filename, options);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="saveType">Type of the object to store.</param>
        /// <param name="filename">Path and filename of the XML file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">Options.</param>
        public static void SaveObjectToXmlFile(object saveObject, Type saveType, string filename, XmlClassifyOptions options = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            var x = new classifier(options, defaultBaseDir).Classify(saveObject, saveType);
            PathUtil.CreatePathToFile(filename);
            x.Save(filename);
        }

        /// <summary>Converts the specified object into an XML tree.</summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="saveObject">Object to convert to an XML tree.</param>
        /// <param name="options">Options.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T saveObject, XmlClassifyOptions options = null)
        {
            return new classifier(options).Classify(saveObject, typeof(T));
        }

        /// <summary>Converts the specified object into an XML tree.</summary>
        /// <param name="saveType">Type of object to convert.</param>
        /// <param name="saveObject">Object to convert to an XML tree.</param>
        /// <param name="options">Options.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement(Type saveType, object saveObject, XmlClassifyOptions options = null)
        {
            return new classifier(options).Classify(saveObject, saveType);
        }

        private sealed class classifier
        {
            private XmlClassifyOptions _options;
            private int _nextId = 0;
            private List<Action> _doAtTheEnd;
            private string _baseDir;
            private string _rootElementName;

            public classifier(XmlClassifyOptions options = null, string defaultBaseDir = null)
            {
                _options = options ?? DefaultOptions ?? new XmlClassifyOptions(); // in case someone set default options to null
                _baseDir = _options.BaseDir ?? defaultBaseDir;
                _rootElementName = _options.RootElementName ?? "item";
            }

            private Dictionary<string, object> _rememberD
            {
                get
                {
                    if (_rememberCacheD == null)
                        _rememberCacheD = new Dictionary<string, object>();
                    return _rememberCacheD;
                }
            }
            private Dictionary<string, object> _rememberCacheD;

            private Dictionary<object, XElement> _rememberC
            {
                get
                {
                    if (_rememberCacheC == null)
                        _rememberCacheC = new Dictionary<object, XElement>(new CustomEqualityComparer<object>(object.ReferenceEquals, o => o.GetHashCode()));
                    return _rememberCacheC;
                }
            }
            private Dictionary<object, XElement> _rememberCacheC;

            private static Type[] _tupleTypes = new[] { typeof(KeyValuePair<,>), typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>) };

            private static bool isIntegerType(Type t)
            {
                return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);
            }

            public object Declassify(Type type, XElement elem, object parentNode = null)
            {
                _doAtTheEnd = new List<Action>();
                var result = declassify(type, elem, null, parentNode);
                foreach (var action in _doAtTheEnd)
                    action();
                return result;
            }

            private object declassify(Type type, XElement elem, object already, object parentNode)
            {
                object result;
                var originalType = type;
                var genericDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

                XmlClassifyTypeOptions typeOptions;
                if (_options._typeOptions.TryGetValue(type, out typeOptions) && typeOptions._substituteType != null)
                    type = typeOptions._substituteType;

                var processXml = typeOptions as IXmlClassifyProcessXml;
                if (processXml != null)
                    processXml.XmlPreprocess(elem);

                if (elem.Attribute("null") != null)
                    result = null;
                else if (type == typeof(XElement))
                    result = elem.Elements().FirstOrDefault();
                else if (type.IsEnum)
                    result = Enum.Parse(type, elem.Value);
                else if (type == typeof(string))
                {
                    if (elem.Attribute("encoding") != null)
                    {
                        if (elem.Attribute("encoding").Value == "c-literal")
                            result = elem.Value.CLiteralUnescape();
                        else if (elem.Attribute("encoding").Value == "base64")
                            result = elem.Value.Base64UrlDecode().FromUtf8();
                        else
                            throw new InvalidDataException("Encoding \"{0}\" is not recognized for elements of type \"string\"".Fmt(elem.Attribute("encoding")));
                    }
                    else
                        result = elem.Value;
                }
                else if (type == typeof(char))
                {
                    if (elem.Attribute("encoding") != null && elem.Attribute("encoding").Value == "codepoint")
                        result = (char) int.Parse(elem.Value);
                    else
                        result = elem.Value[0];
                }
                else if (ExactConvert.IsSupportedType(type))
                    result = ExactConvert.To(type, elem.Value);
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // It’s a nullable type, just determine the inner type and start again
                    result = declassify(type.GetGenericArguments()[0], elem, already, parentNode);
                }
                else if (genericDefinition != null && _tupleTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    // It’s a Tuple or KeyValuePair
                    var genericArguments = type.GetGenericArguments();
                    var tupleParams = new object[genericArguments.Length];
                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        var tagName = genericDefinition == typeof(KeyValuePair<,>) ? (i == 0 ? "key" : "value") : "item" + (i + 1);
                        var subElem = elem.Element(tagName);
                        if (subElem == null)
                            continue;
                        tupleParams[i] = declassify(genericArguments[i], subElem, null, parentNode);
                    }
                    var constructor = type.GetConstructor(genericArguments);
                    if (constructor == null)
                        throw new InvalidOperationException("Could not find expected Tuple constructor.");
                    result = constructor.Invoke(tupleParams);
                }
                else
                {
                    // Check if it’s an array, collection or dictionary
                    Type[] typeParameters;
                    Type keyType = null, valueType = null;
                    if (type.IsArray)
                        valueType = type.GetElementType();
                    else if (type.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                    {
                        keyType = typeParameters[0];
                        valueType = typeParameters[1];
                    }
                    else if (type.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        valueType = typeParameters[0];

                    if (valueType != null)
                    {
                        // It’s a collection or dictionary.

                        if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                            throw new InvalidOperationException("The field {0} is of a dictionary type, but its key type is {1}. Only string, integer types and enums are supported.".Fmt(elem.Name, keyType));

                        object outputList;
                        MethodInfo addMethod;

                        if (type.IsArray)
                        {
                            outputList = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elem.Elements("item").Count() });
                            addMethod = type.GetMethod("Set", new Type[] { typeof(int), valueType });
                        }
                        else if (keyType != null)
                        {
                            outputList = Activator.CreateInstance(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type);
                            addMethod = typeof(IDictionary<,>).MakeGenericType(keyType, valueType).GetMethod("Add", new Type[] { keyType, valueType });
                        }
                        else
                        {
                            outputList = Activator.CreateInstance(type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(ICollection<>) || type.GetGenericTypeDefinition() == typeof(IList<>)) ? typeof(List<>).MakeGenericType(valueType) : type);
                            addMethod = typeof(ICollection<>).MakeGenericType(valueType).GetMethod("Add", new Type[] { valueType });
                        }

                        int i = 0;
                        foreach (var itemTag in elem.Elements("item"))
                        {
                            object key = null, value = null;

                            // Reconstruct the key (if it’s a dictionary)
                            if (keyType != null)
                            {
                                var keyAttr = itemTag.Attribute("key");
                                try { key = isIntegerType(keyType) ? ExactConvert.To(keyType, keyAttr.Value) : keyType.IsEnum ? Enum.Parse(keyType, keyAttr.Value) : keyAttr.Value; }
                                catch { continue; }
                            }

                            var refAttr = itemTag.Attribute("ref");
                            if (refAttr != null)
                            {
                                var j = i;    // remember current value for the lambda
                                _doAtTheEnd.Add(() =>
                                {
                                    if (!_rememberD.ContainsKey(refAttr.Value))
                                        throw new InvalidOperationException(@"An element with the attribute ref=""{0}"" was encountered, but there is no matching element with the corresponding refid=""{0}"".".Fmt(refAttr.Value));
                                    if (type.IsArray)
                                        addMethod.Invoke(outputList, new object[] { i, _rememberD[refAttr.Value] });
                                    else if (keyType == null)
                                        addMethod.Invoke(outputList, new object[] { _rememberD[refAttr.Value] });
                                    else
                                        addMethod.Invoke(outputList, new object[] { key, _rememberD[refAttr.Value] });
                                });
                            }
                            else
                            {
                                var nullAttr = itemTag.Attribute("null");
                                if (nullAttr == null)
                                    value = declassify(valueType, itemTag, null, parentNode);
                                if (type.IsArray)
                                    addMethod.Invoke(outputList, new object[] { i, value });
                                else if (keyType == null)
                                    // Need to do this later so that all the items are in the right order when some items are refs and some aren’t
                                    _doAtTheEnd.Add(() => { addMethod.Invoke(outputList, new object[] { value }); });
                                else
                                    addMethod.Invoke(outputList, new object[] { key, value });
                            }
                            i++;
                        }
                        result = outputList;
                    }
                    else
                    {
                        // It’s NOT a collection or dictionary

                        object ret;

                        Type realType = type;
                        var typeAttr = elem.Attribute("type");
                        if (typeAttr != null)
                        {
                            var candidates = type.Assembly.GetTypes().Where(t => !t.IsGenericType && !t.IsNested && ((t.Namespace == type.Namespace && t.Name == typeAttr.Value) || t.FullName == typeAttr.Value)).ToArray();
                            if (candidates.Any())
                                realType = candidates.First();
                        }
                        else
                        {
                            typeAttr = elem.Attribute("fulltype");
                            var t = typeAttr != null ? Type.GetType(typeAttr.Value) : null;
                            if (t != null)
                                realType = t;
                        }

                        try
                        {
                            ret = already == null || already.GetType() != realType ? Activator.CreateInstance(realType, true) : already;
                        }
                        catch (Exception e)
                        {
                            throw new Exception("An object of type {0} could not be created:\n{1}".Fmt(realType.FullName, e.Message), e);
                        }

                        xmlIntoObject(elem, ret, realType, parentNode);
                        result = ret;
                    }
                }

                // Apply de-substitution (if any)
                if (originalType != type)
                    result = typeOptions._fromSubstitute(result);
                if (elem.Attribute("refid") != null)
                    _rememberD[elem.Attribute("refid").Value] = result;
                return result;
            }

            public void XmlIntoObject(XElement xml, object intoObject, Type type, object parentNode)
            {
                if (xml.Attribute("refid") != null)
                    _rememberD[xml.Attribute("refid").Value] = intoObject;
                xmlIntoObject(xml, intoObject, type, parentNode);
            }

            private void xmlIntoObject(XElement xml, object intoObject, Type type, object parentNode)
            {
                foreach (var fieldForeach in type.GetAllFields())
                {
                    var field = fieldForeach;   // lambda-inside-foreach bug workaround

                    string rFieldName = field.Name.TrimStart('_');
                    MemberInfo getAttrsFrom = field;

                    // Special case: compiler-generated fields for auto-implemented properties have a name that can’t be used as a tag name. Use the property name instead, which is probably what the user expects anyway
                    var m = Regex.Match(rFieldName, @"^<(.*)>k__BackingField$");
                    if (m.Success)
                    {
                        var prop = type.GetAllProperties().FirstOrDefault(p => p.Name == m.Groups[1].Value);
                        if (prop != null)
                        {
                            rFieldName = m.Groups[1].Value;
                            getAttrsFrom = prop;
                        }
                    }

                    // [XmlIgnore]
                    if (getAttrsFrom.IsDefined<XmlIgnoreAttribute>())
                        continue;

                    // [XmlParent]
                    else if (getAttrsFrom.IsDefined<XmlParentAttribute>())
                        field.SetValue(intoObject, parentNode);

                    // [XmlFollowId]
                    else if (getAttrsFrom.IsDefined<XmlFollowIdAttribute>())
                    {
                        if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                            throw new Exception("The field {0}.{1} uses the [XmlFollowId] attribute, but does not have the type XmlDeferredObject<T> for some T.".Fmt(type.FullName, field.Name));

                        Type innerType = field.FieldType.GetGenericArguments()[0];
                        var subElem = xml.Element(rFieldName);
                        if (subElem != null)
                        {
                            var attr = subElem.Attribute("id");
                            if (attr != null)
                            {
                                if (_baseDir == null)
                                    throw new InvalidOperationException(@"An object that uses [XmlFollowId] can only be reconstructed if a base directory is specified (see “BaseDir” in the XmlClassifyOptions class).");
                                string newFile = Path.Combine(_baseDir, innerType.Name, attr.Value + ".xml");
                                field.SetValue(intoObject,
                                    typeof(XmlDeferredObject<>).MakeGenericType(innerType)
                                        .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                        .Invoke(Ut.NewArray<object>(
                                            attr.Value /*id*/,
                                            typeof(XmlClassify).GetMethod("LoadObjectFromXmlFile", BindingFlags.Static | BindingFlags.NonPublic,
                                                null, new Type[] { typeof(Type), typeof(string), typeof(XmlClassifyOptions), typeof(object) }, null) /*generatorMethod*/,
                                            null /*generatorObject*/,
                                            new object[] { /*type*/ innerType, /*filename*/ newFile, /*options*/ _options, /*parent*/ intoObject } /*generatorParams*/
                                        ))
                                );
                            }
                        }
                    }

                    // Fields with no special [Xml...] attributes
                    else
                    {
                        var tag = xml.Elements(rFieldName).FirstOrDefault();
                        if (tag != null)
                        {
                            var refAttr = tag.Attribute("ref");
                            if (refAttr != null)
                                _doAtTheEnd.Add(() =>
                                {
                                    if (!_rememberD.ContainsKey(refAttr.Value))
                                        throw new InvalidOperationException(@"An element with the attribute ref=""{0}"" was encountered, but there is no matching element with the corresponding refid=""{0}"".".Fmt(refAttr.Value));
                                    field.SetValue(intoObject, _rememberD[refAttr.Value]);
                                });
                            else
                                field.SetValue(intoObject, declassify(field.FieldType, tag, field.GetValue(intoObject), intoObject));
                        }
                    }
                }

                if (intoObject is IXmlClassifyProcess)
                    _doAtTheEnd.Add(() => { ((IXmlClassifyProcess) intoObject).AfterXmlDeclassify(); });
            }

            public XElement Classify(object saveObject, Type declaredType, string tagName = null)
            {
                XElement elem = new XElement(tagName ?? _rootElementName);

                // Add a “type” attribute if the instance type is different from the field’s declared type
                Type saveType = declaredType;
                if (saveObject != null)
                {
                    saveType = saveObject.GetType();
                    if (saveType == typeof(IntPtr) || saveType == typeof(Pointer))
                        throw new NotSupportedException("XmlClassify does not support serializing values of type \"{0}\". Consider marking the offending field with [XmlIgnore].".Fmt(saveType));
                    if (declaredType != saveType && !(saveType.IsValueType && declaredType == typeof(Nullable<>).MakeGenericType(saveType)))
                    {
                        // ... but only add this attribute if it is not a collection, because then XmlClassify doesn’t care about the “type” attribute when restoring the object from XML anyway
                        Type[] typeParameters;
                        if (!declaredType.IsArray && !declaredType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters) && !declaredType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        {
                            if (saveType.Assembly.Equals(declaredType.Assembly) && !saveType.IsGenericType && !saveType.IsNested)
                            {
                                if (saveType.Namespace.Equals(declaredType.Namespace))
                                    elem.Add(new XAttribute("type", saveType.Name));
                                else
                                    elem.Add(new XAttribute("type", saveType.FullName));
                            }
                            else
                                elem.Add(new XAttribute("fulltype", saveType.AssemblyQualifiedName));
                        }
                    }
                }

                // See if there's a substitute type defined
                XmlClassifyTypeOptions typeOptions;
                var originalObject = saveObject;
                if (_options._typeOptions.TryGetValue(saveType, out typeOptions) && typeOptions._substituteType != null)
                {
                    saveObject = typeOptions._toSubstitute(saveObject);
                    saveType = typeOptions._substituteType;
                }

                if (saveObject == null)
                {
                    elem.Add(new XAttribute("null", 1));
                    return elem;
                }

                // Preserve reference identity of reference types except string
                if (!(originalObject is ValueType) && !(originalObject is string))
                {
                    if (_rememberC.ContainsKey(originalObject))
                    {
                        var attr = _rememberC[originalObject].Attribute("refid");
                        if (attr == null)
                        {
                            attr = new XAttribute("refid", _nextId.ToString());
                            _nextId++;
                            _rememberC[originalObject].Add(attr);
                        }
                        elem.Add(new XAttribute("ref", attr.Value));
                        return elem;
                    }
                    _rememberC.Add(originalObject, elem);
                }

                if (saveType == typeof(XElement))
                    elem.Add(new XElement(saveObject as XElement));
                else if (saveType == typeof(string))
                {
                    string str = (string) saveObject;
                    if (str.Any(ch => ch < ' '))
                    {
                        elem.Add(new XAttribute("encoding", "c-literal"));
                        elem.Add(str.CLiteralEscape());
                    }
                    else
                        elem.Add(str);
                }
                else if (saveType == typeof(char))
                {
                    char ch = (char) saveObject;
                    if (ch <= ' ')
                    {
                        elem.Add(new XAttribute("encoding", "codepoint"));
                        elem.Add((int) ch);
                    }
                    else
                        elem.Add(ch.ToString());
                }
                else if (saveType.IsEnum)
                    elem.Add(saveObject.ToString());
                else if (ExactConvert.IsSupportedType(saveType))
                {
                    string result;
                    ExactConvert.To(saveObject, out result);
                    elem.Add(result);
                }
                else
                {
                    if (declaredType.IsInterface && declaredType.GetGenericTypeDefinition() != typeof(ICollection<>) && declaredType.GetGenericTypeDefinition() != typeof(IList<>) && declaredType.GetGenericTypeDefinition() != typeof(IDictionary<,>))
                        throw new InvalidOperationException("The field {0} is of an interface type, but not ICollection<T>, IList<T> or IDictionary<TKey, TValue>. Those are the only interface types supported.".Fmt(tagName));

                    // Tuples and KeyValuePairs
                    var genericDefinition = saveType.IsGenericType ? saveType.GetGenericTypeDefinition() : null;
                    if (genericDefinition != null && _tupleTypes.Contains(genericDefinition))
                    {
                        var genericArguments = saveType.GetGenericArguments();
                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            var propertyName = genericDefinition == typeof(KeyValuePair<,>) ? (i == 0 ? "Key" : "Value") : "Item" + (i + 1);
                            var property = saveType.GetProperty(propertyName);
                            if (property == null)
                                throw new InvalidOperationException("Cannot find expected item property in Tuple type.");
                            elem.Add(Classify(property.GetValue(saveObject, null), genericArguments[i], propertyName.ToLowerInvariant()));
                        }
                        return elem;
                    }

                    if (saveObject is IXmlClassifyProcess)
                        ((IXmlClassifyProcess) saveObject).BeforeXmlClassify();

                    // Arrays, collections, dictionaries
                    Type keyType = null, valueType = null;
                    Type[] typeParameters;

                    if (declaredType.IsArray)
                        valueType = declaredType.GetElementType();
                    else if (declaredType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                    {
                        keyType = typeParameters[0];
                        valueType = typeParameters[1];
                    }
                    else if (declaredType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        valueType = typeParameters[0];

                    if (valueType != null)
                    {
                        if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                            throw new InvalidOperationException("The field {0} is of a dictionary type, but its key type is {1}. Only string, integer types and enums are supported.".Fmt(tagName, keyType.FullName));

                        IEnumerator enumerator = null;
                        try
                        {
                            enumerator = (IEnumerator) typeof(IEnumerable).GetMethod("GetEnumerator", new Type[] { }).Invoke(saveObject, new object[] { });
                            var kvpType = keyType == null ? null : typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                            var kvpKey = keyType == null ? null : kvpType.GetProperty("Key");
                            var kvpValue = keyType == null ? null : kvpType.GetProperty("Value");
                            while (enumerator.MoveNext())
                            {
                                var value = keyType == null ? enumerator.Current : kvpValue.GetValue(enumerator.Current, null);
                                var tag = Classify(value, valueType, "item");
                                if (keyType != null)
                                    tag.Add(new XAttribute("key", kvpKey.GetValue(enumerator.Current, null).ToString()));
                                elem.Add(tag);
                            }
                        }
                        finally
                        {
                            if (enumerator != null && enumerator is IDisposable)
                                ((IDisposable) enumerator).Dispose();
                        }
                    }
                    else
                    {
                        bool ignoreIfDefaultOnType = saveType.IsDefined<XmlIgnoreIfDefaultAttribute>(true);
                        bool ignoreIfEmptyOnType = saveType.IsDefined<XmlIgnoreIfEmptyAttribute>(true);

                        foreach (var field in saveType.GetAllFields())
                        {
                            if (field.FieldType == saveType && saveType.IsValueType)
                                throw new InvalidOperationException(@"Cannot serialize an instance of the type {0} because it is a value type that contains itself.".Fmt(saveType.FullName));

                            // Ignore the backing field for events
                            if (typeof(Delegate).IsAssignableFrom(field.FieldType) && saveType.GetEvent(field.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) != null)
                                continue;

                            string rFieldName = field.Name.TrimStart('_');
                            MemberInfo getAttrsFrom = field;

                            // Special case: compiler-generated fields for auto-implemented properties have a name that can’t be used as a tag name. Use the property name instead, which is probably what the user expects anyway
                            var m = Regex.Match(field.Name, @"^<(.*)>k__BackingField$");
                            if (m.Success)
                            {
                                var prop = saveType.GetAllProperties().FirstOrDefault(p => p.Name == m.Groups[1].Value);
                                if (prop != null)
                                {
                                    rFieldName = m.Groups[1].Value;
                                    getAttrsFrom = prop;
                                }
                            }

                            // [XmlIgnore], [XmlParent]
                            if (getAttrsFrom.IsDefined<XmlIgnoreAttribute>() || getAttrsFrom.IsDefined<XmlParentAttribute>())
                                continue;

                            else
                            {
                                object saveValue = field.GetValue(saveObject);
                                bool ignoreIfDefault = ignoreIfDefaultOnType || getAttrsFrom.IsDefined<XmlIgnoreIfDefaultAttribute>(true);

                                if (ignoreIfDefault)
                                {
                                    if (saveValue == null)
                                        continue;
                                    if (saveValue.GetType().IsValueType &&
                                        !(field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) &&
                                        saveValue.Equals(Activator.CreateInstance(saveValue.GetType())))
                                        continue;
                                }

                                var def = getAttrsFrom.GetCustomAttributes<XmlIgnoreIfAttribute>(true).FirstOrDefault();
                                if (def != null && saveValue != null && saveValue.Equals(def.Value))
                                    continue;

                                // Arrays, List<>, and Dictionary<,> all implement ICollection
                                bool ignoreIfEmpty = ignoreIfEmptyOnType || getAttrsFrom.IsDefined<XmlIgnoreIfEmptyAttribute>(true);
                                if (saveValue != null && ignoreIfEmpty && saveValue is ICollection && ((ICollection) saveValue).Count == 0)
                                    continue;

                                // [XmlFollowId]
                                if (getAttrsFrom.IsDefined<XmlFollowIdAttribute>())
                                {
                                    if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                                        throw new InvalidOperationException("A field that uses the [XmlFollowId] attribute must have the type XmlDeferredObject<T> for some T.");

                                    Type innerType = field.FieldType.GetGenericArguments()[0];
                                    string id = (string) field.FieldType.GetProperty("Id").GetValue(saveValue, null);
                                    elem.Add(new XElement(rFieldName, new XAttribute("id", id)));

                                    if ((bool) field.FieldType.GetProperty("Evaluated").GetValue(saveValue, null))
                                    {
                                        if (_baseDir == null)
                                            throw new InvalidOperationException(@"An object that uses [XmlFollowId] can only be stored if a base directory is specified (see “BaseDir” in the XmlClassifyOptions class).");
                                        var prop = field.FieldType.GetProperty("Value");
                                        SaveObjectToXmlFile(prop.GetValue(saveValue, null), prop.PropertyType, Path.Combine(_baseDir, innerType.Name, id + ".xml"), _options);
                                    }
                                }
                                else
                                {
                                    var xelem = Classify(saveValue, field.FieldType, rFieldName);
                                    if (xelem.HasAttributes || xelem.HasElements || xelem.FirstNode != null || !ignoreIfEmpty)
                                        elem.Add(xelem);
                                }
                            }
                        }
                    }

                    var processXml = typeOptions as IXmlClassifyProcessXml;
                    if (processXml != null)
                        processXml.XmlPostprocess(elem);
                }

                return elem;
            }
        }

        #region Post-build step check

        /// <summary>Performs safety checks to ensure that a specific type doesn't cause XmlClassify exceptions. Note that this doesn't guarantee that the data is preserved correctly.
        /// Run this method as a post-build step to ensure reliability of execution. For an example of use, see <see cref="Ut.RunPostBuildChecks"/>. This method is available only in DEBUG mode.</summary>
        /// <typeparam name="T">The type that must be XmlClassify-able.</typeparam>
        /// <param name="rep">Object to report post-build errors to.</param>
        public static void PostBuildStep<T>(IPostBuildReporter rep)
        {
            PostBuildStep(typeof(T), rep);
        }

        /// <summary>Performs safety checks to ensure that a specific type doesn't cause XmlClassify exceptions. Note that this doesn't guarantee that the data is preserved correctly.
        /// Run this method as a post-build step to ensure reliability of execution. For an example of use, see <see cref="Ut.RunPostBuildChecks"/>. This method is available only in DEBUG mode.</summary>
        /// <param name="type">The type that must be XmlClassify-able.</param>
        /// <param name="rep">Object to report post-build errors to.</param>
        public static void PostBuildStep(Type type, IPostBuildReporter rep)
        {
            object obj;
            try
            {
                obj = Activator.CreateInstance(type, nonPublic: true);
            }
            catch (Exception e)
            {
                rep.Error("Unable to instantiate type {0}, required by XmlClassify. Check that it has a parameterless constructor and the constructor doesn't throw. Details: {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
            XElement xel;
            try
            {
                xel = ObjectToXElement(type, obj);
            }
            catch (Exception e)
            {
                rep.Error("Unable to XmlClassify type {0}. {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
            try
            {
                ObjectFromXElement(type, xel);
            }
            catch (Exception e)
            {
                rep.Error("Unable to de-XmlClassify type {0}. {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
        }

        #endregion
    }

    /// <summary>
    /// Contains methods to process an object before <see cref="XmlClassify"/> turns it into XML or after it has restored it from XML.
    /// To have effect, this interface must be implemented by the object being serialised.
    /// </summary>
    public interface IXmlClassifyProcess
    {
        /// <summary>Pre-processes this object before <see cref="XmlClassify"/> turns it into XML.
        /// This method is automatically invoked by <see cref="XmlClassify"/> and should not be called directly.</summary>
        void BeforeXmlClassify();

        /// <summary>Post-processes this object after <see cref="XmlClassify"/> has restored it from XML.
        /// This method is automatically invoked by <see cref="XmlClassify"/> and should not be called directly.</summary>
        void AfterXmlDeclassify();
    }

    /// <summary>
    /// Allows a class to preprocess XML before declassifying, or postprocess the XML generated by XmlClassify after classifying.
    /// To have effect, this interface must be implemented by a class derived from <see cref="XmlClassifyTypeOptions"/>
    /// and associated with a type via <see cref="XmlClassifyOptions.AddTypeOptions"/>.
    /// </summary>
    public interface IXmlClassifyProcessXml
    {
        /// <summary>Pre-processes the provided XML before declassifying.</summary>
        /// <param name="xml">XML to preprocess.</param>
        void XmlPreprocess(XElement xml);
        /// <summary>Post-processes the generated XML after classifying.</summary>
        /// <param name="xml">XML to postprocess.</param>
        void XmlPostprocess(XElement xml);
    }

    /// <summary>Implement this interface in a subclass of <see cref="XmlClassifyTypeOptions"/> to specify how to substitute a type for another type during XmlClassify.</summary>
    /// <typeparam name="TTrue">The type that is actually used for instances in memory.</typeparam>
    /// <typeparam name="TSubstitute">The substitute type to be used for purposes of classifying and declassifying.</typeparam>
    public interface IXmlClassifySubstitute<TTrue, TSubstitute>
    {
        /// <summary>Converts an instance of the “real” type to a substitute instance to be classified.</summary>
        /// <param name="instance">An instance of the “real” type to be substituted.</param>
        /// <returns>The converted object to use in classifying.</returns>
        TSubstitute ToSubstitute(TTrue instance);
        /// <summary>Converts a substitute instance, generated by declassifying, back to the “real” type.</summary>
        /// <param name="instance">An instance of the substituted type, provided by XmlClassify.</param>
        /// <returns>The converted object to put into the real type.</returns>
        TTrue FromSubstitute(TSubstitute instance);
    }

    /// <summary>Specifies some options for use in XmlClassify.</summary>
    public sealed class XmlClassifyOptions
    {
        /// <summary>
        /// The base directory from which to construct the paths for additional XML files whenever a field has
        /// an <see cref="XmlFollowIdAttribute"/> attribute. Inferred automatically from filename if null.
        /// </summary>
        public string BaseDir = null;

        /// <summary>The name of the root element for classified objects.</summary>
        public string RootElementName = "item";

        internal Dictionary<Type, XmlClassifyTypeOptions> _typeOptions = new Dictionary<Type, XmlClassifyTypeOptions>();

        /// <summary>Adds options that are relevant to classifying/declassifying a specific type.</summary>
        /// <param name="type">The type to which these options apply.</param>
        /// <param name="options">Options that apply to the <paramref name="type"/>. To enable type substitution,
        /// pass an instance of a class that implements <see cref="IXmlClassifySubstitute{TTrue,TSubstitute}"/>.
        /// To use XML pre-/post-processing, pass an instance of a class that implements <see cref="IXmlClassifyProcessXml"/>.</param>
        /// <returns>Itself.</returns>
        public XmlClassifyOptions AddTypeOptions(Type type, XmlClassifyTypeOptions options)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (options == null) throw new ArgumentNullException("options");
            if (_typeOptions.ContainsKey(type)) throw new ArgumentException("XmlClassify options for type {0} have already been defined.".Fmt(type), "type");
            if (_typeOptions.Values.Contains(options)) throw new ArgumentException("Must use a different XmlClassifyTypeOptions instance for every type.", "options");
            options.initializeFor(type);
            _typeOptions.Add(type, options);
            return this;
        }
    }

    /// <summary>Provides an abstract base type to derive from to specify type-specific options for use in XmlClassify. See remarks for more information.</summary>
    /// <remarks>
    /// <para>Derive from this type and implement <see cref="IXmlClassifySubstitute{TTrue,TSubstitute}"/> to enable type substitution during XmlClassify.</para>
    /// <para>Derive from this type and implement <see cref="IXmlClassifyProcessXml"/> to pre-/post-process the XML before/after XmlClassify.</para>
    /// <para>Instances of derived classes are passed into <see cref="XmlClassifyOptions.AddTypeOptions"/>.</para>
    /// </remarks>
    public abstract class XmlClassifyTypeOptions
    {
        internal Type _substituteType;
        internal Func<object, object> _toSubstitute;
        internal Func<object, object> _fromSubstitute;

        internal void initializeFor(Type type)
        {
            var substInterfaces = GetType().GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IXmlClassifySubstitute<,>) && t.GetGenericArguments()[0] == type).ToArray();
            if (substInterfaces.Length > 1)
                throw new ArgumentException("The type {0} implements more than one IXmlClassifySubstitute<{1}, *> interface. Expected at most one.".Fmt(GetType(), type));
            else if (substInterfaces.Length == 1)
            {
                _substituteType = substInterfaces[0].GetGenericArguments()[1];
                if (type == _substituteType)
                    throw new InvalidOperationException("The type {0} implements a substitution from type {1} to itself.".Fmt(GetType(), type));
                var toSubstMethod = substInterfaces[0].GetMethod("ToSubstitute");
                var fromSubstMethod = substInterfaces[0].GetMethod("FromSubstitute");
                _toSubstitute = obj =>
                {
                    var result = toSubstMethod.Invoke(this, new[] { obj });
                    if (result != null && result.GetType() != _substituteType) // forbidden just in case because I see no use cases for returning a subtype
                        throw new InvalidOperationException("The method {0} is expected to return an instance of the substitute type, {1}. It returned a subtype, {2}.".Fmt(toSubstMethod, _substituteType, result.GetType()));
                    return result;
                };
                _fromSubstitute = obj =>
                {
                    var result = fromSubstMethod.Invoke(this, new[] { obj });
                    if (result != null && result.GetType() != type) // forbidden just in case because I see no use cases for returning a subtype
                        throw new InvalidOperationException("The method {0} is expected to return an instance of the true type, {1}. It returned a subtype, {2}.".Fmt(fromSubstMethod, type, result.GetType()));
                    return result;
                };
            }
        }
    }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> stores an ID in the corresponding XML tag that points to another, separate 
    /// XML file which in turn contains the actual object for this field or automatically-implemented property. This is only allowed on fields or automatically-implemented properties of type 
    /// <see cref="XmlDeferredObject&lt;T&gt;"/> for some type T. Use <see cref="XmlDeferredObject&lt;T&gt;.Value"/> to retrieve the object. This retrieval is deferred until first use. 
    /// Use <see cref="XmlDeferredObject&lt;T&gt;.Id"/> to retrieve the ID used to reference the object. You can also capture the ID into the class or struct T by using the 
    /// <see cref="XmlIdAttribute"/> attribute within that class or struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class XmlFollowIdAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, it is ignored by <see cref="XmlClassify"/>.
    /// Data stored in this field or automatically-implemented property is not persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class XmlIgnoreAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> does
    /// not generate a tag if the value is null, 0, false, etc. If it is used on a class or struct, it applies to all fields and
    /// automatically-implemented properties in the class or struct. Notice that using this together with
    /// <see cref="XmlIgnoreIfEmptyAttribute"/> will cause the distinction between null and an empty element
    /// to be lost. However, a collection containing only null elements is persisted correctly.
    /// </summary>
    /// <remarks>Warning: Do not use this custom attribute on a field that has a non-default value set in the
    /// containing class’s constructor. Doing so will cause a serialised “null” to revert to that constructor value
    /// upon deserliasation.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class XmlIgnoreIfDefaultAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> does
    /// not generate a tag if that tag would be completely empty (no attributes or subelements). If it is used on
    /// a class or struct, it applies to all collection-type fields in the class or struct. Notice that using this together
    /// with <see cref="XmlIgnoreIfDefaultAttribute"/> will cause the distinction between null and an empty
    /// element to be lost. However, a collection containing only null elements is persisted correctly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class XmlIgnoreIfEmptyAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> does not generate a tag if the field’s or property’s value is equal to the specified value.
    /// Notice that using this together with <see cref="XmlIgnoreIfDefaultAttribute"/> will cause the distinction between the type’s default value and the specified value to be lost.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class XmlIgnoreIfAttribute : Attribute
    {
        private object _value;
        /// <summary>Constructs an <see cref="XmlIgnoreIfAttribute"/> instance.</summary>
        /// <param name="value"></param>
        public XmlIgnoreIfAttribute(object value) { _value = value; }
        /// <summary>Retrieves the value which causes a field or automatically-implemented property to be ignored.</summary>
        public object Value { get { return _value; } }
    }

    /// <summary>
    /// When reconstructing persisted objects using <see cref="XmlClassify"/>, a field or automatically-implemented property with this attribute receives a reference to the object which was
    /// its parent node in the XML tree. If the field or automatically-implemented property is of an incompatible type, a run-time exception occurs. If there was no parent node, the field or 
    /// automatically-implemented property is set to null. When persisting objects, fields and automatically-implemented properties with this attribute are skipped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class XmlParentAttribute : Attribute { }

    /// <summary>
    /// When reconstructing persisted objects using <see cref="XmlClassify"/>, a field or automatically-implemented property with this attribute receives the ID that was used to refer to the
    /// XML file that stores this object.  See <see cref="XmlFollowIdAttribute"/> for more information. The field or automatically-implemented property must be of type string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class XmlIdAttribute : Attribute { }

    /// <summary>
    /// Provides mechanisms to hold an object that has an ID and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public sealed class XmlDeferredObject<T>
    {
        /// <summary>Initialises a deferred object using a delegate or lambda expression.</summary>
        /// <param name="id">Id that refers to the object to be generated.</param>
        /// <param name="generator">Function to generate the object.</param>
        public XmlDeferredObject(string id, Func<T> generator) { _id = id; _generator = generator; }

        /// <summary>Initialises a deferred object using an actual object. Evaluation is not deferred.</summary>
        /// <param name="id">Id that refers to the object.</param>
        /// <param name="value">The object to store.</param>
        public XmlDeferredObject(string id, T value) { _id = id; _cached = value; _haveCache = true; }

        /// <summary>Initialises a deferred object using a method reference and an array of parameters.</summary>
        /// <param name="id">ID that refers to the object to be generated.</param>
        /// <param name="generatorMethod">Reference to a method that generates the object.</param>
        /// <param name="generatorObject">Object on which the method should be invoked. Use null for static methods.</param>
        /// <param name="generatorParams">Set of parameters for the method invocation.</param>
        public XmlDeferredObject(string id, MethodInfo generatorMethod, object generatorObject, object[] generatorParams)
        {
            _id = id;
            _generator = () => (T) generatorMethod.Invoke(generatorObject, generatorParams);
        }

        private Func<T> _generator;
        private T _cached;
        private bool _haveCache = false;
        private string _id;

        /// <summary>
        /// Gets or sets the object stored in this <see cref="XmlDeferredObject&lt;T&gt;"/>. The property getter causes the object to be evaluated when called. 
        /// The setter overrides the object with a pre-computed object whose evaluation is not deferred.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_haveCache)
                {
                    _cached = _generator();
                    // Update any field in the class that has an [XmlId] attribute and is of type string.
                    foreach (var field in _cached.GetType().GetAllFields().Where(fld => fld.FieldType == typeof(string) && fld.IsDefined<XmlIdAttribute>()))
                        field.SetValue(_cached, _id);
                    _haveCache = true;
                }
                return _cached;
            }
            set
            {
                _cached = value;
                _haveCache = true;
            }
        }

        /// <summary>Determines whether the object has been computed.</summary>
        public bool Evaluated { get { return _haveCache; } }

        /// <summary>Returns the ID used to refer to the object.</summary>
        public string Id { get { return _id; } }
    }
}
