using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Xml
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
        /// it will receive the object passed in here as its value. Default is null.</param>
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
                return elem.Value;
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
                    else
                        outputList = type.GetConstructor(new Type[] { }).Invoke(new object[] { });

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

                    var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (constructor == null)
                        throw new MissingMethodException("The type {0} has no parameterless constructor.".Fmt(type.FullName));
                    try { ret = constructor.Invoke(Type.EmptyTypes); }
                    catch (Exception e) { throw new Exception("The parameterless constructor of the type {0} threw an exception.".Fmt(type.FullName), e); }

                    foreach (var field in type.GetAllFields())
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
                                throw new Exception("The field {0}.{1} uses the [XmlFollowId] attribute, but does not have the type XmlDeferredObject<T> for some T.".Fmt(type.FullName, field.Name));

                            Type innerType = field.FieldType.GetGenericArguments()[0];
                            var attr = elem.Attribute(rFieldName);
                            if (attr != null)
                            {
                                string newFile = Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + attr.Value + ".xml");
                                field.SetValue(ret,
                                    // new XmlDeferredObject<InnerType>(attr.Value, method loadObjectFromXmlFile, new[] { innerType, newFile, baseDir, ret })
                                    typeof(XmlDeferredObject<>).MakeGenericType(innerType)
                                        .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                        .Invoke(new object[] {
                                            attr.Value,
                                            typeof(XmlClassify).GetMethod("loadObjectFromXmlFile", new Type[] { typeof(Type), typeof(string), typeof(string), typeof(object) }),
                                            null,
                                            new object[] { innerType, newFile, baseDir, ret }
                                        })
                                );
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
        /// <param name="filename">Path and filename of the XML file to be created.
        /// If the file already exists, it will be overwritten.</param>
        public static void SaveObjectToXmlFile<T>(T saveObject, string filename)
        {
            string baseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            saveObjectToXmlFile(saveObject, filename, baseDir);
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
            saveObjectToXmlFile(saveObject, filename, baseDir);
        }

        private static void saveObjectToXmlFile(object saveObject, string filename, string baseDir)
        {
            var x = objectToXElement(saveObject, baseDir, "item");
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
            return objectToXElement(saveObject, null, "item");
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
            return objectToXElement(saveObject, baseDir, "item");
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
            return objectToXElement(saveObject, baseDir, tagName);
        }

        private static XElement objectToXElement(object saveObject, string baseDir, string tagName)
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
                elem.Add(saveObject);
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
                        var tag = objectToXElement(value, baseDir, "item");
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
                    foreach (var field in saveType.GetAllFields())
                    {
                        string rFieldName = field.Name.TrimStart('_');
                        var attribs = field.GetCustomAttributes(false);

                        // [XmlIgnore], [XmlParent]
                        if (attribs.Any(x => x is XmlIgnoreAttribute || x is XmlParentAttribute))
                            continue;

                        // [XmlFollowId]
                        else if (attribs.Any(x => x is XmlFollowIdAttribute))
                        {
                            object saveValue = field.GetValue(saveObject);

                            if (field.FieldType.GetGenericTypeDefinition() != typeof(XmlDeferredObject<>))
                                throw new Exception("A field that uses the [XmlFollowId] attribute must have the type XmlDeferredObject<T> for some T.");

                            Type innerType = field.FieldType.GetGenericArguments()[0];
                            string id = (string) field.FieldType.GetProperty("Id").GetValue(saveValue, null);
                            elem.Add(new XElement(rFieldName, new XAttribute("id", id)));

                            if ((bool) field.FieldType.GetProperty("Evaluated").GetValue(saveValue, null))
                                saveObjectToXmlFile(field.FieldType.GetProperty("Value").GetValue(saveValue, null), Path.Combine(baseDir, innerType.Name + Path.DirectorySeparatorChar + id + ".xml"), baseDir);
                        }

                        // Fields with no special [Xml...] attributes
                        else
                            elem.Add(objectToXElement(field.GetValue(saveObject), baseDir, rFieldName));
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
                    foreach (var field in _cached.GetType().GetAllFields().Where(fld => fld.FieldType == typeof(string) && fld.IsDefined(typeof(XmlIdAttribute), false)))
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
