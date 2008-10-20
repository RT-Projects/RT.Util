using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Collections;
using RT.Util.ExtensionMethods;
using System.Threading;
using System.Diagnostics;

namespace RT.Util.XMLClassify
{
    /// <summary>
    /// Provides static methods to save objects of (almost) arbitrary classes into XML files and load them again.
    /// The functionality is similar to XmlSerializer, but uses the newer C# XML API and is also more full-featured.
    /// </summary>
    public static class XMLClassify
    {
        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ReadObjectFromXMLFile<T>(string Filename) where T : new()
        {
            return ReadObjectFromXMLFile<T>(Filename, null);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XMLParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ReadObjectFromXMLFile<T>(string Filename, object ParentNode) where T : new()
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            return ReadObjectFromXMLFile<T>(Filename, BaseDir, ParentNode);
        }

        /// <summary>
        /// Reads an object of the specified type from the specified XML file.
        /// </summary>
        /// <typeparam name="T">Type of object to read.</typeparam>
        /// <param name="Filename">Path and filename of the XML file to read from.</param>
        /// <param name="BaseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XMLFollowIDAttribute"/> attribute.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XMLParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ReadObjectFromXMLFile<T>(string Filename, string BaseDir, object ParentNode) where T : new()
        {
            var StrRead = new StreamReader(Filename, Encoding.UTF8);
            XElement XElem = XElement.Load(StrRead);
            StrRead.Close();
            return ReadObjectFromXElement<T>(XElem, BaseDir, ParentNode);
        }

        /// <summary>
        /// Reconstructs an object of the specified type from the specified XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to reconstruct.</typeparam>
        /// <param name="XElem">XML tree to reconstruct object from.</param>
        /// <param name="BaseDir">The base directory from which to locate additional XML files
        /// whenever a field has an <see cref="XMLFollowIDAttribute"/> attribute.</param>
        /// <param name="ParentNode">If the type T contains a field with the <see cref="XMLParentAttribute"/> attribute,
        /// it will receive the object passed in here as its value. Default is null.</param>
        /// <returns>A new instance of the requested type.</returns>
        public static T ReadObjectFromXElement<T>(XElement XElem, string BaseDir, object ParentNode) where T : new()
        {
            T ReturnObject = new T();

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string RFieldName = Field.Name.TrimStart('_');
                var Attribs = Field.GetCustomAttributes(false);

                // [XMLIgnore]
                if (Attribs.Any(x => x is XMLIgnoreAttribute))
                    continue;

                // [XMLParent]
                else if (Attribs.Any(x => x is XMLParentAttribute))
                    Field.SetValue(ReturnObject, ParentNode);

                // [XMLFollowID]
                else if (Attribs.Any(x => x is XMLFollowIDAttribute))
                {
                    if (Field.FieldType.GetGenericTypeDefinition() != typeof(XMLDeferredObject<>))
                        throw new Exception("A field that uses the [XMLFollowID] attribute must have the type XMLDeferredObject<T> for some T.");

                    Type InnerType = Field.FieldType.GetGenericArguments()[0];
                    var Attr = XElem.Attribute(RFieldName);
                    if (Attr != null)
                    {
                        string NewFile = Path.Combine(BaseDir, InnerType.Name + Path.DirectorySeparatorChar + Attr.Value + ".xml");
                        Field.SetValue(ReturnObject,
                            // new XMLDeferredObject<InnerType>(ID, XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, t))
                            typeof(XMLDeferredObject<>).MakeGenericType(InnerType)
                                .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                .Invoke(new object[] {
                                    Attr.Value,
                                    // XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, t)
                                    typeof(XMLClassify).GetMethod("ReadObjectFromXMLFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                        .MakeGenericMethod(InnerType),
                                    null,
                                    new object[] { NewFile, BaseDir, ReturnObject }
                                })
                        );
                    }
                }

                // Primitive types
                else if (Field.FieldType == typeof(string) || Field.FieldType == typeof(bool) || Field.FieldType == typeof(int) ||
                         Field.FieldType == typeof(DateTime) || Field.FieldType.IsEnum)
                {
                    var Attr = XElem.Attribute(RFieldName);
                    try
                    {
                        if (Field.FieldType == typeof(string))
                            Field.SetValue(ReturnObject, Attr.Value);
                        else if (Field.FieldType == typeof(bool))
                            Field.SetValue(ReturnObject, bool.Parse(Attr.Value));
                        else if (Field.FieldType == typeof(int))
                            Field.SetValue(ReturnObject, int.Parse(Attr.Value));
                        else if (Field.FieldType == typeof(DateTime))
                            Field.SetValue(ReturnObject, DateTime.Parse(Attr.Value));
                        else // enums
                            Field.SetValue(ReturnObject, Attr.Value.ToStaticValue(Field.FieldType));
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
                        var XMLMethod = typeof(XMLClassify).GetMethod("ReadObjectFromXElement",
                            new Type[] { typeof(XElement), typeof(string), typeof(object) });

                        // Check if it's an array, collection or dictionary
                        Type KeyType = null, ValueType = null;

                        if (Field.FieldType.IsArray)
                            ValueType = Field.FieldType.GetElementType();
                        else if (!Field.FieldType.ImplementsInterface2(typeof(IDictionary<,>), out KeyType, out ValueType))
                            Field.FieldType.ImplementsInterface1(typeof(ICollection<>), out ValueType);

                        if (ValueType == null)
                        {
                            Field.SetValue(ReturnObject, XMLMethod.MakeGenericMethod(Field.FieldType)
                                .Invoke(null, new object[] { Subtag, BaseDir, ReturnObject }));
                        }
                        else
                        {
                            if (!ValueType.IsEnum && ValueType != typeof(string) && ValueType != typeof(int) &&
                                ValueType != typeof(bool) && ValueType != typeof(DateTime) && ValueType != typeof(XElement))
                                XMLMethod = XMLMethod.MakeGenericMethod(ValueType);

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
                                    try { Key = KeyType == typeof(int) ? (object) int.Parse(KeyAttr.Value) : KeyAttr.Value; }
                                    catch { continue; }
                                }
                                var NullAttr = ItemTag.Attribute("null");
                                if (NullAttr == null)
                                {
                                    if (ValueType.IsEnum || ValueType == typeof(string) || ValueType == typeof(int) ||
                                        ValueType == typeof(bool) || ValueType == typeof(DateTime))
                                    {
                                        var ValueAttr = ItemTag.Attribute("value");
                                        try
                                        {
                                            Value = Field.FieldType.IsEnum ? (object) ValueAttr.Value.ToStaticValue(Field.FieldType) :
                                                    Field.FieldType == typeof(bool) ? (object) bool.Parse(ValueAttr.Value) :
                                                    Field.FieldType == typeof(int) ? (object) int.Parse(ValueAttr.Value) :
                                                    Field.FieldType == typeof(DateTime) ? (object) DateTime.Parse(ValueAttr.Value) :
                                                    ValueAttr.Value;
                                        }
                                        catch { Value = ValueType.IsValueType ? ValueType.GetConstructor(new Type[] { }).Invoke(new object[] { }) : null; }
                                    }
                                    else if (ValueType == typeof(XElement))
                                        Value = ItemTag.Elements().FirstOrDefault();
                                    else
                                        Value = XMLMethod.Invoke(null, new object[] { ItemTag, BaseDir, ReturnObject });
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
        public static void SaveObjectAsXML<T>(T SaveObject, string Filename)
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            SaveObjectAsXML(SaveObject, Filename, BaseDir);
        }

        /// <summary>
        /// Stores the specified object in an XML file with the given path and filename.
        /// </summary>
        /// <typeparam name="T">Type of the object to store.</typeparam>
        /// <param name="SaveObject">Object to store in an XML file.</param>
        /// <param name="Filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XMLFollowIDAttribute"/> attribute.</param>
        public static void SaveObjectAsXML<T>(T SaveObject, string Filename, string BaseDir)
        {
            var x = ObjectAsXML(SaveObject, BaseDir, "item");
            if (!Directory.Exists(Path.GetDirectoryName(Filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(Filename));
            x.Save(Filename);
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="SaveObject">Object to convert to an XML tree.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XMLFollowIDAttribute"/> attribute.</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectAsXML<T>(T SaveObject, string BaseDir)
        {
            return ObjectAsXML(SaveObject, BaseDir, "item");
        }

        /// <summary>
        /// Converts the specified object into an XML tree.
        /// </summary>
        /// <typeparam name="T">Type of object to convert.</typeparam>
        /// <param name="SaveObject">Object to convert to an XML tree.</param>
        /// <param name="BaseDir">The base directory from which to construct the paths for
        /// additional XML files whenever a field has an <see cref="XMLFollowIDAttribute"/> attribute.</param>
        /// <param name="TagName">Name of the top-level XML tag to use for this object.
        /// Default is "item".</param>
        /// <returns>XML tree generated from the object.</returns>
        public static XElement ObjectAsXML<T>(T SaveObject, string BaseDir, string TagName)
        {
            XElement XElem = new XElement(TagName);

            if (typeof(T) == typeof(XElement))
            {
                XElem.Add(SaveObject);
                return XElem;
            }

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string RFieldName = Field.Name.TrimStart('_');
                var Attribs = Field.GetCustomAttributes(false);

                // [XMLIgnore]
                if (Attribs.Any(x => x is XMLIgnoreAttribute))
                    continue;

                // [XMLParent]
                else if (Attribs.Any(x => x is XMLParentAttribute))
                    continue;

                // [XMLFollowID]
                else if (Attribs.Any(x => x is XMLFollowIDAttribute))
                {
                    if (Field.FieldType.GetGenericTypeDefinition() != typeof(XMLDeferredObject<>))
                        throw new Exception("A field that uses the [XMLFollowID] attribute must have the type XMLDeferredObject<T> for some T.");

                    Type InnerType = Field.FieldType.GetGenericArguments()[0];
                    Type XMLType = typeof(XMLDeferredObject<>).MakeGenericType(InnerType);
                    string ID = (string) XMLType.GetProperty("ID").GetValue(Field.GetValue(SaveObject), null);
                    XElem.SetAttributeValue(RFieldName, ID);

                    if ((bool) XMLType.GetProperty("Evaluated").GetValue(Field.GetValue(SaveObject), null))
                        typeof(XMLClassify).GetMethods()
                            .Where(mi => mi.Name == "SaveObjectAsXML" && mi.GetParameters().Count() == 3)
                            .First().MakeGenericMethod(InnerType).Invoke(null, new object[] {
                                XMLType.GetProperty("Value").GetValue(Field.GetValue(SaveObject), null),
                                Path.Combine(BaseDir, InnerType.Name + Path.DirectorySeparatorChar + ID + ".xml"),
                                BaseDir
                            });
                }

                // Primitive types
                else if (Field.FieldType.IsEnum || Field.FieldType == typeof(string) || Field.FieldType == typeof(bool) || Field.FieldType == typeof(int) || Field.FieldType == typeof(DateTime))
                {
                    if (Field.GetValue(SaveObject) != null)
                        XElem.SetAttributeValue(RFieldName, Field.FieldType == typeof(DateTime)
                            ? typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Field.GetValue(SaveObject), new object[] { "u" }).ToString()
                            : Field.GetValue(SaveObject).ToString());
                }

                else if (Field.GetValue(SaveObject) != null)
                {
                    var XMLMethod = typeof(XMLClassify).GetMethods().Where(mi => mi.Name == "ObjectAsXML" && mi.GetParameters().Count() == 3).First();

                    Type KeyType = null, ValueType = null;

                    if (Field.FieldType.IsArray)
                        ValueType = Field.FieldType.GetElementType();
                    else if (!Field.FieldType.ImplementsInterface2(typeof(IDictionary<,>), out KeyType, out ValueType))
                        Field.FieldType.ImplementsInterface1(typeof(ICollection<>), out ValueType);

                    if (ValueType == null)
                    {
                        // Field.FieldType is not an array or collection or dictionary; use recursion to store the object
                        XElem.Add(XMLMethod.MakeGenericMethod(Field.FieldType)
                            .Invoke(null, new object[] { Field.GetValue(SaveObject), BaseDir, RFieldName }));
                    }
                    else
                    {
                        XMLMethod = XMLMethod.MakeGenericMethod(ValueType);
                        var DateTimeMethod = ValueType == typeof(DateTime) ? typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }) : null;
                        if (KeyType != null && KeyType != typeof(int) && KeyType != typeof(string))
                            throw new Exception(string.Format("The field {0}.{1} is a dictionary whose key type is {2}, but only int and string are supported.",
                                typeof(T).FullName, Field.Name, KeyType.FullName));
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
                                Subtag.SetAttributeValue("null", 1);
                            }
                            else if (ValueType.IsEnum || ValueType == typeof(bool) || ValueType == typeof(int) || ValueType == typeof(string) || ValueType == typeof(DateTime))
                            {
                                Subtag = new XElement("item");
                                Subtag.SetAttributeValue("value", ValueType == typeof(DateTime) ? DateTimeMethod.Invoke(Value, new object[] { "u" }).ToString() : Value);
                            }
                            else
                                Subtag = (XElement) XMLMethod.Invoke(null, new object[] { Value, BaseDir, "item" });
                            if (Key != null)
                                Subtag.SetAttributeValue("key", Key);
                            CollectionTag.Add(Subtag);
                        }
                        XElem.Add(CollectionTag);
                    }
                }
            }

            return XElem;
        }
    }

    /// <summary>
    /// If this attribute is used on a field, the XML tag attribute will contain an ID that points to another, separate
    /// XML file which in turn contains the actual object for this field. This is only allowed on fields of type
    /// <see cref="XMLDeferredObject&lt;T&gt;"/> for some class type T. Use <see cref="XMLDeferredObject&lt;T&gt;.Value"/>
    /// to retrieve the object. This retrieval is deferred until first use. Use <see cref="XMLDeferredObject&lt;T&gt;.ID"/>
    /// to retrieve the ID used to reference the object. You can also capture the ID into the class T by using the
    /// <see cref="XMLIDAttribute"/> attribute within that class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLFollowIDAttribute : Attribute { }

    /// <summary>
    /// If this attribute is used on a field, it is ignored by XMLClassify. Data stored in this field is not persisted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLIgnoreAttribute : Attribute { }

    /// <summary>
    /// A field with this attribute set will receive a reference to the object which was its parent node
    /// in the XML tree. If the field is of the wrong type, a runtime exception will occur. If there was
    /// no parent node, the field will be set to null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLParentAttribute : Attribute { }

    /// <summary>
    /// A field with this attribute set will receive the ID that was used to refer to the XML file
    /// that stores this object. See <see cref="XMLFollowIDAttribute"/>. The field must
    /// be of type <see langword="string"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLIDAttribute : Attribute { }

    /// <summary>
    /// Provides mechanisms to hold an object that has an ID and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public class XMLDeferredObject<T>
    {
        /// <summary>Initialises a deferred object using a delegate or lambda expression.</summary>
        /// <param name="ID">ID that refers to the object to be generated.</param>
        /// <param name="Generator">Function to generate the object.</param>
        public XMLDeferredObject(string ID, Func<T> Generator) { _ID = ID; this.Generator = Generator; }

        /// <summary>Initialises a deferred object using an actual object. Evaluation is not deferred.</summary>
        /// <param name="ID">ID that refers to the object.</param>
        /// <param name="Value">The object to store.</param>
        public XMLDeferredObject(string ID, T Value) { _ID = ID; Cached = Value; HaveCache = true; }

        /// <summary>Initialises a deferred object using a method reference and an array of parameters.</summary>
        /// <param name="ID">ID that refers to the object to be generated.</param>
        /// <param name="GeneratorMethod">Reference to the method that will return the computed object.</param>
        /// <param name="GeneratorObject">Object on which the method should be invoked. Use null for static methods.</param>
        /// <param name="GeneratorParams">Set of parameters for the method invocation.</param>
        public XMLDeferredObject(string ID, MethodInfo GeneratorMethod, object GeneratorObject, object[] GeneratorParams)
        {
            _ID = ID;
            this.Generator = () => (T) GeneratorMethod.Invoke(GeneratorObject, GeneratorParams);
        }

        private Func<T> Generator;
        private T Cached;
        private bool HaveCache = false;
        private string _ID;

        /// <summary>
        /// Gets or sets the object stored in this <see cref="XMLDeferredObject&lt;T&gt;"/>. The property getter will
        /// cause the object to be evaluated when called. The setter will override the object with a pre-computed
        /// object whose evaluation is not deferred.
        /// </summary>
        public T Value
        {
            get
            {
                if (!HaveCache)
                {
                    Cached = Generator();
                    // Update any field in the class that has an [XMLID] attribute and is of type string.
                    foreach (var Field in Cached.GetType().GetFields()
                        .Where(fld => fld.FieldType == typeof(string) &&
                                      fld.GetCustomAttributes(false).Any(attr => attr is XMLIDAttribute)))
                        Field.SetValue(Cached, _ID);
                    HaveCache = true;
                }
                return Cached;
            }
            set
            {
                Cached = value;
                HaveCache = true;
            }
        }

        /// <summary>Determines whether the object has been computed.</summary>
        public bool Evaluated { get { return HaveCache; } }

        /// <summary>Returns the ID used to refer to the object.</summary>
        public string ID { get { return _ID; } }
    }
}
