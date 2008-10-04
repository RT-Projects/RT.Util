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

namespace RT.Util
{
    /*
     *  ####     NOTES     ####
     *
     *  The current situation is that [XMLAttribute] or [XMLContents] MUST be specified in order to store a field
     *  in the XML file or restore it from the XML file. All other fields will be ignored.
     *
     *  The plan for the future, however, is to store fields by default and use an [XMLIgnore] attribute on the fields
     *  that should not be stored or restored. Strings, ints, DateTimes and enums would be stored on XML tag attributes
     *  by default, everything else in the tag contents. This renders the [XMLAttribute] attribute completely useless,
     *  so it will be removed. [XMLContents] would no longer serve any pragmatic use, and only make XML files larger,
     *  so I'm considering abolishing that too.
     *
     *  Further, the tag names are currently sometimes derived from the class names, sometimes from the field names,
     *  unless a custom tag name is specified. Using class names for tag names doesn't really make sense; the tag name
     *  should just be the field name. The type of the field can be inferred from the class, so it need not be stored
     *  in the XML file. This way the XML file will survive class renames, too (though unfortunately not field renames).
     *
     *  Lastly, for lists and dictionaries, I'm considering using only the tagname "item", and the attribute name "key"
     *  for dictionaries. Dictionaries whose value type is also a type storable in an XML attribute would use the
     *  attribute name "value", otherwise they would be stored in a subtag called "value". This makes it darn obvious
     *  that the things listed are list items or key/value pairs, respectively.
     *
     *  It would be nice to also have an IXMLSerializable interface which would allow a class to define its own custom
     *  XML storage format. However, at the moment I don't anticipate that I need it.
     *
     */


    /// <summary>
    /// Provides static methods to save objects of (almost) arbitrary classes into XML files and load them again.
    /// The functionality is similar to XmlSerializer, but uses the newer C# XML API and is also more full-featured.
    /// </summary>
    public static class XMLClassify
    {
        public static T ReadObjectFromXMLFile<T>(string Filename) where T : class, new()
        {
            return ReadObjectFromXMLFile<T>(Filename, null);
        }

        public static T ReadObjectFromXMLFile<T>(string Filename, object ParentNode) where T : class, new()
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            return ReadObjectFromXMLFile<T>(Filename, BaseDir, ParentNode);
        }

        public static T ReadObjectFromXMLFile<T>(string Filename, string BaseDir, object ParentNode) where T : class, new()
        {
            try
            {
                var sr = new StreamReader(Filename, Encoding.UTF8);
                XElement xe = XElement.Load(sr);
                sr.Close();
                return ReadObjectFromXElement<T>(xe, BaseDir, ParentNode);
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static T ReadObjectFromXElement<T>(XElement XElem, string BaseDir, object ParentNode)
        {
            if (typeof(T) == typeof(string))
                return (T) (object) XElem.FirstNode.ToString();
            if (typeof(T) == typeof(int))
                return (T) (object) (int.Parse(XElem.FirstNode.ToString()));
            if (typeof(T) == typeof(DateTime))
                return (T) (object) DateTime.Parse(XElem.FirstNode.ToString());
            if (typeof(T).IsEnum)
            {
                try { return (T) XElem.FirstNode.ToString().ToStaticValue<T>(); }
                catch { }
            }

            // The following is equivalent to
            //      T t = new T();
            // but we can't do that because doing so requires a new() constraint on T,
            // but such a constraint precludes strings, enums etc. from working.
            T t = (T) typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { });

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var Attrib in Field.GetCustomAttributes(false))
                {
                    // without FollowID
                    if (Attrib is XMLAttributeAttribute && !((XMLAttributeAttribute) Attrib).FollowID)
                    {
                        if (Field.FieldType != typeof(string) && Field.FieldType != typeof(int) && Field.FieldType != typeof(DateTime) && !Field.FieldType.IsEnum)
                            throw new ArgumentException(string.Format("The type of {0}.{1} is {2}, but only string, int, DateTime and enums are supported for XML attributes.", typeof(T), Field.Name, Field.FieldType.FullName));
                        Func<XAttribute, bool> FindAttr = x => x.Name == Field.Name || (Field.Name.StartsWith("_") && x.Name == Field.Name.Substring(1));
                        if (XElem.Attributes().Any(FindAttr))
                        {
                            var attr = XElem.Attributes().First(FindAttr);
                            Field.SetValue(t,
                                Field.FieldType == typeof(int) ? (object) int.Parse(attr.Value) :
                                Field.FieldType == typeof(DateTime) ? (object) DateTime.Parse(attr.Value) :
                                Field.FieldType.IsEnum ? attr.Value.ToStaticValue(Field.FieldType) :
                                attr.Value
                            );
                        }
                        break;
                    }
                    // with FollowID
                    else if (Attrib is XMLAttributeAttribute && Field.FieldType.IsGenericType)
                    {
                        if (!typeof(IXMLAttributeValue<>).MakeGenericType(Field.FieldType.GetGenericArguments()[0]).IsAssignableFrom(Field.FieldType))
                            throw new Exception("A field that uses the [XMLAttribute] attribute with FollowID set to true must have a type compatible with IXMLAttributeValue<T> for some T.");
                        Type InnerType = Field.FieldType.GetGenericArguments()[0];
                        Func<XAttribute, bool> FindAttr = x => x.Name == Field.Name || (Field.Name.StartsWith("_") && x.Name == Field.Name.Substring(1));
                        if (XElem.Attributes().Any(FindAttr))
                        {
                            string ID = XElem.Attributes().First(FindAttr).Value;
                            string NewFile = BaseDir + Path.DirectorySeparatorChar + InnerType.Name + Path.DirectorySeparatorChar + ID + ".xml";
                            Field.SetValue(t,
                                // new XMLAttributeValue<InnerType>(ID, XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, t))
                                typeof(XMLAttributeValue<>).MakeGenericType(InnerType)
                                    .GetConstructor(new Type[] { typeof(string), typeof(MethodInfo), typeof(object), typeof(object[]) })
                                    .Invoke(new object[] {
                                        ID,
                                        // XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, t)
                                        typeof(XMLClassify).GetMethod("ReadObjectFromXMLFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                            .MakeGenericMethod(InnerType),
                                        null,
                                        new object[] { NewFile, BaseDir, t }
                                    })
                            );
                        }
                        break;
                    }
                    else if (Attrib is XMLParentAttribute)
                    {
                        Field.SetValue(t, ParentNode);
                        break;
                    }
                    else if (Attrib is XMLContentsAttribute)
                    {
                        string TagName = ((XMLContentsAttribute) Attrib).TagName;
                        if (Field.FieldType == typeof(string))
                            Field.SetValue(t, XElem.FirstNode.ToString());
                        else if (Field.FieldType == typeof(int))
                            Field.SetValue(t, int.Parse(XElem.FirstNode.ToString()));
                        else if (Field.FieldType == typeof(DateTime))
                            Field.SetValue(t, DateTime.Parse(XElem.FirstNode.ToString()));
                        else if (Field.FieldType.IsEnum)
                            Field.SetValue(t, XElem.FirstNode.ToString().ToStaticValue(Field.FieldType));
                        else if (Field.FieldType.IsGenericType &&
                            Field.FieldType.GetGenericArguments().Count() == 2 &&
                            typeof(IDictionary<,>).MakeGenericType(Field.FieldType.GetGenericArguments()).IsAssignableFrom(Field.FieldType))
                        {
                            if (!(Attrib is XMLContentsDictionaryAttribute))
                                throw new Exception("Use the [XMLContentsDictionary(...)] attribute for dictionaries instead of [XMLContents].");
                            string Key = ((XMLContentsDictionaryAttribute) Attrib).Key;
                            if (Key == null || Key.Length == 0)
                                throw new Exception("You must specify a non-null and non-empty key name in the [XMLContentsDictionary(...)] attribute.");

                            Type InnerType = Field.FieldType.GetGenericArguments()[1];
                            if (TagName == null)
                                TagName = InnerType.Name;

                            Type KeyType = Field.FieldType.GetGenericArguments()[0];
                            if (KeyType != typeof(string) && KeyType != typeof(int))
                                throw new ArgumentException(string.Format("The type of the dictionary key for {0}.{1} is {2}, but only string and int are supported.",
                                    typeof(T).FullName, Field.Name, KeyType.FullName));

                            if (Field.GetValue(t) == null)
                                Field.SetValue(t, Field.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                            var MyDictionary = Field.GetValue(t);

                            // MyDictionary.Clear();
                            Field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(MyDictionary, new object[] { });

                            foreach (var DicElem in XElem.Elements(TagName))
                            {
                                if (!DicElem.Attributes(Key).Any())
                                    throw new ArgumentException(string.Format("While reading {0}.{1}, I expected the XML tag to have an attribute called \"{2}\", but it didn't.",
                                        typeof(T).FullName, Field.Name, Key));

                                // MyDictionary.Add(KeyType Key, InnerType Value)
                                Field.FieldType.GetMethod("Add", new Type[] { KeyType, InnerType }).Invoke(MyDictionary, new object[] { 
                                    KeyType == typeof(string) ? (object) DicElem.Attributes(Key).First().Value : int.Parse(DicElem.Attributes(Key).First().Value),
                                    // XMLClassify.ReadObjectFromXElement<InnerType>(DicElem, BaseDir, t)
                                    typeof(XMLClassify).GetMethod("ReadObjectFromXElement", new Type[] { typeof(XElement), typeof(string), typeof(object) })
                                        .MakeGenericMethod(InnerType).Invoke(null, new object[] { DicElem, BaseDir, t })
                                });
                            }
                        }
                        else if (Field.FieldType.IsGenericType &&
                            typeof(ICollection<>).MakeGenericType(Field.FieldType.GetGenericArguments()[0]).IsAssignableFrom(Field.FieldType))
                        {
                            Type InnerType = Field.FieldType.GetGenericArguments().First();
                            if (TagName == null)
                                TagName = InnerType.Name;

                            if (Field.GetValue(t) == null)
                                Field.SetValue(t, Field.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                            var MyCollection = Field.GetValue(t);

                            // MyCollection.Clear()
                            Field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(MyCollection, new object[] { });

                            foreach (var ListElem in XElem.Elements(TagName))
                            {
                                // MyCollection.Add(InnerType Value)
                                Field.FieldType.GetMethod("Add", new Type[] { InnerType }).Invoke(MyCollection, new object[] {
                                    // XMLClassify.ReadObjectFromXElement<InnerType>(ListElem, BaseDir, t)
                                    typeof(XMLClassify).GetMethod("ReadObjectFromXElement", new Type[] { typeof(XElement), typeof(string), typeof(object) })
                                        .MakeGenericMethod(InnerType).Invoke(null, new object[] { ListElem, BaseDir, t })
                                });
                            }
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(
                                "The type of {0}.{1} is {2}, but the only types supported for XML tag contents are: " +
                                "string; int; DateTime; enums; classes that implement ICollection<> (such as List<>); " +
                                "classes that implement IDictionary<> (such as Dictionary<>). If you use an IDictionary<>, " +
                                "use the [XMLContentsDictionary] attribute instead of [XMLContents].",
                                typeof(T).FullName, Field.Name, Field.FieldType.FullName
                            ));
                        }
                        break;
                    }
                }
            }

            return t;
        }

        public static void SaveObjectAsXML(Type ObjectType, object Object, string Filename, string BaseDir)
        {
            XNode Node = ObjectAsXML(ObjectType, Object, null, BaseDir);
            if (Node is XElement)
                ((XElement) Node).Save(Filename);
            else
                File.WriteAllText(Filename, Node.ToString(), Encoding.UTF8);
        }

        public static void SaveObjectAsXML<T>(T Object, string Filename) where T : class, new()
        {
            SaveObjectAsXML(typeof(T), Object, Filename, ".");
        }

        public static XNode ObjectAsXML<T>(T Object, string BaseDir)
        {
            return ObjectAsXML(typeof(T), Object, null, BaseDir);
        }

        public static XNode ObjectAsXML(Type T, object Object, string OverrideTagName, string BaseDir)
        {
            XText Txt = null;
            if (T == typeof(string))
                Txt = new XText(Object as string);
            else if (T == typeof(int) || T.IsEnum)
                Txt = new XText(Object.ToString());
            else if (T == typeof(DateTime))
                Txt = new XText(typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Object, new object[] { "u" }).ToString());

            if (Txt != null)
                return OverrideTagName == null ? (XNode)Txt : new XElement(OverrideTagName, Txt);

            XElement Elem = new XElement(OverrideTagName == null ? T.Name : OverrideTagName);

            foreach (var Field in T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var Attrib in Field.GetCustomAttributes(false))
                {
                    // without FollowID
                    if (Attrib is XMLAttributeAttribute && !((XMLAttributeAttribute) Attrib).FollowID)
                    {
                        if (Field.FieldType != typeof(string) && Field.FieldType != typeof(int) && Field.FieldType != typeof(DateTime) && !Field.FieldType.IsEnum)
                            throw new ArgumentException(string.Format("The type of {0}.{1} is {2}, but only string, int, DateTime and enums are supported for XML attributes.", T, Field.Name, Field.FieldType));
                        if (Field.GetValue(Object) != null)
                            Elem.SetAttributeValue(Field.Name.TrimStart('_'),
                                Field.FieldType == typeof(DateTime)
                                // DateTime.ToString("u")
                                    ? typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Field.GetValue(Object), new object[] { "u" })
                                    : Field.GetValue(Object).ToString()
                            );
                        break;
                    }
                    // with FollowID
                    else if (Attrib is XMLAttributeAttribute && Field.FieldType.IsGenericType &&
                        typeof(IXMLAttributeValue<>).MakeGenericType(Field.FieldType.GetGenericArguments()[0]).IsAssignableFrom(Field.FieldType))
                    {
                        Type InnerType = Field.FieldType.GetGenericArguments()[0];
                        string ID = (string) typeof(IXMLAttributeValue<>).MakeGenericType(InnerType).GetProperty("ID").GetValue(Field.GetValue(Object), null);
                        Elem.SetAttributeValue(Field.Name.TrimStart('_'), ID);
                        if ((bool) typeof(IXMLAttributeValue<>).MakeGenericType(InnerType).GetProperty("Evaluated").GetValue(Field.GetValue(Object), null))
                            XMLClassify.SaveObjectAsXML(InnerType,
                                typeof(IXMLAttributeValue<>).MakeGenericType(InnerType).GetProperty("Value").GetValue(Field.GetValue(Object), null),
                                BaseDir + Path.DirectorySeparatorChar + InnerType.Name + Path.DirectorySeparatorChar + ID + ".xml", BaseDir);
                        break;
                    }
                    else if (Attrib is XMLParentAttribute)
                    {
                        // ignore this
                        break;
                    }
                    else if (Attrib is XMLContentsAttribute)
                    {
                        string TagName = ((XMLContentsAttribute) Attrib).TagName;
                        if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericArguments().Count() == 2 &&
                            typeof(IDictionary<,>).MakeGenericType(Field.FieldType.GetGenericArguments()).IsAssignableFrom(Field.FieldType))
                        {
                            if (!(Attrib is XMLContentsDictionaryAttribute))
                                throw new Exception("Use the [XMLContentsDictionary(...)] attribute for dictionaries instead of [XMLContents].");
                            string Key = ((XMLContentsDictionaryAttribute) Attrib).Key;
                            if (Key == null || Key.Length == 0)
                                throw new Exception("You must specify a non-null and non-empty key name in the [XMLContentsDictionary(...)] attribute.");

                            Type InnerType = Field.FieldType.GetGenericArguments()[1];
                            if (TagName == null)
                                TagName = InnerType.Name;

                            Type KeyType = Field.FieldType.GetGenericArguments()[0];
                            if (KeyType != typeof(string) && KeyType != typeof(int))
                                throw new ArgumentException(string.Format("The type of the dictionary key for {0}.{1} is {2}, but only string and int are supported.",
                                    T.FullName, Field.Name, KeyType.FullName));

                            var Collection = typeof(IEnumerable).GetMethod("GetEnumerator").Invoke(Field.GetValue(Object), new object[] { }) as IEnumerator;
                            while (Collection.MoveNext())
                            {
                                Type KvpType = typeof(KeyValuePair<,>).MakeGenericType(KeyType, InnerType);
                                XElement NewElem = XMLClassify.ObjectAsXML(InnerType, KvpType.GetProperty("Value").GetValue(Collection.Current, null), TagName, BaseDir) as XElement;
                                NewElem.SetAttributeValue(Key, KvpType.GetProperty("Key").GetValue(Collection.Current, null));
                                Elem.Add(NewElem);
                            }
                            break;
                        }
                        else if (Field.FieldType.IsGenericType && typeof(ICollection<>).MakeGenericType(Field.FieldType.GetGenericArguments()[0]).IsAssignableFrom(Field.FieldType))
                        {
                            Type InnerType = Field.FieldType.GetGenericArguments()[0];
                            if (TagName == null)
                                TagName = InnerType.Name;
                            var Collection = typeof(IEnumerable).GetMethod("GetEnumerator").Invoke(Field.GetValue(Object), new object[] { }) as IEnumerator;
                            while (Collection.MoveNext())
                                Elem.Add(XMLClassify.ObjectAsXML(InnerType, Collection.Current, TagName, BaseDir));
                            break;
                        }
                        else if (Field.FieldType.IsEnum || Field.FieldType == typeof(string) || Field.FieldType == typeof(int) || Field.FieldType == typeof(DateTime))
                        {
                            Elem.Add(new XElement(Field.Name.TrimStart('_'),
                                Field.FieldType == typeof(DateTime)
                                    ? typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Field.GetValue(Object), new object[] { "u" })
                                    : Field.GetValue(Object).ToString()
                            ));
                        }
                    }
                }
            }

            return Elem;
        }
    }

    /// <summary>
    /// Use this attribute to specify that a field in a class should be stored as an XML attribute on the object's tag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLAttributeAttribute : Attribute
    {
        /// <summary>
        /// If this is set to false, the value is stored literally. This is only allowed for int, string, DateTime and enums.
        /// If this is set to true, the XML attribute will contain an ID that points to another, separate XML file which in
        /// turn contains the actual object. This is only allowed on fields of type <see cref="IXMLAttributeValue&lt;T&gt;"/>
        /// for some class type T. Use <see cref="IXMLAttributeValue&lt;T&gt;.Value"/> to retrieve the object. This retrieval
        /// is deferred until first use. Use <see cref="IXMLAttributeValue&lt;T&gt;.ID"/> to retrieve the ID used to reference
        /// the object. You can also capture the ID into the class T by using the [XMLID] attribute within that class.
        /// </summary>
        public bool FollowID = false;
    }

    /// <summary>
    /// Use this attribute to specify that a field in a class should be stored in the contents of the object's XML tag
    /// as one or more child nodes. If the field's type is a collection or dictionary, this will generate several child
    /// nodes directly within the object's XML node. Otherwise, it will generate a single child node. If the field's
    /// type is a dictionary, use the [XMLContentsDictionary] attribute instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLContentsAttribute : Attribute
    {
        /// <summary>
        /// The name of the tag to use. If the field is a list or dictionary, there will be several tags of this name.
        /// </summary>
        public string TagName;
    }

    /// <summary>
    /// Use this attribute on a field of a dictionray type (a type that implements <see cref="IDictionary&lt;TKey, TValue&gt;"/>)
    /// to specify that it should be stored in the contents of the object's XML tag as a series of child nodes. If the field's
    /// type is not a dictionary, this will throw an exception at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLContentsDictionaryAttribute : XMLContentsAttribute
    {
        /// <summary>
        /// Constructor for the [XMLContentsDictionary] attribute.
        /// </summary>
        /// <param name="Key">
        /// Specifies the name of the XML attribute to use for storing each dictionary key.
        /// </param>
        public XMLContentsDictionaryAttribute(string Key) { this.Key = Key; }

        /// <summary>
        /// Specifies the name of the XML attribute to use for storing each dictionary key.
        /// </summary>
        public string Key;
    }

    /// <summary>
    /// A field with this attribute set will receive a reference to the object which was its parent node
    /// in the XML tree. If the field is of the wrong type, a runtime exception will occur. If there was
    /// no parent node, the field will be set to null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLParentAttribute : Attribute
    {
    }

    /// <summary>
    /// A field with this attribute set will receive the ID that was used to refer to the XML file
    /// that stores this object. See <see cref="XMLAttributeAttribute.FollowID"/>. The field must
    /// be of type <see langword="string"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class XMLIDAttribute : Attribute
    {
    }

    /// <summary>
    /// Provides methods to hold an object that has an ID and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public interface IXMLAttributeValue<T>
    {
        /// <summary>Returns the ID used to refer to the object.</summary>
        string ID { get; }
        /// <summary>Retrieves the object.</summary>
        T Value { get; set; }
        /// <summary>Determines whether the object has been computed.</summary>
        bool Evaluated { get; }
    }

    /// <summary>
    /// Provides the mechanisms to hold an object that has an ID and gets evaluated at first use.
    /// </summary>
    /// <typeparam name="T">The type of the contained object.</typeparam>
    public class XMLAttributeValue<T> : IXMLAttributeValue<T>
    {
        /// <summary>Initialises a deferred object using a delegate or lambda expression.</summary>
        /// <param name="ID">ID that refers to the object to be generated.</param>
        /// <param name="Generator">Function to generate the object.</param>
        public XMLAttributeValue(string ID, Func<T> Generator) { _ID = ID; this.Generator = Generator; }

        /// <summary>Initialises a deferred object using an actual object. Evaluation is not deferred.</summary>
        /// <param name="ID">ID that refers to the object.</param>
        /// <param name="Value">The object to store.</param>
        public XMLAttributeValue(string ID, T Value) { _ID = ID; Cached = Value; HaveCache = true; }

        /// <summary>Initialises a deferred object using a method reference and a set of parameters.</summary>
        /// <param name="ID">ID that refers to the object to be generated.</param>
        /// <paparam name="GeneratorMethod">Reference to the method that will return the computed object.</paparam>
        /// <param name="GeneratorObject">Object on which the method should be invoked. Use null for static methods.</param>
        /// <param name="GeneratorParams">Set of parameters for the method invocation.</param>
        public XMLAttributeValue(string ID, MethodInfo GeneratorMethod, object GeneratorObject, object[] GeneratorParams)
        {
            _ID = ID;
            this.Generator = () => (T) GeneratorMethod.Invoke(GeneratorObject, GeneratorParams);
        }

        private Func<T> Generator;
        private T Cached;
        private bool HaveCache = false;
        private string _ID;

        /// <summary>
        /// Sets or gets the object stored in this <see cref="XMLAttributeValue"/>. The property getter will
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
            }
        }

        /// <summary>Determines whether the object has been computed.</summary>
        public bool Evaluated { get { return HaveCache; } }
        /// <summary>Returns the ID used to refer to the object.</summary>
        public string ID { get { return _ID; } }
    }
}
