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
    public static class XMLClassify
    {
        public static T ReadObjectFromXMLFile<T>(string Filename, object ParentNode) where T : class, new()
        {
            string BaseDir = Filename.Contains(Path.DirectorySeparatorChar) ? Filename.Remove(Filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            return ReadObjectFromXMLFile<T>(Filename, BaseDir, ParentNode);
        }

        public static T ReadObjectFromXMLFile<T>(string Filename, string BaseDir, object ParentNode) where T : class, new()
        {
            var sr = new StreamReader(Filename, Encoding.UTF8);
            XElement xe = XElement.Load(sr);
            sr.Close();
            return ReadObjectFromXElement<T>(xe, BaseDir, ParentNode);
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

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLAttributeAttribute : Attribute
    {
        public bool FollowID = false;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLContentsAttribute : Attribute
    {
        public string TagName;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLContentsDictionaryAttribute : XMLContentsAttribute
    {
        public XMLContentsDictionaryAttribute(string Key) { this.Key = Key; }
        public string Key;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLParentAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLIDAttribute : Attribute
    {
    }

    public interface IXMLAttributeValue<T>
    {
        string ID { get; }
        T Value { get; set; }
        bool Evaluated { get; }
    }

    public class XMLAttributeValue<T> : IXMLAttributeValue<T>
    {
        public XMLAttributeValue(string ID, Func<T> Generator) { _ID = ID; this.Generator = Generator; }
        public XMLAttributeValue(string ID, T Value) { _ID = ID; Cached = Value; HaveCache = true; }
        public XMLAttributeValue(string ID, MethodInfo GeneratorMethod, object GeneratorObject, object[] GeneratorParams)
        {
            _ID = ID;
            this.Generator = () => (T) GeneratorMethod.Invoke(GeneratorObject, GeneratorParams);
        }

        private Func<T> Generator;
        private T Cached;
        private bool HaveCache = false;
        private string _ID;

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

        public bool Evaluated { get { return HaveCache; } }
        public string ID { get { return _ID; } }
    }
}
