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
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string Filename) where T : new()
        {
            return LoadObjectFromXmlFile<T>(Filename, null);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string Filename, object ParentNode) where T : new()
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            return LoadObjectFromXmlFile<T>(Filename, BaseDir, ParentNode);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <param name="BaseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T LoadObjectFromXmlFile<T>(string Filename, string BaseDir, object ParentNode) where T : new()
        {
            var StrRead = new StreamReader(Filename, Encoding.UTF8);
            XElement XElem = XElement.Load(StrRead);
            StrRead.Close();
            return ObjectFromXElement<T>(XElem, BaseDir, ParentNode);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="XElem">XML tree to reconstruct object from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement XElem) where T: new()
        {
            return ObjectFromXElement<T>(XElem, null, null);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="XElem">XML tree to reconstruct object from.</param>
        /// <param name="BaseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XmlParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ObjectFromXElement<T>(XElement XElem, string BaseDir, object ParentNode) where T : new()
        {
            T ReturnObject = new T();

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string RFieldName = Field.Name.TrimStart('_');
                var Attribs = Field.GetCustomAttributes(false);

                // [XmlIgnore]
                if (Attribs.Any(x => x is XmlIgnoreAttribute))
                    continue;

                // [XmlParent]
                else if (Attribs.Any(x => x is XmlParentAttribute))
                    Field.SetValue(ReturnObject, ParentNode);

                // [XmlFollowId]
                else if (Attribs.Any(x => x is XmlFollowIdAttribute))
                {
                    if (Field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                        throw new Exception("The field {0}.{1} uses the [XmlFollowId] attribute, but does not have the type XmlDeferredObject<T> for some T.".Fmt(typeof(T).FullName, Field.Name));

                    Type InnerType = Field.FieldType.GetGenericArguments()[0];
                    var Attr = XElem.Attribute(RFieldName);
                    if (Attr != null)
                    {
                        string NewFile = Path.Combine(BaseDir, InnerType.Name + Path.DirectorySeparatorChar + Attr.Value + ".xml");
                        Field.SetValue(ReturnObject,
                            // new XmlDeferredObject<InnerType>(Attr.Value, XmlClassify.LoadObjectFromXmlFile<InnerType>(NewFile, BaseDir, t))
                            typeof(XmlDeferredObject<>).MakeGenericType(InnerType)
                                .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                .Invoke(new object[] {
                                    Attr.Value,
                                    // XmlClassify.LoadObjectFromXmlFile<InnerType>(NewFile, BaseDir, t)
                                    typeof(XmlClassify).GetMethod("LoadObjectFromXmlFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                        .MakeGenericMethod(InnerType),
                                    null,
                                    new object[] { NewFile, BaseDir, ReturnObject }
                                })
                        );
                    }
                }

                // Primitive types
                else if (Field.FieldType == typeof(string) || Field.FieldType == typeof(bool) || IsIntegerType(Field.FieldType) || IsDecimalType(Field.FieldType) ||
                         Field.FieldType == typeof(DateTime) || Field.FieldType.IsEnum)
                {
                    var Attr = XElem.Attribute(RFieldName);
                    try
                    {
                        Type t = Field.FieldType;
                        if (t == typeof(string))
                            Field.SetValue(ReturnObject, Attr.Value);
                        else if (t.IsEnum)
                            Field.SetValue(ReturnObject, Attr.Value.ToStaticValue(t));
                        else    // bool, DateTime, integer types, decimal types
                            try { Field.SetValue(ReturnObject, ParseValue(t, Attr.Value)); }
                            catch { }
                    }
                    catch { }
                }

                else
                {
                    var Subtags = XElem.Elements().Where(xn => xn.Name == RFieldName);
                    if (!Subtags.Any()) continue;
                    var Subtag = Subtags.First();

                    if (Field.FieldType == typeof(XElement))
                        Field.SetValue(ReturnObject, Subtag.Elements().FirstOrDefault());
                    else
                    {
                        var xmlMethod = typeof(XmlClassify).GetMethod("ObjectFromXElement",
                            new Type[] { typeof(XElement), typeof(string), typeof(object) });

                        // Check if it's an array, collection or dictionary
                        Type KeyType = null, ValueType = null;
                        Type[] TypeParameters = null;

                        if (Field.FieldType.IsArray)
                            ValueType = Field.FieldType.GetElementType();
                        else if (Field.FieldType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out TypeParameters))
                        {
                            KeyType = TypeParameters[0];
                            ValueType = TypeParameters[1];
                        }
                        else if (Field.FieldType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out TypeParameters))
                            ValueType = TypeParameters[0];

                        if (ValueType == null)
                        {
                            Field.SetValue(ReturnObject, xmlMethod.MakeGenericMethod(Field.FieldType)
                                .Invoke(null, new object[] { Subtag, BaseDir, ReturnObject }));
                        }
                        else
                        {
                            if (KeyType != null && KeyType != typeof(string) && !IsIntegerType(KeyType) && !KeyType.IsEnum)
                                throw new Exception("The field {0}.{1} is of a dictionary type, but its key type is {2}. Only string and integer types are supported.".Fmt(typeof(T).FullName, Field.Name, KeyType));

                            if (!ValueType.IsEnum && ValueType != typeof(string) && !IsIntegerType(ValueType) && !IsDecimalType(ValueType) &&
                                ValueType != typeof(bool) && ValueType != typeof(DateTime) && ValueType != typeof(XElement))
                                xmlMethod = xmlMethod.MakeGenericMethod(ValueType);

                            object MyList;
                            if (Field.FieldType.IsArray)
                                MyList = Field.FieldType.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { Subtags.Count() });
                            else
                            {
                                MyList = Field.GetValue(ReturnObject);
                                if (MyList == null)
                                {
                                    MyList = Field.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                                    Field.SetValue(ReturnObject, MyList);
                                }
                                else
                                    Field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(MyList, new object[] { });
                            }

                            var AddMethod = Field.FieldType.IsArray
                                ? Field.FieldType.GetMethod("Set", new Type[] { typeof(int), ValueType })
                                : KeyType == null
                                    ? Field.FieldType.GetMethod("Add", new Type[] { ValueType })
                                    : Field.FieldType.GetMethod("Add", new Type[] { KeyType, ValueType });

                            int i = 0;
                            foreach (var ItemTag in Subtag.Elements().Where(xn => xn.Name == "item"))
                            {
                                object Key = null, Value = null;
                                if (KeyType != null)
                                {
                                    var KeyAttr = ItemTag.Attribute("key");
                                    try { Key = IsIntegerType(KeyType) ? (object) ParseValue(KeyType, KeyAttr.Value) : KeyAttr.Value; }
                                    catch { continue; }
                                }
                                var NullAttr = ItemTag.Attribute("null");
                                if (NullAttr == null)
                                {
                                    if (ValueType.IsEnum || ValueType == typeof(string) || IsIntegerType(ValueType) || IsDecimalType(ValueType) ||
                                        ValueType == typeof(bool) || ValueType == typeof(DateTime))
                                    {
                                        var ValueAttr = ItemTag.Attribute("value");
                                        try
                                        {
                                            Value = Field.FieldType == typeof(bool) || Field.FieldType == typeof(DateTime) || IsIntegerType(Field.FieldType) || IsDecimalType(Field.FieldType)
                                                ? (object) ParseValue(Field.FieldType, ValueAttr.Value)
                                                : Field.FieldType.IsEnum ? (object) ValueAttr.Value.ToStaticValue(Field.FieldType) : ValueAttr.Value;
                                        }
                                        catch { Value = ValueType.IsValueType ? ValueType.GetConstructor(new Type[] { }).Invoke(new object[] { }) : null; }
                                    }
                                    else if (ValueType == typeof(XElement))
                                        Value = ItemTag.Elements().FirstOrDefault();
                                    else
                                        Value = xmlMethod.Invoke(null, new object[] { ItemTag, BaseDir, ReturnObject });
                                }
                                if (Field.FieldType.IsArray)
                                    AddMethod.Invoke(MyList, new object[] { i++, Value });
                                else if (KeyType == null)
                                    AddMethod.Invoke(MyList, new object[] { Value });
                                else
                                    AddMethod.Invoke(MyList, new object[] { Key, Value });
                            }
                        }
                    }
                }
            }
            return ReturnObject;
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="SaveObject">Object to store in an XML file.</param>
        /// <param name="Filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        public static void SaveObjectToXmlFile<T>(T SaveObject, string Filename)
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            SaveObjectToXmlFile(SaveObject, Filename, BaseDir);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="SaveObject">Object to store in an XML file.</param>
        /// <param name="Filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        public static void SaveObjectToXmlFile<T>(T SaveObject, string Filename, string BaseDir)
        {
            var x = ObjectToXElement(SaveObject, BaseDir, "item");
            PathUtil.CreatePathToFile(Filename);
            x.Save(Filename);
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="SaveObject">Object to convert to an XML tree.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T SaveObject)
        {
            return ObjectToXElement(SaveObject, null, "item");
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="SaveObject">Object to convert to an XML tree.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T SaveObject, string BaseDir)
        {
            return ObjectToXElement(SaveObject, BaseDir, "item");
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="SaveObject">Object to convert to an XML tree.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XmlFollowIdAttribute"/> attribute.</param>
        /// <param name="TagName">Name of the top-level XML tag to use for this object.
        /// Default is "item".</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectToXElement<T>(T SaveObject, string BaseDir, string TagName)
        {
            XElement XElem = new XElement(TagName);

            if (typeof(T) == typeof(XElement))
            {
                XElem.Add(new XElement(SaveObject as XElement));
                return XElem;
            }

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string RFieldName = Field.Name.TrimStart('_');
                var Attribs = Field.GetCustomAttributes(false);

                // [XmlIgnore]
                if (Attribs.Any(x => x is XmlIgnoreAttribute))
                    continue;

                // [XmlParent]
                else if (Attribs.Any(x => x is XmlParentAttribute))
                    continue;

                // [XmlFollowId]
                else if (Attribs.Any(x => x is XmlFollowIdAttribute))
                {
                    if (Field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                        throw new Exception("A field that uses the [XmlFollowId] attribute must have the type XmlDeferredObject<T> for some T.");

                    Type innerType = Field.FieldType.GetGenericArguments()[0];
                    Type xmlType = typeof(XmlDeferredObject<>).MakeGenericType(innerType);
                    string id = (string) xmlType.GetProperty("Id").GetValue(Field.GetValue(SaveObject), null);
                    XElem.SetAttributeValue(RFieldName, id);

                    if ((bool) xmlType.GetProperty("Evaluated").GetValue(Field.GetValue(SaveObject), null))
                        typeof(XmlClassify).GetMethods()
                            .Where(mi => mi.Name == "SaveObjectToXmlFile" && mi.GetParameters().Count() == 3)
                            .First().MakeGenericMethod(innerType).Invoke(null, new object[] {
                                xmlType.GetProperty("Value").GetValue(Field.GetValue(SaveObject), null),
                                Path.Combine(BaseDir, innerType.Name + Path.DirectorySeparatorChar + id + ".xml"),
                                BaseDir
                            });
                }

                else if (Field.GetValue(SaveObject) != null)
                {
                    // Primitive types
                    if (Field.FieldType.IsEnum || Field.FieldType == typeof(string) || Field.FieldType == typeof(bool) || Field.FieldType == typeof(DateTime) || IsIntegerType(Field.FieldType) || IsDecimalType(Field.FieldType))
                        XElem.SetAttributeValue(RFieldName, SafeToString(Field.FieldType, Field.GetValue(SaveObject)));
                    else
                    {
                        var xmlMethod = typeof(XmlClassify).GetMethods().Where(mi => mi.Name == "ObjectToXElement" && mi.GetParameters().Count() == 3).First();

                        Type KeyType = null, ValueType = null;
                        Type[] TypeParameters = null;

                        if (Field.FieldType.IsArray)
                            ValueType = Field.FieldType.GetElementType();
                        else if (Field.FieldType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out TypeParameters))
                        {
                            KeyType = TypeParameters[0];
                            ValueType = TypeParameters[1];
                        }
                        else if (Field.FieldType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out TypeParameters))
                            ValueType = TypeParameters[0];

                        if (ValueType == null)
                        {
                            // Field.FieldType is not an array or collection or dictionary; use recursion to store the object
                            XElem.Add(xmlMethod.MakeGenericMethod(Field.FieldType)
                                .Invoke(null, new object[] { Field.GetValue(SaveObject), BaseDir, RFieldName }));
                        }
                        else
                        {
                            xmlMethod = xmlMethod.MakeGenericMethod(ValueType);
                            if (KeyType != null && KeyType != typeof(string) && !IsIntegerType(KeyType))
                                throw new Exception("The field {0}.{1} is a dictionary whose key type is {2}, but only string and integer types are supported."
                                    .Fmt(typeof(T).FullName, Field.Name, KeyType.FullName));
                            var Enumerator = Field.FieldType.GetMethod("GetEnumerator", new Type[] { }).Invoke(Field.GetValue(SaveObject), new object[] { }) as IEnumerator;
                            Type KvpType = KeyType == null ? null : typeof(KeyValuePair<,>).MakeGenericType(KeyType, ValueType);
                            var CollectionTag = new XElement(RFieldName);
                            while (Enumerator.MoveNext())
                            {
                                object Key = null;
                                if (KeyType != null)
                                    Key = KvpType.GetProperty("Key").GetValue(Enumerator.Current, null);
                                var Value = KeyType == null ? Enumerator.Current : KvpType.GetProperty("Value").GetValue(Enumerator.Current, null);
                                XElement Subtag;
                                if (Value == null)
                                {
                                    Subtag = new XElement("item");
                                    if (Key != null) Subtag.SetAttributeValue("key", Key);
                                    Subtag.SetAttributeValue("null", 1);
                                }
                                else if (ValueType.IsEnum || ValueType == typeof(bool) || IsIntegerType(ValueType) || IsDecimalType(ValueType) || ValueType == typeof(string) || ValueType == typeof(DateTime))
                                {
                                    Subtag = new XElement("item");
                                    if (Key != null) Subtag.SetAttributeValue("key", Key);
                                    Subtag.SetAttributeValue("value", SafeToString(ValueType, Value));
                                }
                                else
                                {
                                    Subtag = (XElement) xmlMethod.Invoke(null, new object[] { Value, BaseDir, "item" });
                                    if (Key != null) Subtag.SetAttributeValue("key", Key);
                                }
                                CollectionTag.Add(Subtag);
                            }
                            XElem.Add(CollectionTag);
                        }
                    }
                }
            }

            return XElem;
        }

        private static string SafeToString(Type type, object obj)
        {
            if (type == typeof(DateTime))
            {
                string result;
                RConvert.Exact((DateTime) obj, out result);
                return result;
            }

            if (IsDecimalType(type))
            {
                var met = type.GetMethods().Where(m => m.Name == "ToString" && !m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).First();
                return (string) met.Invoke(obj, new object[] { "R" });
            }

            return obj.ToString();
        }

        private static object ParseValue(Type type, string StringToParse)
        {
            var met = type.GetMethods().Where(m => m.Name == "Parse" && m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)).First();
            return met.Invoke(null, new object[] { StringToParse });
        }

        private static bool IsIntegerType(Type t)
        {
            return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);
        }

        private static bool IsDecimalType(Type t)
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
                    foreach (var Field in _cached.GetType().GetFields()
                        .Where(fld => fld.FieldType == typeof(string) &&
                                      fld.GetCustomAttributes(false).Any(attr => attr is XmlIdAttribute)))
                        Field.SetValue(_cached, _id);
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

        /// <summary>Returns the Id used to refer to the object.</summary>
        public string Id { get { return _id; } }
    }
}
