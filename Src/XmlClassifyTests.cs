using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.Xml
{
    [TestFixture]
    public sealed class XmlClassifyTests
    {
        private sealed class blankClass
        {
        }

        private sealed class basicClass
        {
            public int AnInt = -123;
            public ushort AUShort = 4747;
            public string AString = "str";
            public bool ABool;
            public ulong AULong;
            public double ADouble = 3.14;
            public decimal ADecimal = 3.1415m;
            public DateTime ADateTime;
            public int key = 25; // to test Dictionary keys
            public double? nullable1 = null;
            public double? nullable2 = 58.47;

            public void AssertEqual(basicClass actual)
            {
                Assert.AreEqual(AnInt, actual.AnInt);
                Assert.AreEqual(AUShort, actual.AUShort);
                Assert.AreEqual(AString, actual.AString);
                Assert.AreEqual(ABool, actual.ABool);
                Assert.AreEqual(AULong, actual.AULong);
                Assert.AreEqual(ADouble, actual.ADouble);
                Assert.AreEqual(ADecimal, actual.ADecimal);
                Assert.AreEqual(ADateTime, actual.ADateTime);
                Assert.AreEqual(key, actual.key);
                Assert.AreEqual(nullable1, actual.nullable1);
                Assert.AreEqual(nullable2, actual.nullable2);
            }
        }

        private sealed class classWithList
        {
            public List<string> List = new List<string>();
            public List<Dictionary<string, string>> ListDicts = new List<Dictionary<string, string>>();
        }

        private sealed class classWithDict
        {
            public Dictionary<string, string> Dict = new Dictionary<string, string>();
            public Dictionary<string, List<string>> DictLists = new Dictionary<string, List<string>>();
            public Dictionary<string, basicClass> DictClasses = new Dictionary<string, basicClass>();
        }

        private sealed class nestedClass
        {
            public basicClass Basic;
            public nestedClass Nested;

            public void AssertEqual(nestedClass actual)
            {
                Basic.AssertEqual(actual.Basic);
                if (Nested != null && actual.Nested != null)
                    Nested.AssertEqual(actual.Nested);
                else if (Nested == null && actual.Nested != null)
                    Assert.Fail("Nested classes: expected null, actual non-null.");
                else if (Nested != null && actual.Nested == null)
                    Assert.Fail("Nested classes: expected non-null, actual null.");
            }
        }

        private sealed class xmlClass
        {
            public string Str;
            public XElement Xml;
        }

        private sealed class classWithPrivateCtor
        {
            private classWithPrivateCtor()
            {
                Field = 9867;
            }

            public int Field = 7462;
        }

        private sealed class classWithNoCtor
        {
            private classWithNoCtor(int dummy)
            {
            }
        }

        private sealed class classWithThrowingCtor
        {
            private classWithThrowingCtor()
            {
                throw new Exception("Test exception");
            }
        }

        private sealed class classWithAdvancedTypes
        {
            // Should use List<int> when restoring
            public ICollection<int> IntCollection;
            public IList<int> IntList;

            // Should use Dictionary<int, string> when restoring
            public IDictionary<int, string> IntStringDic;

            // Should work as expected
            public SortedDictionary<int, string> SortedDic;
        }

        [Test]
        public void TestBlankClass()
        {
            XElement xel;
            xel = XmlClassify.ObjectToXElement(new blankClass());
            XmlClassify.ObjectFromXElement<blankClass>(xel);
        }

        [Test]
        public void TestBasicClass()
        {
            var clsEx = new basicClass()
            {
                AnInt = -876,
                AUShort = 9876,
                AString = "test String!",
                ABool = true,
                AULong = 9999999999999999999,
                ADouble = Math.PI,
                ADecimal = 123456.789123456m,
                ADateTime = DateTime.UtcNow,
                nullable1 = null,
                nullable2 = 47.48,
            };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<basicClass>(xel);

            clsEx.AssertEqual(clsAc);

            // Double check manually - in this test only.
            Assert.AreEqual(clsEx.AnInt, clsAc.AnInt);
            Assert.AreEqual(clsEx.AUShort, clsAc.AUShort);
            Assert.AreEqual(clsEx.AString, clsAc.AString);
            Assert.AreEqual(clsEx.ABool, clsAc.ABool);
            Assert.AreEqual(clsEx.AULong, clsAc.AULong);
            Assert.AreEqual(clsEx.ADecimal, clsAc.ADecimal);
            Assert.IsTrue(clsEx.nullable1 == null);
            Assert.IsTrue(clsAc.nullable1 == null);
            Assert.IsFalse(clsEx.nullable2 == null);
            Assert.IsFalse(clsAc.nullable2 == null);
            Assert.AreEqual(clsEx.nullable2.Value, clsAc.nullable2.Value);
        }

        [Test]
        public void TestStringNull()
        {
            var clsEx = new basicClass() { AString = null };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<basicClass>(xel);

            clsEx.AssertEqual(clsAc);
        }

        [Test]
        public void TestClassWithList()
        {
            var clsEx = new classWithList();
            clsEx.List.Add("abc");
            clsEx.List.Add(null);
            clsEx.List.Add("def");
            clsEx.ListDicts.Add(new Dictionary<string, string>());
            clsEx.ListDicts.Add(null);
            clsEx.ListDicts.Add(new Dictionary<string, string>() { { "abc", "def" }, { "key", "value" }, { "null", null } });
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<classWithList>(xel);

            assertList(clsEx.List, clsAc.List);
            assertListDict(clsEx.ListDicts, clsAc.ListDicts);
        }

        [Test]
        public void TestClassWithDict()
        {
            var clsEx = new classWithDict();
            clsEx.Dict.Add("abc", "def");
            clsEx.Dict.Add("key", "value");
            clsEx.Dict.Add("null", null);
            clsEx.DictLists = new Dictionary<string, List<string>>() {
                { "null", null },
                { "empty", new List<string>() },
                { "single", new List<string>() { "def" } },
                { "multple", new List<string>() { "one", null, "three" } }
            };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<classWithDict>(xel);

            assertDict(clsEx.Dict, clsAc.Dict);
            assertDictList(clsEx.DictLists, clsAc.DictLists);
        }

        [Test]
        public void TestDictSubclass()
        {
            var clsEx = new classWithDict();
            clsEx.DictClasses.Add("test1", new basicClass());
            clsEx.DictClasses.Add("test2", new basicClass() { AnInt = 63827, key = 429745 });
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<classWithDict>(xel);

            Assert.AreEqual(clsEx.DictClasses["test1"].AnInt, clsAc.DictClasses["test1"].AnInt);
            Assert.AreEqual(clsEx.DictClasses["test1"].key, clsAc.DictClasses["test1"].key);
            Assert.AreEqual(clsEx.DictClasses["test2"].AnInt, clsAc.DictClasses["test2"].AnInt);
            Assert.AreEqual(clsEx.DictClasses["test2"].key, clsAc.DictClasses["test2"].key);
        }

        [Test]
        public void TestClassWithXml()
        {
            var clsEx = new xmlClass()
            {
                Str = "control",
                Xml =
                    new XElement("bla", new XAttribute("attr1", "val1"),
                        new XElement("sub1",
                            new XElement("sub1.1")),
                        new XElement("sub2", new XAttribute("attr2", "val2")))
            };
            var xel = XmlClassify.ObjectToXElement(clsEx);
            var clsAc = XmlClassify.ObjectFromXElement<xmlClass>(xel);

            Assert.AreEqual(clsEx.Str, clsAc.Str);
            Assert.AreEqual(clsEx.Xml.ToString(SaveOptions.DisableFormatting), clsAc.Xml.ToString(SaveOptions.DisableFormatting));
        }

        [Test]
        public void TestNestedClass()
        {
            var nestedEx = new nestedClass()
            {
                Basic = new basicClass()
                {
                    AnInt = -5552346,
                    AString = "blah",
                },
                Nested = new nestedClass()
                {
                    Basic = new basicClass()
                    {
                        AString = "deep",
                        ADouble = 1.618,
                    }
                }
            };
            var xel = XmlClassify.ObjectToXElement(nestedEx);
            var nestedAc = XmlClassify.ObjectFromXElement<nestedClass>(xel);

            // Full comparison
            nestedEx.AssertEqual(nestedAc);

            // Sanity checks
            Assert.AreEqual(null, nestedEx.Nested.Nested);
            Assert.AreEqual(-123, nestedEx.Nested.Basic.AnInt);
            Assert.AreEqual(false, nestedEx.Nested.Basic.ABool);

            // Spot checks
            Assert.AreEqual(nestedEx.Basic.AnInt, nestedAc.Basic.AnInt);
            Assert.AreEqual(nestedEx.Basic.AString, nestedAc.Basic.AString);
            Assert.AreEqual(nestedEx.Nested.Basic.AString, nestedAc.Nested.Basic.AString);
            Assert.AreEqual(nestedEx.Nested.Basic.ADouble, nestedAc.Nested.Basic.ADouble);
            Assert.AreEqual(nestedEx.Nested.Nested, nestedAc.Nested.Nested);
        }

        private void assertDict<K, V>(Dictionary<K, V> expected, Dictionary<K, V> actual)
        {
            if (expected == null && actual == null)
                return;
            Assert.IsTrue(expected != null && actual != null);
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(key));
                Assert.AreEqual(expected[key], actual[key]);
            }
        }

        private void assertList<V>(List<V> expected, List<V> actual)
        {
            if (expected == null && actual == null)
                return;
            Assert.IsTrue(expected != null && actual != null);
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        private void assertDictList<K, V>(Dictionary<K, List<V>> expected, Dictionary<K, List<V>> actual)
        {
            if (expected == null && actual == null)
                return;
            Assert.IsTrue(expected != null && actual != null);
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(key));
                assertList(expected[key], actual[key]);
            }
        }

        private void assertListDict<K, V>(List<Dictionary<K, V>> expected, List<Dictionary<K, V>> actual)
        {
            if (expected == null && actual == null)
                return;
            Assert.IsTrue(expected != null && actual != null);
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
                assertDict(expected[i], actual[i]);
        }

        [Test]
        public void TestPartialLoad()
        {
            var elem = new XElement("item", new XElement("AULong", "987654"));
            var loaded = XmlClassify.ObjectFromXElement<basicClass>(elem);

            Assert.AreEqual(-123, loaded.AnInt);
            Assert.AreEqual(4747, loaded.AUShort);
            Assert.AreEqual("str", loaded.AString);
            Assert.AreEqual(987654L, loaded.AULong);
            Assert.AreEqual(3.14, loaded.ADouble);
            Assert.AreEqual(3.1415m, loaded.ADecimal);
        }

        [Test]
        public void TestConstructors()
        {
            var elem = new XElement("item");
            var loaded1 = XmlClassify.ObjectFromXElement<classWithPrivateCtor>(elem);
            Assert.AreEqual(9867, loaded1.Field);

            try
            {
                var loaded2 = XmlClassify.ObjectFromXElement<classWithThrowingCtor>(elem);
                Assert.Fail("Expected exception");
            }
            catch (Exception) { }

            try
            {
                var loaded3 = XmlClassify.ObjectFromXElement<classWithNoCtor>(elem);
                Assert.Fail("Expected System.Exception");
            }
            catch { }
        }

        [Test]
        public void TestAdvancedClass()
        {
            var x = new classWithAdvancedTypes
            {
                IntCollection = new int[] { 1, 2, 3, 4 },
                IntList = new int[] { 5, 6, 7, 8 },
                IntStringDic = new SortedDictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" } },
                SortedDic = new SortedDictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" } }
            };

            var xml = XmlClassify.ObjectToXElement(x);
            var x2 = XmlClassify.ObjectFromXElement<classWithAdvancedTypes>(xml);

            Assert.IsTrue(x2.IntCollection != null);
            Assert.IsTrue(x2.IntCollection.SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(x2.IntList != null);
            Assert.IsTrue(x2.IntList.SequenceEqual(new[] { 5, 6, 7, 8 }));
            Assert.IsTrue(x2.IntStringDic != null);
            Assert.IsTrue(x2.IntStringDic.GetType() == typeof(Dictionary<int, string>));
            Assert.IsTrue(x2.IntStringDic.Keys.Order().SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(x2.IntStringDic.Values.Order().SequenceEqual(new[] { "four", "one", "three", "two" }));
            Assert.IsTrue(x2.SortedDic != null);
            Assert.IsTrue(x2.SortedDic.GetType() == typeof(SortedDictionary<int, string>));
            Assert.IsTrue(x2.SortedDic.Keys.Order().SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(x2.SortedDic.Values.Order().SequenceEqual(new[] { "four", "one", "three", "two" }));
        }

        private sealed class classRefTest1
        {
            public int Int1, Int2;
            public string String1, String2, String3;
            public Version Version1, Version2, Version3;
            public List<int> List1, List2, List3;
        }

        [Test]
        public void TestReferenceIdentity1()
        {
            var co = new classRefTest1();
            co.Int1 = co.Int2 = 5;
            co.String1 = co.String2 = new string('a', 5);
            co.String3 = new string('a', 5);
            co.Version1 = co.Version2 = new Version(4, 7);
            co.Version3 = new Version(4, 7);
            co.List1 = co.List2 = new List<int>();
            co.List3 = new List<int>();

            var xml = XmlClassify.ObjectToXElement(co);
            Assert.IsTrue(XNode.DeepEquals(XElement.Parse(@"<test>  <thingy stuff=""123"" /> </test>"), XElement.Parse(@"       <test  ><thingy    stuff  =  ""123""/>     </test>  ")));
            Assert.IsFalse(XNode.DeepEquals(XElement.Parse(@"<test>  <thingy stufff=""123"" /> </test>"), XElement.Parse(@"       <test  ><thingy    stuff  =  ""123""/>     </test>  ")));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Int1>5</Int1>
                  <Int2>5</Int2>
                  <String1 refid=""0"">aaaaa</String1>
                  <String2 ref=""0"" />
                  <String3>aaaaa</String3>
                  <Version1 refid=""1"">
                    <Major>4</Major>
                    <Minor>7</Minor>
                    <Build>-1</Build>
                    <Revision>-1</Revision>
                  </Version1>
                  <Version2 ref=""1"" />
                  <Version3>
                    <Major>4</Major>
                    <Minor>7</Minor>
                    <Build>-1</Build>
                    <Revision>-1</Revision>
                  </Version3>
                  <List1 refid=""2"" />
                  <List2 ref=""2"" />
                  <List3 />
                </item>")));

            var cn = XmlClassify.ObjectFromXElement<classRefTest1>(XElement.Parse(xml.ToString()));
            Assert.IsTrue(object.ReferenceEquals(cn.String1, cn.String2));
            Assert.IsFalse(object.ReferenceEquals(cn.String1, cn.String3));
            Assert.IsTrue(object.ReferenceEquals(cn.Version1, cn.Version2));
            Assert.IsFalse(object.ReferenceEquals(cn.Version1, cn.Version3));
            Assert.IsTrue(object.ReferenceEquals(cn.List1, cn.List2));
            Assert.IsFalse(object.ReferenceEquals(cn.List1, cn.List3));

            // Some sanity checks
            Assert.IsFalse(object.ReferenceEquals(co.String1, cn.String1));
            Assert.IsFalse(object.ReferenceEquals(co.Version1, cn.Version1));
            Assert.IsFalse(object.ReferenceEquals(co.List1, cn.List1));
            Assert.AreEqual(co.String1, cn.String2);
            Assert.AreEqual(co.String1, cn.String3);
            Assert.AreEqual(co.Version1, cn.Version2);
            Assert.AreEqual(co.Version1, cn.Version3);
            Assert.AreEqual(co.List1, cn.List2);
            Assert.AreEqual(co.List1, cn.List3);
        }

        private sealed class classRefTest2
        {
            public List<int> ListInt = new List<int>();
            public List<string> ListString = new List<string>();
            public List<Version> ListVersion = new List<Version>();
            public List<List<int>> ListList = new List<List<int>>();
        }

        [Test]
        public void TestReferenceIdentity2()
        {
            var co = new classRefTest2();
            co.ListInt.Add(5); co.ListInt.Add(5);
            co.ListString.Add(new string('a', 5)); co.ListString.Add(new string('a', 5)); co.ListString.Add(co.ListString[0]);
            co.ListVersion.Add(new Version(4, 7)); co.ListVersion.Add(new Version(4, 7)); co.ListVersion.Add(co.ListVersion[0]);
            co.ListList.Add(new List<int>()); co.ListList.Add(new List<int>()); co.ListList.Add(co.ListList[0]);

            var xml = XmlClassify.ObjectToXElement(co);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <ListInt>
                    <item>5</item>
                    <item>5</item>
                  </ListInt>
                  <ListString>
                    <item refid=""0"">aaaaa</item>
                    <item>aaaaa</item>
                    <item ref=""0"" />
                  </ListString>
                  <ListVersion>
                    <item refid=""1"">
                      <Major>4</Major>
                      <Minor>7</Minor>
                      <Build>-1</Build>
                      <Revision>-1</Revision>
                    </item>
                    <item>
                      <Major>4</Major>
                      <Minor>7</Minor>
                      <Build>-1</Build>
                      <Revision>-1</Revision>
                    </item>
                    <item ref=""1"" />
                  </ListVersion>
                  <ListList>
                    <item refid=""2"" />
                    <item />
                    <item ref=""2"" />
                  </ListList>
                </item>")));

            var cn = XmlClassify.ObjectFromXElement<classRefTest2>(XElement.Parse(xml.ToString()));
            Assert.IsTrue(object.ReferenceEquals(cn.ListString[0], cn.ListString[2]));
            Assert.IsFalse(object.ReferenceEquals(cn.ListString[0], cn.ListString[1]));
            Assert.IsTrue(object.ReferenceEquals(cn.ListVersion[0], cn.ListVersion[2]));
            Assert.IsFalse(object.ReferenceEquals(cn.ListVersion[0], cn.ListVersion[1]));
            Assert.IsTrue(object.ReferenceEquals(cn.ListList[0], cn.ListList[2]));
            Assert.IsFalse(object.ReferenceEquals(cn.ListList[0], cn.ListList[1]));

            // Some sanity checks
            Assert.IsFalse(object.ReferenceEquals(co.ListString[0], cn.ListString[0]));
            Assert.IsFalse(object.ReferenceEquals(co.ListVersion[0], cn.ListVersion[0]));
            Assert.IsFalse(object.ReferenceEquals(co.ListList[0], cn.ListList[0]));
            Assert.AreEqual(co.ListString[0], cn.ListString[2]);
            Assert.AreEqual(co.ListVersion[0], cn.ListVersion[2]);
        }

#pragma warning disable 0649 // Field is never assigned to, and will always have its default value null
        private class classWithVersion
        {
            public Version Version;
            public Version Version2;
            public Version VersionNull;
        }
#pragma warning restore 0649 // Field is never assigned to, and will always have its default value null

        private class substituteVersion
        {
            public string Value;

            public class Options : XmlClassifyTypeOptions, IXmlClassifySubstitute<Version, substituteVersion>
            {
                public substituteVersion ToSubstitute(Version instance)
                {
                    return instance == null ? null : new substituteVersion { Value = instance.ToString() };
                }

                public Version FromSubstitute(substituteVersion instance)
                {
                    return instance == null ? null : Version.Parse(instance.Value);
                }
            }
        }

        private class optionsVersionToString : XmlClassifyTypeOptions, IXmlClassifySubstitute<Version, string>
        {
            public string ToSubstitute(Version instance) { return instance == null ? null : instance.ToString(); }
            public Version FromSubstitute(string instance) { return instance == null ? null : Version.Parse(instance); }
        }

        [Test]
        public void TestTypeSubstitution()
        {
            var inst = new classWithVersion { Version = new Version(5, 3, 1) };
            inst.Version2 = inst.Version;
            var opts = new XmlClassifyOptions().AddTypeOptions(typeof(Version), new substituteVersion.Options());
            var xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Version refid=""0"">
                    <Value>5.3.1</Value>
                  </Version>
                  <Version2 ref=""0"" />
                  <VersionNull null=""1"" />
                </item>")));

            assertSubst(inst, opts, xml);

            opts = new XmlClassifyOptions().AddTypeOptions(typeof(Version), new optionsVersionToString());
            xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Version refid=""0"">5.3.1</Version>
                  <Version2 ref=""0"" />
                  <VersionNull null=""1"" />
                </item>")));

            assertSubst(inst, opts, xml);
        }

        private static void assertSubst(classWithVersion inst, XmlClassifyOptions opts, XElement xml)
        {
            var inst2 = XmlClassify.ObjectFromXElement<classWithVersion>(xml, opts);
            Assert.IsTrue(object.ReferenceEquals(inst2.Version, inst2.Version2)); // reference identity preserved
            Assert.IsFalse(object.ReferenceEquals(inst.Version, inst2.Version)); // sanity check
            Assert.AreEqual(inst.Version2, inst2.Version);
            Assert.IsTrue(inst.Version2.Equals(inst2.Version)); // just in case
            Assert.AreEqual(5, inst2.Version.Major);
            Assert.AreEqual(3, inst2.Version.Minor);
            Assert.AreEqual(1, inst2.Version.Build);
        }

#warning TODO: Check that classifying a nullable type actually uses the substitution for the underlying type
    }
}
