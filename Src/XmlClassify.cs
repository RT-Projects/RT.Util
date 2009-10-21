using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;
using System.Text.RegularExpressions;

namespace RT.Util.Xml
{
    /// <summary>
    /// Provides static methods to save objects of (almost) arbitrary classes into XML files and load them again.
    /// See the remarks section for features and limitations.
    /// </summary>
    /// <remarks>
    /// <para>By default, XmlClassify persists the value of all instance fields, including private, inherited and compiler-generated ones. It does not persist static members or the result of property getters.
    ///    Each field is persisted in an XML tag whose name is the field's name minus any leading underscores. Compiler-generated fields for automatically-implemented properties are
    ///    instead persisted in an XML tag whose name is the automatically-implemented property's name minus any leading underscores.</para>
    /// <para>Features:</para>
    /// <list type="bullet">
    /// <item><description>XmlClassify fully supports all the built-in types which are keywords in C# except 'object'. It also supports DateTime.</description></item>
    /// <item><description>XmlClassify fully supports classes and structs that contain only fields of the above types as well as fields whose type is itself such a class or struct.</description></item>
    /// <item><description>XmlClassify has special handling for classes that implement IDictionary&lt;K, V&gt;, where V must be a type also supported by XmlClassify. K must be string, an integer type, or an enum type.
    ///    If the field is of a concrete type, that type is maintained.
    ///    If the field is of the interface type IDictionary&lt;K, V&gt; itself, the type Dictionary&lt;K, V&gt; is used to reconstruct the object.</description></item>
    /// <item><description>XmlClassify has special handling for classes that implement ICollection&lt;T&gt;, where T must be a type also supported by XmlClassify.
    ///    If the field is of a concrete type, that type is maintained.
    ///    If the field is of the interface type ICollection&lt;T&gt; itself, the type List&lt;T&gt; is used to reconstruct the object.
    ///    If the type also implements IDictionary&lt;K, V&gt;, the special handling for that takes precedence.</description></item>
    /// <item><description>For classes that don't implement any of the above-mentioned interfaces, XmlClassify supports polymorphism. The actual type of an instance is persisted if it is different from the declared type.</description></item>
    /// <item><description>XmlClassify supports auto-generated properties. The XML tag's name is the name of the property rather than the hidden auto-generated field, although the field's value is persisted.
    ///    All other properties are ignored.</description></item>
    /// <item><description>XmlClassify ignores the order of XML tags (except when handling collections and dictionaries). It uses tag names to identify which tag belongs to which field.</description></item>
    /// <item><description>XmlClassify silently discards unrecognised XML tags instead of throwing errors. This is by design because it enables the programmer to remove a field from a class without invalidating previously-saved XML files.</description></item>
    /// <item><description>XmlClassify silently ignores missing XML tags. A field whose XML tag is missing retains the value assigned to it by the parameterless constructor.
    ///    This is by design because it enables the programmer to add a new field to a class (and specifying a default initialisation value for it) without invalidating previously-saved XML files.</description></item>
    /// <item><description>The following custom attributes can be used to alter XmlClassify's behaviour. See the custom attribute class's documentation for more information:
    ///    <see cref="XmlFollowIdAttribute"/>, <see cref="XmlIdAttribute"/>, <see cref="XmlIgnoreAttribute"/>, <see cref="XmlIgnoreIfAttribute"/>,
    ///    <see cref="XmlIgnoreIfDefaultAttribute"/>, <see cref="XmlIgnoreIfEmptyAttribute"/>, <see cref="XmlParentAttribute"/>. If an attribute can be used on a field, it can equally well be used on an auto-generated property,
    ///    but not on any other properties.
    /// </description></item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item><description>XmlClassify requires that the type have a parameterless constructor, although it need not be public. This parameterless constructor is executed with all its side-effects before the object is reconstructed.</description></item>
    /// <item><description>XmlClassify does not handle cycles in the object graph (this causes a stack overflow).</description></item>
    /// <item><description>If the object graph is an acyclic graph but not a tree, object identity is lost. The reconstructed graph contains multiple copies of the objects that are referenced multiple times.</description></item>
    /// <item><description>If a field is of type ICollection&lt;T&gt;, IDictionary&lt;K, V&gt;, or any class that implements either of these, polymorphism is not supported, and nor is any information stored in those classes.
    ///    In particular, this means that the comparer used by a SortedDictionary&lt;K, V&gt; is not persisted. A comparer assigned by the class's parameterless constructor is also not used.</description></item>
    /// </list>
    /// </remarks>
    public static class XmlClassify
    {
        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string filename) where T : new()
        {
            return LoadObjectFromXmlFile<T>(filename, null);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <param name="parentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it receives the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string filename, object parentNode) where T : new()
        {
            string BaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            return LoadObjectFromXmlFile<T>(filename, BaseDir, parentNode);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="filename">Path and filename of the XML file to read from.</param>
        /// <param name="baseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="parentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it receives the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string filename, string baseDir, object parentNode) where T : new()
        {
            return (T) loadObjectFromXmlFile(typeof(T), filename, baseDir, parentNode);
        }

        private static object loadObjectFromXmlFile(Type type, string filename, string baseDir, object parentNode)
        {
            var strRead = new StreamReader(filename, Encoding.UTF8);
            XElement elem = XElement.Load(strRead);
            strRead.Close();
            return objectFromXElement(type, elem, baseDir, parentNode);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem)
        {
            return (T) objectFromXElement(typeof(T), elem, null, null);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <param name="type">Type of the object to reconstruct.</param>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static object ObjectFromXElement(Type type, XElement elem)
        {
            return objectFromXElement(type, elem, null, null);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <param name="baseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem, string baseDir)
        {
            return (T) objectFromXElement(typeof(T), elem, baseDir, null);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <param name="baseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="parentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it receives the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem, string baseDir, object parentNode)
        {
            return (T) objectFromXElement(typeof(T), elem, baseDir, parentNode);
        }

        private static object objectFromXElement(Type type, XElement elem, string baseDir, object parentNode)
        {
            if (elem.Attribute("null") != null)
                return null;
            else if (type == typeof(XElement))
                return elem.Elements().FirstOrDefault();
            else if (type.IsEnum)
                return Enum.Parse(type, elem.Value);
            else if (type == typeof(string))
            {
                if (elem.Attribute("encoding") != null && elem.Attribute("encoding").Value == "base64")
                    return elem.Value.Base64UrlDecode().FromUtf8();
                return elem.Value;
            }
            else if (type == typeof(char))
            {
                if (elem.Attribute("encoding") != null && elem.Attribute("encoding").Value == "codepoint")
                    return (char) int.Parse(elem.Value);
                return elem.Value[0];
            }
            else if (RConvert.IsSupportedType(type))
                return RConvert.Exact(type, elem.Value);
            else
            {
                Type[] typeParameters;

                // If it's a nullable type, just determine the inner type and start again
                if (type.TryGetInterfaceGenericParameters(typeof(Nullable<>), out typeParameters))
                    return objectFromXElement(typeParameters[0], elem, baseDir, parentNode);

                // Check if it's an array, collection or dictionary
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
                    if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                        throw new Exception("The field {0} is of a dictionary type, but its key type is {1}. Only string, integer types and enums are supported.".Fmt(elem.Name, keyType));

                    object outputList;
                    if (type.IsArray)
                        outputList = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elem.Elements("item").Count() });
                    else if (type.GetGenericTypeDefinition() == typeof(ICollection<>))
                        outputList = Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType));
                    else if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        outputList = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
                    else
                        outputList = Activator.CreateInstance(type);

                    var addMethod = type.IsArray
                        ? type.GetMethod("Set", new Type[] { typeof(int), valueType })
                        : keyType == null
                            ? type.GetMethod("Add", new Type[] { valueType })
                            : type.GetMethod("Add", new Type[] { keyType, valueType });

                    int i = 0;
                    foreach (var itemTag in elem.Elements("item"))
                    {
                        object key = null, value = null;
                        if (keyType != null)
                        {
                            var keyAttr = itemTag.Attribute("key");
                            try { key = isIntegerType(keyType) ? RConvert.Exact(keyType, keyAttr.Value) : keyType.IsEnum ? Enum.Parse(keyType, keyAttr.Value) : keyAttr.Value; }
                            catch { continue; }
                        }
                        var nullAttr = itemTag.Attribute("null");
                        if (nullAttr == null)
                            value = objectFromXElement(valueType, itemTag, baseDir, parentNode);
                        if (type.IsArray)
                            addMethod.Invoke(outputList, new object[] { i++, value });
                        else if (keyType == null)
                            addMethod.Invoke(outputList, new object[] { value });
                        else
                            addMethod.Invoke(outputList, new object[] { key, value });
                    }
                    return outputList;
                }
                else
                {
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
                        ret = Activator.CreateInstance(realType, true);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("An object of type {0} could not be created:\n{1}".Fmt(realType.FullName, e.Message), e);
                    }

                    foreach (var field in realType.GetAllFields())
                    {
                        string rFieldName = field.Name.TrimStart('_');
                        MemberInfo getAttrsFrom = field;

                        // Special case: compiler-generated fields for auto-implemented properties have a name that can't be used as a tag name. Use the property name instead, which is probably what the user expects anyway
                        var m = Regex.Match(rFieldName, @"^<(.*)>k__BackingField$");
                        if (m.Success)
                        {
                            var prop = realType.GetAllProperties().FirstOrDefault(p => p.Name == m.Groups[1].Value);
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
                            field.SetValue(ret, parentNode);

                        // [XmlFollowId]
                        else if (getAttrsFrom.IsDefined<XmlFollowIdAttribute>())
                        {
                            if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                                throw new Exception("The field {0}.{1} uses the [XmlFollowId] attribute, but does not have the type XmlDeferredObject<T> for some T.".Fmt(realType.FullName, field.Name));

                            Type innerType = field.FieldType.GetGenericArguments()[0];
                            var subElem = elem.Element(rFieldName);
                            if (subElem != null)
                            {
                                var attr = subElem.Attribute("id");
                                if (attr != null)
                                {
                                    string newFile = Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + attr.Value + ".xml");
                                    field.SetValue(ret,
                                         typeof(XmlDeferredObject<>).MakeGenericType(innerType)
                                             .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                             .Invoke(new object[] { 
                                                 attr.Value,
                                                 typeof(XmlClassify).GetMethod("loadObjectFromXmlFile", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Type), typeof(string), typeof(string), typeof(object) }, null),
                                                 null,
                                                 new object[] { innerType, newFile, baseDir, ret }
                                            })
                                     );
                                }
                            }
                        }

                        // Fields with no special [Xml...] attributes
                        else
                        {
                            var tag = elem.Elements(rFieldName);
                            if (tag.Any())
                                field.SetValue(ret, objectFromXElement(field.FieldType, tag.First(), baseDir, ret));
                        }
                    }
                    return ret;
                }
            }
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="filename">Path and filename of the XML file to be created. If the file already exists, it is overwritten.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename)
        {
            string baseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            saveObjectToXmlFile(saveObject, typeof(T), filename, baseDir);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="filename">Path and filename of the XML file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="baseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename, string baseDir)
        {
            saveObjectToXmlFile(saveObject, typeof(T), filename, baseDir);
        }

        private static void saveObjectToXmlFile(object saveObject, Type declaredType, string filename, string baseDir)
        {
            var x = objectToXElement(saveObject, declaredType, baseDir, "item");
            PathUtil.CreatePathToFile(filename);
            x.Save(filename);
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="saveObject">Object to convert to an XML tree.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T saveObject)
        {
            return objectToXElement(saveObject, typeof(T), null, "item");
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="saveObject">Object to convert to an XML tree.</param>
        /// <param name="baseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T saveObject, string baseDir)
        {
            return objectToXElement(saveObject, typeof(T), baseDir, "item");
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="saveObject">Object to convert to an XML tree.</param>
        /// <param name="baseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="tagName">Name of the top-level XML tag to use for this object.
        /// Default is "item".</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T saveObject, string baseDir, string tagName)
        {
            return objectToXElement(saveObject, typeof(T), baseDir, tagName);
        }

        private static XElement objectToXElement(object saveObject, Type declaredType, string baseDir, string tagName)
        {
            XElement elem = new XElement(tagName);

            if (saveObject == null)
            {
                elem.Add(new XAttribute("null", 1));
                return elem;
            }

            Type saveType = saveObject.GetType();

            if (saveType == typeof(XElement))
                elem.Add(new XElement(saveObject as XElement));
            else if (saveType == typeof(string))
            {
                string str = (string) saveObject;
                if (str.Any(ch => ch < ' '))
                {
                    elem.Add(new XAttribute("encoding", "base64"));
                    elem.Add(str.ToUtf8().Base64UrlEncode());
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
            else if (RConvert.IsSupportedType(saveType))
            {
                string result;
                RConvert.Exact(saveObject, out result);
                elem.Add(result);
            }
            else
            {
                Type keyType = null, valueType = null;
                Type[] typeParameters;

                if (saveType.IsArray)
                    valueType = saveType.GetElementType();
                else if (saveType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                {
                    keyType = typeParameters[0];
                    valueType = typeParameters[1];
                }
                else if (saveType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                    valueType = typeParameters[0];

                if (valueType != null)
                {
                    if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                        throw new Exception("The field {0} is of a dictionary type, but its key type is {1}. Only string, integer types and enums are supported.".Fmt(tagName, keyType.FullName));

                    var enumerator = saveType.GetMethod("GetEnumerator", new Type[] { }).Invoke(saveObject, new object[] { }) as IEnumerator;
                    Type kvpType = keyType == null ? null : typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                    while (enumerator.MoveNext())
                    {
                        object key = null;
                        var value = keyType == null ? enumerator.Current : kvpType.GetProperty("Value").GetValue(enumerator.Current, null);
                        var tag = objectToXElement(value, valueType, baseDir, "item");
                        if (keyType != null)
                        {
                            key = kvpType.GetProperty("Key").GetValue(enumerator.Current, null);
                            tag.Add(new XAttribute("key", key.ToString()));
                        }
                        elem.Add(tag);
                    }
                }
                else
                {
                    bool ignoreIfDefault = saveType.IsDefined<XmlIgnoreIfDefaultAttribute>(true);
                    bool ignoreIfEmpty = saveType.IsDefined<XmlIgnoreIfEmptyAttribute>(true);

                    foreach (var field in saveType.GetAllFields())
                    {
                        string rFieldName = field.Name.TrimStart('_');
                        MemberInfo getAttrsFrom = field;

                        // Special case: compiler-generated fields for auto-implemented properties have a name that can't be used as a tag name. Use the property name instead, which is probably what the user expects anyway
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

                            if ((ignoreIfDefault || getAttrsFrom.IsDefined<XmlIgnoreIfDefaultAttribute>(true)) && (saveValue == null || (saveValue.GetType().IsValueType && saveValue.Equals(Activator.CreateInstance(saveValue.GetType())))))
                                continue;

                            var def = getAttrsFrom.GetCustomAttributes<XmlIgnoreIfAttribute>(true);
                            if (def.Any() && saveValue.Equals(def.First().Value))
                                continue;

                            // [XmlFollowId]
                            if (getAttrsFrom.IsDefined<XmlFollowIdAttribute>())
                            {
                                if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                                    throw new Exception("A field that uses the [XmlFollowId] attribute must have the type XmlDeferredObject<T> for some T.");

                                Type innerType = field.FieldType.GetGenericArguments()[0];
                                string id = (string) field.FieldType.GetProperty("Id").GetValue(saveValue, null);
                                elem.Add(new XElement(rFieldName, new XAttribute("id", id)));

                                if ((bool) field.FieldType.GetProperty("Evaluated").GetValue(saveValue, null))
                                {
                                    var prop = field.FieldType.GetProperty("Value");
                                    saveObjectToXmlFile(prop.GetValue(saveValue, null), prop.PropertyType, Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + id + ".xml"), baseDir);
                                }
                            }
                            else
                            {
                                var subElem = objectToXElement(saveValue, field.FieldType, baseDir, rFieldName);
                                if (!subElem.IsEmpty || subElem.HasAttributes || !ignoreIfEmpty)
                                    elem.Add(subElem);
                            }
                        }
                    }

                    if (!saveType.Equals(declaredType))
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

            return elem;
        }

        private static bool isIntegerType(Type t)
        {
            return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);
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
    public class XmlFollowIdAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, it is ignored by <see cref="XmlClassify"/>.
    /// Data stored in this field or automatically-implemented property is not persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XmlIgnoreAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> does not generate a tag if the value is null, 0, or false.
    /// If it is used on a class or struct, it applies to all fields and automatically-implemented properties in the class or struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class XmlIgnoreIfDefaultAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property of a collection or dictionary type, <see cref="XmlClassify"/> does not generate a tag if the collection 
    /// or dictionary is empty. Notice that using this together with <see cref="XmlIgnoreIfDefaultAttribute"/> will cause the distinction between null and an empty collection or 
    /// dictionary to be lost. However, a collection or dictionary containing only null elements is persisted correctly.
    /// If it is used on a class or struct, it applies to all collection-type fields in the class or struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class XmlIgnoreIfEmptyAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field or automatically-implemented property, <see cref="XmlClassify"/> does not generate a tag if the field's or property's value is equal to the specified value.
    /// Notice that using this together with <see cref="XmlIgnoreIfDefaultAttribute"/> will cause the distinction between the type's default value and the specified value to be lost.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XmlIgnoreIfAttribute : Attribute
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
    public class XmlParentAttribute : Attribute { }

    /// <summary>
    /// When reconstructing persisted objects using <see cref="XmlClassify"/>, a field or automatically-implemented property with this attribute receives the ID that was used to refer to the
    /// XML file that stores this object.  See <see cref="XmlFollowIdAttribute"/> for more information. The field or automatically-implemented property must be of type string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XmlIdAttribute : Attribute { }

    /// <summary>
    /// Provides mechanisms to hold an object that has an ID and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public class XmlDeferredObject<T>
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
