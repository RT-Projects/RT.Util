using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.XmlClassify
{
    /// <summary>
    /// Provides static methods to save objects of (almost) arbitrary classes into XML files and load them again.
    /// The functionality is similar to XmlSerializer, but uses the newer C# XML API and is also more full-featured.
    /// </summary>
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
        /// it will receive the object passed in here as its value. Default is null.</param>
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
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string filename, string baseDir, object parentNode) where T : new()
        {
            var strRead = new StreamReader(filename, Encoding.UTF8);
            XElement elem = XElement.Load(strRead);
            strRead.Close();
            return ObjectFromXElement<T>(elem, baseDir, parentNode);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem) where T : new()
        {
            return ObjectFromXElement<T>(elem, null, null);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="elem">XML tree to reconstruct object from.</param>
        /// <param name="baseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="parentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement elem, string baseDir, object parentNode) where T : new()
        {
            T ret = new T();

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string rFieldName = field.Name.TrimStart('_');
                var attribs = field.GetCustomAttributes(false);

                // [XmlIgnore]
                if (attribs.Any(x => x is XmlIgnoreAttribute))
                    continue;

                // [XmlParent]
                else if (attribs.Any(x => x is XmlParentAttribute))
                    field.SetValue(ret, parentNode);

                // [XmlFollowId]
                else if (attribs.Any(x => x is XmlFollowIdAttribute))
                {
                    if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                        throw new Exception("The field {0}.{1} uses the [XmlFollowId] attribute, but does not have the type XmlDeferredObject<T> for some T.".Fmt(typeof(T).FullName, field.Name));

                    Type innerType = field.FieldType.GetGenericArguments()[0];
                    var attr = elem.Attribute(rFieldName);
                    if (attr != null)
                    {
                        string newFile = Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + attr.Value + ".xml");
                        field.SetValue(ret,
                            // new XmlDeferredObject<InnerType>(Attr.Value, XmlClassify.LoadObjectFromXmlFile<InnerType>(NewFile, BaseDir, t))
                            typeof(XmlDeferredObject<>).MakeGenericType(innerType)
                                .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                .Invoke(new object[] {
                                    attr.Value,
                                    // XmlClassify.LoadObjectFromXmlFile<InnerType>(NewFile, BaseDir, t)
                                    typeof(XmlClassify).GetMethod("LoadObjectFromXmlFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                        .MakeGenericMethod(innerType),
                                    null,
                                    new object[] { newFile, baseDir, ret }
                                })
                        );
                    }
                }

                // Primitive types
                else if (field.FieldType == typeof(string) || field.FieldType == typeof(bool) || isIntegerType(field.FieldType) || isDecimalType(field.FieldType) ||
                         field.FieldType == typeof(DateTime) || field.FieldType.IsEnum)
                {
                    var attr = elem.Attribute(rFieldName);
                    try
                    {
                        Type t = field.FieldType;
                        if (t == typeof(string))
                            field.SetValue(ret, attr.Value);
                        else if (t.IsEnum)
                            field.SetValue(ret, Enum.Parse(t, attr.Value));
                        else    // bool, DateTime, integer types, decimal types
                            try { field.SetValue(ret, parseValue(t, attr.Value)); }
                            catch { }
                    }
                    catch { }
                }

                else
                {
                    var subtags = elem.Elements().Where(xn => xn.Name == rFieldName);
                    if (!subtags.Any()) continue;
                    var subtag = subtags.First();

                    if (field.FieldType == typeof(XElement))
                        field.SetValue(ret, subtag.Elements().FirstOrDefault());
                    else
                    {
                        var xmlMethod = typeof(XmlClassify).GetMethod("ObjectFromXElement",
                            new Type[] { typeof(XElement), typeof(string), typeof(object) });

                        // Check if it's an array, collection or dictionary
                        Type keyType = null, valueType = null;
                        Type[] typeParameters = null;

                        if (field.FieldType.IsArray)
                            valueType = field.FieldType.GetElementType();
                        else if (field.FieldType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                        {
                            keyType = typeParameters[0];
                            valueType = typeParameters[1];
                        }
                        else if (field.FieldType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                            valueType = typeParameters[0];

                        if (valueType == null)
                        {
                            field.SetValue(ret, xmlMethod.MakeGenericMethod(field.FieldType)
                                .Invoke(null, new object[] { subtag, baseDir, ret }));
                        }
                        else
                        {
                            if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                                throw new Exception("The field {0}.{1} is of a dictionary type, but its key type is {2}. Only string and integer types are supported.".Fmt(typeof(T).FullName, field.Name, keyType));

                            if (!valueType.IsEnum && valueType != typeof(string) && !isIntegerType(valueType) && !isDecimalType(valueType) &&
                                valueType != typeof(bool) && valueType != typeof(DateTime) && valueType != typeof(XElement))
                                xmlMethod = xmlMethod.MakeGenericMethod(valueType);

                            object outputList;
                            if (field.FieldType.IsArray)
                                outputList = field.FieldType.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { subtags.Count() });
                            else
                            {
                                outputList = field.GetValue(ret);
                                if (outputList == null)
                                {
                                    outputList = field.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                                    field.SetValue(ret, outputList);
                                }
                                else
                                    field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(outputList, new object[] { });
                            }

                            var addMethod = field.FieldType.IsArray
                                ? field.FieldType.GetMethod("Set", new Type[] { typeof(int), valueType })
                                : keyType == null
                                    ? field.FieldType.GetMethod("Add", new Type[] { valueType })
                                    : field.FieldType.GetMethod("Add", new Type[] { keyType, valueType });

                            int i = 0;
                            foreach (var itemTag in subtag.Elements().Where(xn => xn.Name == "item"))
                            {
                                object key = null, value = null;
                                if (keyType != null)
                                {
                                    var keyAttr = itemTag.Attribute("key");
                                    try { key = isIntegerType(keyType) ? (object) parseValue(keyType, keyAttr.Value) : keyAttr.Value; }
                                    catch { continue; }
                                }
                                var nullAttr = itemTag.Attribute("null");
                                if (nullAttr == null)
                                {
                                    if (valueType.IsEnum || valueType == typeof(string) || isIntegerType(valueType) || isDecimalType(valueType) ||
                                        valueType == typeof(bool) || valueType == typeof(DateTime))
                                    {
                                        var valueAttr = itemTag.Attribute("value");
                                        try
                                        {
                                            value = field.FieldType == typeof(bool) || field.FieldType == typeof(DateTime) || isIntegerType(field.FieldType) || isDecimalType(field.FieldType)
                                                ? (object) parseValue(field.FieldType, valueAttr.Value)
                                                : field.FieldType.IsEnum ? Enum.Parse(field.FieldType, valueAttr.Value) : valueAttr.Value;
                                        }
                                        catch { value = valueType.IsValueType ? valueType.GetConstructor(new Type[] { }).Invoke(new object[] { }) : null; }
                                    }
                                    else if (valueType == typeof(XElement))
                                        value = itemTag.Elements().FirstOrDefault();
                                    else
                                        value = xmlMethod.Invoke(null, new object[] { itemTag, baseDir, ret });
                                }
                                if (field.FieldType.IsArray)
                                    addMethod.Invoke(outputList, new object[] { i++, value });
                                else if (keyType == null)
                                    addMethod.Invoke(outputList, new object[] { value });
                                else
                                    addMethod.Invoke(outputList, new object[] { key, value });
                            }
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename)
        {
            string baseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            SaveObjectToXmlFile(saveObject, filename, baseDir);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="saveObject">Object to store in an XML file.</param>
        /// <param name="filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        /// <param name="baseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename, string baseDir)
        {
            var x = ObjectToXElement(saveObject, baseDir, "item");
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
            return ObjectToXElement(saveObject, null, "item");
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
            return ObjectToXElement(saveObject, baseDir, "item");
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
            XElement elem = new XElement(tagName);

            if (typeof(T) == typeof(XElement))
            {
                elem.Add(new XElement(saveObject as XElement));
                return elem;
            }

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string rFieldName = field.Name.TrimStart('_');
                var attribs = field.GetCustomAttributes(false);

                // [XmlIgnore]
                if (attribs.Any(x => x is XmlIgnoreAttribute))
                    continue;

                // [XmlParent]
                else if (attribs.Any(x => x is XmlParentAttribute))
                    continue;

                // [XmlFollowId]
                else if (attribs.Any(x => x is XmlFollowIdAttribute))
                {
                    if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                        throw new Exception("A field that uses the [XmlFollowId] attribute must have the type XmlDeferredObject<T> for some T.");

                    Type innerType = field.FieldType.GetGenericArguments()[0];
                    Type xmlType = typeof(XmlDeferredObject<>).MakeGenericType(innerType);
                    string id = (string) xmlType.GetProperty("Id").GetValue(field.GetValue(saveObject), null);
                    elem.SetAttributeValue(rFieldName, id);

                    if ((bool) xmlType.GetProperty("Evaluated").GetValue(field.GetValue(saveObject), null))
                        typeof(XmlClassify).GetMethods()
                            .Where(mi => mi.Name == "SaveObjectToXmlFile" && mi.GetParameters().Count() == 3)
                            .First().MakeGenericMethod(innerType).Invoke(null, new object[] {
                                xmlType.GetProperty("Value").GetValue(field.GetValue(saveObject), null),
                                Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + id + ".xml"),
                                baseDir
                            });
                }

                else if (field.GetValue(saveObject) != null)
                {
                    // Primitive types
                    if (field.FieldType.IsEnum || field.FieldType == typeof(string) || field.FieldType == typeof(bool) || field.FieldType == typeof(DateTime) || isIntegerType(field.FieldType) || isDecimalType(field.FieldType))
                        elem.SetAttributeValue(rFieldName, safeToString(field.FieldType, field.GetValue(saveObject)));
                    else
                    {
                        var xmlMethod = typeof(XmlClassify).GetMethods().Where(mi => mi.Name == "ObjectToXElement" && mi.GetParameters().Count() == 3).First();

                        Type keyType = null, valueType = null;
                        Type[] typeParameters = null;

                        if (field.FieldType.IsArray)
                            valueType = field.FieldType.GetElementType();
                        else if (field.FieldType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                        {
                            keyType = typeParameters[0];
                            valueType = typeParameters[1];
                        }
                        else if (field.FieldType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                            valueType = typeParameters[0];

                        if (valueType == null)
                        {
                            // Field.FieldType is not an array or collection or dictionary; use recursion to store the object
                            elem.Add(xmlMethod.MakeGenericMethod(field.FieldType)
                                .Invoke(null, new object[] { field.GetValue(saveObject), baseDir, rFieldName }));
                        }
                        else
                        {
                            xmlMethod = xmlMethod.MakeGenericMethod(valueType);
                            if (keyType != null && keyType != typeof(string) && !isIntegerType(keyType))
                                throw new Exception("The field {0}.{1} is a dictionary whose key type is {2}, but only string and integer types are supported."
                                    .Fmt(typeof(T).FullName, field.Name, keyType.FullName));
                            var enumerator = field.FieldType.GetMethod("GetEnumerator", new Type[] { }).Invoke(field.GetValue(saveObject), new object[] { }) as IEnumerator;
                            Type kvpType = keyType == null ? null : typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                            var collectionTag = new XElement(rFieldName);
                            while (enumerator.MoveNext())
                            {
                                object key = null;
                                if (keyType != null)
                                    key = kvpType.GetProperty("Key").GetValue(enumerator.Current, null);
                                var value = keyType == null ? enumerator.Current : kvpType.GetProperty("Value").GetValue(enumerator.Current, null);
                                XElement subtag;
                                if (value == null)
                                {
                                    subtag = new XElement("item");
                                    if (key != null) subtag.SetAttributeValue("key", key);
                                    subtag.SetAttributeValue("null", 1);
                                }
                                else if (valueType.IsEnum || valueType == typeof(bool) || isIntegerType(valueType) || isDecimalType(valueType) || valueType == typeof(string) || valueType == typeof(DateTime))
                                {
                                    subtag = new XElement("item");
                                    if (key != null) subtag.SetAttributeValue("key", key);
                                    subtag.SetAttributeValue("value", safeToString(valueType, value));
                                }
                                else
                                {
                                    subtag = (XElement) xmlMethod.Invoke(null, new object[] { value, baseDir, "item" });
                                    if (key != null) subtag.SetAttributeValue("key", key);
                                }
                                collectionTag.Add(subtag);
                            }
                            elem.Add(collectionTag);
                        }
                    }
                }
            }

            return elem;
        }

        private static string safeToString(Type type, object obj)
        {
            if (type == typeof(DateTime))
            {
                string result;
                RConvert.Exact((DateTime) obj, out result);
                return result;
            }

            if (isDecimalType(type))
            {
                var met = type.GetMethods().Where(m => m.Name == "ToString" && !m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).First();
                return (string) met.Invoke(obj, new object[] { "R" });
            }

            return obj.ToString();
        }

        private static object parseValue(Type type, string strToParse)
        {
            var met = type.GetMethods().Where(m => m.Name == "Parse" && m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).First();
            return met.Invoke(null, new object[] { strToParse });
        }

        private static bool isIntegerType(Type t)
        {
            return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);
        }

        private static bool isDecimalType(Type t)
        {
            return t == typeof(float) || t == typeof(double) || t == typeof(decimal);
        }
    }

    /// <summary>
    /// If this attribute is used on a field, the XML tag attribute will contain an ID that points to another, separate
    /// XML file which in turn contains the actual object for this field. This is only allowed on fields of type
    /// <see cref="XmlDeferredObject&lt;T&gt;"/> for some class type T. Use <see cref="XmlDeferredObject&lt;T&gt;.Value"/>
    /// to retrieve the object. This retrieval is deferred until first use. Use <see cref="XmlDeferredObject&lt;T&gt;.Id"/>
    /// to retrieve the Id used to reference the object. You can also capture the ID into the class T by using the
    /// <see cref="XmlIdAttribute"/> attribute within that class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlFollowIdAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field, it is ignored by XmlClassify. Data stored in this field is not persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlIgnoreAttribute : Attribute { }

    /// <summary>
    /// A field with this attribute set will receive a reference to the object which was its parent node
    /// in the XML tree. If the field is of the wrong type, a runtime exception will occur. If there was
    /// no parent node, the field will be set to null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlParentAttribute : Attribute { }

    /// <summary>
    /// A field with this attribute set will receive the Id that was used to refer to the XML file
    /// that stores this object. See <see cref="XmlFollowIdAttribute"/>. The field must
    /// be of type <see langword="string"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlIdAttribute : Attribute { }

    /// <summary>
    /// Provides mechanisms to hold an object that has an Id and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public class XmlDeferredObject<T>
    {
        /// <summary>Initialises a deferred object using a delegate or lambda expression.</summary>
        /// <param name="id">Id that refers to the object to be generated.</param>
        /// <param name="generator">Function to generate the object.</param>
        public XmlDeferredObject(string id, Func<T> generator) { _id = id; this._generator = generator; }

        /// <summary>Initialises a deferred object using an actual object. Evaluation is not deferred.</summary>
        /// <param name="id">Id that refers to the object.</param>
        /// <param name="value">The object to store.</param>
        public XmlDeferredObject(string id, T value) { _id = id; _cached = value; _haveCache = true; }

        /// <summary>Initialises a deferred object using a method reference and an array of parameters.</summary>
        /// <param name="id">Id that refers to the object to be generated.</param>
        /// <param name="generatorMethod">Reference to the method that will return the computed object.</param>
        /// <param name="generatorObject">Object on which the method should be invoked. Use null for static methods.</param>
        /// <param name="generatorParams">Set of parameters for the method invocation.</param>
        public XmlDeferredObject(string id, MethodInfo generatorMethod, object generatorObject, object[] generatorParams)
        {
            _id = id;
            this._generator = () => (T) generatorMethod.Invoke(generatorObject, generatorParams);
        }

        private Func<T> _generator;
        private T _cached;
        private bool _haveCache = false;
        private string _id;

        /// <summary>
        /// Gets or sets the object stored in this <see cref="XmlDeferredObject&lt;T&gt;"/>. The property getter will
        /// cause the object to be evaluated when called. The setter will override the object with a pre-computed
        /// object whose evaluation is not deferred.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_haveCache)
                {
                    _cached = _generator();
                    // Update any field in the class that has an [XmlId] attribute and is of type string.
                    foreach (var field in _cached.GetType().GetFields()
                        .Where(fld => fld.FieldType == typeof(string) &&
                                      fld.GetCustomAttributes(false).Any(attr => attr is XmlIdAttribute)))
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
