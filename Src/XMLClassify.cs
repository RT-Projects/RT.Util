using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Collections;

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

        public static T ReadObjectFromXElement<T>(XElement XElem, string BaseDir, object ParentNode) where T : class, new()
        {
            if (typeof(T) == typeof(string))
                return XElem.FirstNode.ToString() as T;
            if (typeof(T) == typeof(int))
                return (T) (object) (int.Parse(XElem.FirstNode.ToString()));
            if (typeof(T) == typeof(DateTime))
                return DateTime.Parse(XElem.FirstNode.ToString()) as T;

            T t = new T();

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var Attrib in Field.GetCustomAttributes(false))
                {
                    // without FollowID
                    if (Attrib is XMLAttributeAttribute && !((XMLAttributeAttribute) Attrib).FollowID)
                    {
                        if (Field.FieldType != typeof(string) && Field.FieldType != typeof(int) && Field.FieldType != typeof(DateTime))
                            throw new ArgumentException(string.Format("The type of {0}.{1} is {2}, but only string, int and DateTime are supported for XML attributes.", typeof(T), Field.Name, Field.FieldType));
                        Func<XAttribute, bool> FindAttr = x => x.Name == Field.Name || (Field.Name.StartsWith("_") && x.Name == Field.Name.Substring(1));
                        if (XElem.Attributes().Any(FindAttr))
                        {
                            var attr = XElem.Attributes().First(FindAttr);
                            Field.SetValue(t,
                                Field.FieldType == typeof(int) ? (object) int.Parse(attr.Value) :
                                Field.FieldType == typeof(DateTime) ? (object) DateTime.Parse(attr.Value) :
                                attr.Value
                            );
                        }
                        break;
                    }
                    // with FollowID, using IDeferred<>
                    else if (Attrib is XMLAttributeAttribute && Field.FieldType.GetGenericTypeDefinition() == typeof(IDeferred<>))
                    {
                        Type InnerType = Field.FieldType.GetGenericArguments().First();
                        Func<XAttribute, bool> FindAttr = x => x.Name == Field.Name || (Field.Name.StartsWith("_") && x.Name == Field.Name.Substring(1));
                        if (XElem.Attributes().Any(FindAttr))
                        {
                            string NewFile = BaseDir + Path.DirectorySeparatorChar + InnerType.Name + Path.DirectorySeparatorChar + XElem.Attributes().First(FindAttr).Value + ".xml";
                            Field.SetValue(t,
                                // new DeferredClass<InnerType>(XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, null))
                                typeof(DeferredClass<>).MakeGenericType(InnerType)
                                    .GetConstructor(new Type[] { typeof(MethodInfo), typeof(object), typeof(object[]) })
                                    .Invoke(new object[] { 
                                        // XMLClassify.ReadObjectFromXMLFile<InnerType>(NewFile, BaseDir, null)
                                        typeof(XMLClassify).GetMethod("ReadObjectFromXMLFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                            .MakeGenericMethod(InnerType),
                                        null,
                                        new object[] { NewFile, BaseDir, null }
                                    })
                            );
                        }
                        break;
                    }
                    // with FollowID, without IDeferred<>
                    else if (Attrib is XMLAttributeAttribute)
                    {
                        Func<XAttribute, bool> FindAttr = x => x.Name == Field.Name || (Field.Name.StartsWith("_") && x.Name == Field.Name.Substring(1));
                        if (XElem.Attributes().Any(FindAttr))
                        {
                            string NewFile = BaseDir + Path.DirectorySeparatorChar + Field.FieldType.Name + Path.DirectorySeparatorChar + XElem.Attributes().First(FindAttr).Value + ".xml";
                            Field.SetValue(t,
                                // XMLClassify.ReadObjectFromXMLFile<Field.FieldType>(NewFile, BaseDir, null)
                                typeof(XMLClassify).GetMethod("ReadObjectFromXMLFile", new Type[] { typeof(string), typeof(string), typeof(object) })
                                    .MakeGenericMethod(Field.FieldType).Invoke(null, new object[] { NewFile, BaseDir, null })
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
                        if (Field.FieldType == typeof(string) || Field.FieldType == typeof(int) || Field.FieldType == typeof(DateTime))
                        {
                            var txt = XElem.FirstNode.ToString();
                            Field.SetValue(t,
                                Field.FieldType == typeof(int) ? (object) int.Parse(txt) :
                                Field.FieldType == typeof(DateTime) ? (object) DateTime.Parse(txt) :
                                txt
                            );
                        }
                        else if (Field.FieldType.IsGenericType && 
                            Field.FieldType.GetGenericArguments().Count() == 2 &&
                            typeof(IDictionary<,>).MakeGenericType(Field.FieldType.GetGenericArguments()[0],Field.FieldType.GetGenericArguments()[1]).IsAssignableFrom(Field.FieldType))
                        {
                            string Key = ((XMLContentsDictionaryAttribute) Attrib).Key;
                            Type KeyType = Field.FieldType.GetGenericArguments()[0];
                            if (KeyType != typeof(string) && KeyType != typeof(int))
                                throw new ArgumentException(string.Format("The type of the dictionary key for {0}.{1} is {2}, but only string and int are supported.",
                                    typeof(T), Field.Name, KeyType));
                            Type InnerType = Field.FieldType.GetGenericArguments()[1];
                            if (TagName == null)
                                TagName = InnerType.Name;

                            if (Field.GetValue(t) == null)
                                Field.SetValue(t, Field.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                            var MyDictionary = Field.GetValue(t);

                            // MyDictionary.Clear();
                            Field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(MyDictionary, new object[] { });

                            foreach (var DicElem in XElem.Elements(TagName))
                            {
                                if (!DicElem.Attributes(Key).Any())
                                    throw new ArgumentException(string.Format("While reading {0}.{1}, I expected the XML tag to have an attribute called \"{2}\", but it didn't.",
                                        typeof(T), Field.Name, Key));
                                object ConvertedKey = KeyType == typeof(string) ? (object) DicElem.Attributes(Key).First().Value : int.Parse(DicElem.Attributes(Key).First().Value);

                                // MyDictionary.Add<InnerType>(...)
                                Field.FieldType.GetMethod("Add", new Type[] { KeyType, InnerType }).Invoke(MyDictionary, new object[] { ConvertedKey,
                                    // XMLClassify.ReadObjectFromXElement<InnerType>(DicElem, BaseDir, t)
                                    typeof(XMLClassify).GetMethod("ReadObjectFromXElement", new Type[] { typeof(XElement), typeof(string), typeof(object) })
                                        .MakeGenericMethod(InnerType).Invoke(null, new object[] { DicElem, BaseDir, t })
                                });
                            }
                            Field.SetValue(t, MyDictionary);
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

                            // MyCollection.Clear();
                            Field.FieldType.GetMethod("Clear", new Type[] { }).Invoke(MyCollection, new object[] { });

                            foreach (var ListElem in XElem.Elements(TagName))
                            {
                                // MyCollection.Add<InnerType>(...)
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
                                "The type of {0}.{1} is {2}, but only string, int, DateTime and classes that implement ICollection<> (such as List<>) or IDictionary<> (such as Dictionary<>) are supported for XML tag contents. If you use an IDictionary<>, use the [XMLContentsDictionary] attribute instead of [XMLContents].",
                                typeof(T), Field.Name, Field.FieldType
                            ));
                        }
                        break;
                    }
                }
            }

            return t;
        }

        public static void SaveObjectAsXML<T>(T Object, string Filename) where T : class, new()
        {
            XNode Node = ObjectAsXML(Object);
            if (Node is XElement)
                ((XElement) Node).Save(Filename);
            else
                File.WriteAllText(Filename, Node.ToString(), Encoding.UTF8);
        }

        public static XNode ObjectAsXML<T>(T Object)
        {
            if (typeof(T) == typeof(string))
                return new XText(Object as string);
            if (typeof(T) == typeof(int))
                return new XText(Object.ToString());
            if (typeof(T) == typeof(DateTime))
                return new XText(typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Object, new object[] { "u" }).ToString());

            XElement Elem = new XElement(typeof(T).Name);

            foreach (var Field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var Attrib in Field.GetCustomAttributes(false))
                {
                    if (Attrib is XMLAttributeAttribute && !((XMLAttributeAttribute) Attrib).FollowID)
                    {
                        if (Field.FieldType != typeof(string) && Field.FieldType != typeof(int) && Field.FieldType != typeof(DateTime))
                            throw new ArgumentException(string.Format("The type of {0}.{1} is {2}, but only string, int and DateTime are supported for XML attributes.", typeof(T), Field.Name, Field.FieldType));
                        Elem.SetAttributeValue(Field.Name.TrimStart('_'),
                            Field.FieldType == typeof(int) || Field.FieldType == typeof(string) ? Field.GetValue(Object).ToString() :
                            typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Field.GetValue(Object), new object[] { "u" })
                        );
                        break;
                    }
                    else if (Attrib is XMLAttributeAttribute)   // with FollowID set
                    {
                        // not implemented yet
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
                        if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            Type InnerType = Field.FieldType.GetGenericArguments().First();
                            if (TagName == null)
                                TagName = InnerType.Name;
                            var Enumerator = typeof(IEnumerable<>).MakeGenericType(InnerType).GetMethod("GetEnumerator").Invoke(Field.GetValue(Object), new object[] { }) as IEnumerator;
                            while (Enumerator.MoveNext())
                                Elem.Add(typeof(XMLClassify).GetMethod("ObjectAsXML").MakeGenericMethod(InnerType).Invoke(null, new object[] { Enumerator.Current }));
                            break;
                        }
                        else if (Field.FieldType == typeof(string) || Field.FieldType == typeof(int) || Field.FieldType == typeof(DateTime))
                        {
                            Elem.Add(new XElement(Field.Name.TrimStart('_'),
                                Field.FieldType == typeof(int) || Field.FieldType == typeof(string) ? Field.GetValue(Object).ToString() :
                                typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) }).Invoke(Field.GetValue(Object), new object[] { "u" })
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
        public string Key;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLSubtagAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class XMLParentAttribute : Attribute
    {
    }

    public interface IDeferred<T>
    {
        T Value { get; set; }
        bool Evaluated { get; }
    }

    public class DeferredClass<T> : IDeferred<T>
    {
        public DeferredClass(Func<T> Generator) { this.Generator = Generator; }
        public DeferredClass(MethodInfo GeneratorMethod, object GeneratorObject, object[] GeneratorParams)
        {
            this.Generator = () => (T) GeneratorMethod.Invoke(GeneratorObject, GeneratorParams);
        }
        public DeferredClass(T Value) { this.Cached = Value; }

        private Func<T> Generator;
        private T Cached;
        private bool HaveCache = false;

        public T Value
        {
            get
            {
                if (!HaveCache)
                {
                    Cached = Generator();
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
    }
}
