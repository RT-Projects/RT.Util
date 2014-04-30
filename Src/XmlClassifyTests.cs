using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

#pragma warning disable 618 // XmlClassify is obsolete

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
            public int[] IntArray;
        }

        [Test]
        public void TestBlankClass()
        {
            XElement xel = XmlClassify.ObjectToXElement(new blankClass());
            XmlClassify.ObjectFromXElement<blankClass>(xel);
        }

        [Test]
        public void TestRootElementName()
        {
            XElement xel = XmlClassify.ObjectToXElement(new blankClass(), new XmlClassifyOptions { RootElementName = "BlankClass" });
            Assert.IsTrue(XNode.DeepEquals(xel, XElement.Parse(@"<BlankClass/>")));
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
                IntArray = new int[] { 9, 10, 11, 12 },
                IntStringDic = new SortedDictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" } },
                SortedDic = new SortedDictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" } }
            };

            var xml = XmlClassify.ObjectToXElement(x);
            var x2 = XmlClassify.ObjectFromXElement<classWithAdvancedTypes>(xml);

            Assert.IsTrue(x2.IntCollection != null);
            Assert.IsTrue(x2.IntCollection.SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(x2.IntList != null);
            Assert.IsTrue(x2.IntList.SequenceEqual(new[] { 5, 6, 7, 8 }));
            Assert.IsTrue(x2.IntArray != null);
            Assert.IsTrue(x2.IntArray.SequenceEqual(new[] { 9, 10, 11, 12 }));
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
                  <String1>aaaaa</String1>
                  <String2>aaaaa</String2>
                  <String3>aaaaa</String3>
                  <Version1 refid=""0"">
                    <Major>4</Major>
                    <Minor>7</Minor>
                    <Build>-1</Build>
                    <Revision>-1</Revision>
                  </Version1>
                  <Version2 ref=""0"" />
                  <Version3>
                    <Major>4</Major>
                    <Minor>7</Minor>
                    <Build>-1</Build>
                    <Revision>-1</Revision>
                  </Version3>
                  <List1 refid=""1"" />
                  <List2 ref=""1"" />
                  <List3 />
                </item>")));

            var cn = XmlClassify.ObjectFromXElement<classRefTest1>(XElement.Parse(xml.ToString()));
            Assert.IsTrue(object.ReferenceEquals(cn.Version1, cn.Version2));
            Assert.IsFalse(object.ReferenceEquals(cn.Version1, cn.Version3));
            Assert.IsTrue(object.ReferenceEquals(cn.List1, cn.List2));
            Assert.IsFalse(object.ReferenceEquals(cn.List1, cn.List3));

            // Some sanity checks
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
            co.ListInt.Add(5);
            co.ListInt.Add(5);
            co.ListString.Add(new string('a', 5));
            co.ListString.Add(new string('a', 5));
            co.ListString.Add(co.ListString[0]);
            co.ListVersion.Add(new Version(4, 7));
            co.ListVersion.Add(new Version(4, 7));
            co.ListVersion.Add(co.ListVersion[0]);
            co.ListList.Add(new List<int>());
            co.ListList.Add(new List<int>());
            co.ListList.Add(co.ListList[0]);

            var xml = XmlClassify.ObjectToXElement(co);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <ListInt>
                    <item>5</item>
                    <item>5</item>
                  </ListInt>
                  <ListString>
                    <item>aaaaa</item>
                    <item>aaaaa</item>
                    <item>aaaaa</item>
                  </ListString>
                  <ListVersion>
                    <item refid=""0"">
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
                    <item ref=""0"" />
                  </ListVersion>
                  <ListList>
                    <item refid=""1"" />
                    <item />
                    <item ref=""1"" />
                  </ListList>
                </item>")));

            var cn = XmlClassify.ObjectFromXElement<classRefTest2>(XElement.Parse(xml.ToString()));
            Assert.IsTrue(object.ReferenceEquals(cn.ListVersion[0], cn.ListVersion[2]));
            Assert.IsFalse(object.ReferenceEquals(cn.ListVersion[0], cn.ListVersion[1]));
            Assert.IsTrue(object.ReferenceEquals(cn.ListList[0], cn.ListList[2]));
            Assert.IsFalse(object.ReferenceEquals(cn.ListList[0], cn.ListList[1]));

            // Some sanity checks
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

        private class substituteVersion
        {
            public string Value;

            public class Options : XmlClassifyTypeOptions, IXmlClassifySubstitute<Version, substituteVersion>
            {
                public substituteVersion ToSubstitute(Version version)
                {
                    return version.NullOr(v => new substituteVersion { Value = v.ToString() });
                }

                public Version FromSubstitute(substituteVersion subst)
                {
                    return subst.NullOr(s => Version.Parse(s.Value));
                }
            }
        }

        private class classWithGuid
        {
            public Guid Guid;
            public Guid? GuidNullableNull;
            public Guid? GuidNullableNotNull;
        }

        private class substituteGuid
        {
            public string Value;

            public class Options : XmlClassifyTypeOptions, IXmlClassifySubstitute<Guid, substituteGuid>
            {
                public substituteGuid ToSubstitute(Guid guid)
                {
                    return new substituteGuid { Value = guid.ToString() };
                }

                public Guid FromSubstitute(substituteGuid subst)
                {
                    return Guid.Parse(subst.Value);
                }
            }
        }

        private struct stringWrapper
        {
            public string Value;

            public class Options : XmlClassifyTypeOptions, IXmlClassifySubstitute<stringWrapper, string>
            {
                public stringWrapper FromSubstitute(string str)
                {
                    return new stringWrapper { Value = str };
                }

                public string ToSubstitute(stringWrapper subst)
                {
                    return subst.Value;
                }
            }
        }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value null

        private class optionsVersionToString : XmlClassifyTypeOptions, IXmlClassifySubstitute<Version, string>
        {
            public string ToSubstitute(Version version) { return version.NullOr(v => v.ToString()); }
            public Version FromSubstitute(string str) { return str.NullOr(s => Version.Parse(s)); }
        }
        private class optionsGuidToString : XmlClassifyTypeOptions, IXmlClassifySubstitute<Guid, string>
        {
            public string ToSubstitute(Guid guid) { return guid.ToString(); }
            public Guid FromSubstitute(string str) { return Guid.Parse(str); }
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

            assertSubstVersion(inst, opts, xml);

            opts = new XmlClassifyOptions().AddTypeOptions(typeof(Version), new optionsVersionToString());
            xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Version refid=""0"">5.3.1</Version>
                  <Version2 ref=""0"" />
                  <VersionNull null=""1"" />
                </item>")));

            assertSubstVersion(inst, opts, xml);
        }

        private static void assertSubstVersion(classWithVersion inst, XmlClassifyOptions opts, XElement xml)
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

        private static Guid _testGuid = Guid.Parse("d3909698-14cb-4af2-886b-739c6dc567eb");

        [Test]
        public void TestTypeSubstitutionNullable()
        {
            var inst = new classWithGuid
            {
                Guid = _testGuid,
                GuidNullableNotNull = _testGuid,
                GuidNullableNull = null
            };
            var opts = new XmlClassifyOptions().AddTypeOptions(typeof(Guid), new substituteGuid.Options());
            var xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Guid>
                    <Value>d3909698-14cb-4af2-886b-739c6dc567eb</Value>
                  </Guid>
                  <GuidNullableNull null=""1"" />
                  <GuidNullableNotNull>
                    <Value>d3909698-14cb-4af2-886b-739c6dc567eb</Value>
                  </GuidNullableNotNull>
                </item>")));
            assertSubstGuid(opts, xml);

            opts = new XmlClassifyOptions().AddTypeOptions(typeof(Guid), new optionsGuidToString());
            xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Guid>d3909698-14cb-4af2-886b-739c6dc567eb</Guid>
                  <GuidNullableNull null=""1"" />
                  <GuidNullableNotNull>d3909698-14cb-4af2-886b-739c6dc567eb</GuidNullableNotNull>
                </item>")));
            assertSubstGuid(opts, xml);
        }

        private static void assertSubstGuid(XmlClassifyOptions opts, XElement xml)
        {
            var inst2 = XmlClassify.ObjectFromXElement<classWithGuid>(xml, opts);
            Assert.IsTrue(inst2.Guid.Equals(_testGuid));
            Assert.IsTrue(inst2.GuidNullableNotNull.Value.Equals(_testGuid));
            Assert.IsTrue(inst2.GuidNullableNull == null);
        }

        [Test]
        public void TestTypeSubstitutionStruct()
        {
            var inst = new stringWrapper { Value = "val" };
            var opts = new XmlClassifyOptions().AddTypeOptions(typeof(stringWrapper), new stringWrapper.Options());
            var xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item>val</item>")));
            var inst2 = XmlClassify.ObjectFromXElement<stringWrapper>(xml, opts);
            Assert.AreEqual("val", inst2.Value);

            inst = new stringWrapper { Value = null };
            xml = XmlClassify.ObjectToXElement(inst, opts);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item null='1'/>")));
            inst2 = XmlClassify.ObjectFromXElement<stringWrapper>(xml, opts);
            Assert.IsNull(inst2.Value);
        }

        private sealed class settingsClass
        {
            public setting S1 = new setting();
            public setting S2 = new setting { Something = "FOO", Other = "BAR" };
            public setting S3 = null;
            public List<setting> L = new List<setting> { new setting { Other = "1", Something = "1" }, new setting { Other = "2" } };
            public bunch B = new bunch { C1 = new subClass { Derived = "TEST" } };
        }

        private sealed class setting
        {
            public string Something = "foo";
            public string Other = "bar";
        }

        private class bunch
        {
            public baseClass C1 = new baseClass { Base = "BASE" };
            public baseClass C2 = new baseClass { Base = "BASE" };
            public baseClass C3 = new subClass { Base = "BASE" };
            public baseClass C4 = new subClass { Base = "BASE", Derived = "DERIVED" };
            public baseClass C5 = new subClass();
            public baseClass C6 = null;
        }

        private class baseClass
        {
            public string Base = "base";
        }

        private class subClass : baseClass
        {
            public string Derived = "derived";
        }

        [Test]
        public void TestNestedDefaultsUpgradeability()
        {
            var xml = XElement.Parse(@"
                <item>
                    <S1>
                        <Something>foo1</Something>
                        <Other>bar1</Other>
                    </S1>
                    <S2>
                        <Something>FOO1</Something>
                        <Other>BAR1</Other>
                    </S2>
                </item>
            ");
            var settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual("foo1", settings.S1.Something);
            Assert.AreEqual("bar1", settings.S1.Other);
            Assert.AreEqual("FOO1", settings.S2.Something);
            Assert.AreEqual("BAR1", settings.S2.Other);

            xml = XElement.Parse(@"
                <item>
                    <S1>
                        <Something>foo1</Something>
                    </S1>
                    <S2>
                        <Something>FOO1</Something>
                    </S2>
                    <S3><Something>blah</Something></S3>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual("foo1", settings.S1.Something);
            Assert.AreEqual("bar", settings.S1.Other);
            Assert.AreEqual("FOO1", settings.S2.Something);
            Assert.AreEqual("BAR", settings.S2.Other);
            Assert.AreEqual("blah", settings.S3.Something);

            xml = XElement.Parse(@"
                <item>
                    <S1>
                    </S1>
                    <S2>
                    </S2>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual("foo", settings.S1.Something);
            Assert.AreEqual("bar", settings.S1.Other);
            Assert.AreEqual("FOO", settings.S2.Something);
            Assert.AreEqual("BAR", settings.S2.Other);

            xml = XElement.Parse(@"
                <item>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual("foo", settings.S1.Something);
            Assert.AreEqual("bar", settings.S1.Other);
            Assert.AreEqual("FOO", settings.S2.Something);
            Assert.AreEqual("BAR", settings.S2.Other);

            // List
            xml = XElement.Parse(@"
                <item>
                    <L>
                        <item>
                            <Other>a</Other>
                        </item>
                        <item>
                            <Other>b</Other>
                        </item>
                    </L>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(2, settings.L.Count);
            Assert.AreEqual("a", settings.L[0].Other);
            Assert.AreEqual("b", settings.L[1].Other);
            Assert.AreEqual("foo", settings.L[0].Something);
            Assert.AreEqual("foo", settings.L[1].Something);

            xml = XElement.Parse(@"
                <item>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(2, settings.L.Count);
            Assert.AreEqual("1", settings.L[0].Other);
            Assert.AreEqual("2", settings.L[1].Other);
            Assert.AreEqual("1", settings.L[0].Something);
            Assert.AreEqual("foo", settings.L[1].Something);
        }

        [Test]
        public void TestSubclassesAndUpgradeability()
        {
            var xml = XElement.Parse(@"
                <item>
                </item>
            ");
            var settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C1.GetType());
            Assert.AreEqual(typeof(baseClass), settings.B.C2.GetType());
            Assert.AreEqual(typeof(subClass), settings.B.C3.GetType());
            Assert.AreEqual(typeof(subClass), settings.B.C4.GetType());
            Assert.AreEqual("base", settings.B.C1.Base);
            Assert.AreEqual("TEST", (settings.B.C1 as subClass).Derived);
            Assert.AreEqual("BASE", settings.B.C2.Base);
            Assert.AreEqual("BASE", settings.B.C3.Base);
            Assert.AreEqual("derived", (settings.B.C3 as subClass).Derived);
            Assert.AreEqual("BASE", settings.B.C4.Base);
            Assert.AreEqual("DERIVED", (settings.B.C4 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C1 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C1>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C1.GetType());
            Assert.AreEqual("XML", settings.B.C1.Base);
            Assert.AreEqual("TEST", (settings.B.C1 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C2 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C2>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C2.GetType());
            Assert.AreEqual("XML", settings.B.C2.Base);
            Assert.AreEqual("derived", (settings.B.C2 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C3 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C3>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C3.GetType());
            Assert.AreEqual("BASE", settings.B.C3.Base);
            Assert.AreEqual("XML", (settings.B.C3 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C4 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C4>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C4.GetType());
            Assert.AreEqual("XML", settings.B.C4.Base);
            Assert.AreEqual("DERIVED", (settings.B.C4 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C5 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C5>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C5.GetType());
            Assert.AreEqual("base", settings.B.C5.Base);
            Assert.AreEqual("XML", (settings.B.C5 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C6 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C6>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C6.GetType());
            Assert.AreEqual("base", settings.B.C6.Base);
            Assert.AreEqual("XML", (settings.B.C6 as subClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C1 fulltype=""RT.Util.Xml.XmlClassifyTests+subClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C1>
                    </B>
                </item>
            ");
            settings = XmlClassify.ObjectFromXElement<settingsClass>(xml);
            Assert.AreEqual(typeof(subClass), settings.B.C1.GetType());
            Assert.AreEqual("base", settings.B.C1.Base); // not BASE because it's a different type, even if a subtype
            Assert.AreEqual("XML", (settings.B.C1 as subClass).Derived);
        }

        private sealed class TestPrePostProcess : IXmlClassifyProcess
        {
            [XmlIgnore]
            private Color _color;
            private string _colorAsString;

            private List<blankClass> _list;
            private blankClass _specified;

            public Color Color { get { return _color; } }

            /// <summary>For XmlClassify</summary>
            private TestPrePostProcess() { }

            public TestPrePostProcess(Color color)
            {
                _color = color;

                // This forces the use of reference equality
                _specified = new blankClass();
                _list = new List<blankClass> { _specified };
            }

            public void BeforeXmlClassify()
            {
                _colorAsString = "{0:X2}{1:X2}{2:X2}".Fmt(_color.R, _color.G, _color.B);
            }

            public void AfterXmlDeclassify()
            {
                _color = Color.FromArgb(
                    Convert.ToInt32(_colorAsString.Substring(0, 2), 16),
                    Convert.ToInt32(_colorAsString.Substring(2, 2), 16),
                    Convert.ToInt32(_colorAsString.Substring(4, 2), 16));

                // Test that the reference equality has been restored correctly *before* AfterXmlDeclassify() was called
                Assert.IsNotNull(_specified);
                Assert.IsNotNull(_list);
                Assert.AreEqual(1, _list.Count);
                Assert.IsTrue(object.ReferenceEquals(_list[0], _specified));
            }
        }

        [Test]
        public void TestPrePostprocess()
        {
            var instance = new TestPrePostProcess(Color.FromArgb(171, 205, 239));
            var xml = XmlClassify.ObjectToXElement(instance);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><colorAsString>ABCDEF</colorAsString><list><item refid='0'/></list><specified ref='0'/></item>")));

            var reconstructed = XmlClassify.ObjectFromXElement<TestPrePostProcess>(xml);
            Assert.AreEqual(reconstructed.Color, Color.FromArgb(171, 205, 239));
        }

        private sealed class SerializeThis
        {
            public string KeepThis;
            public string SetThisToXyz;
        }

        private sealed class serializeThisOptions : XmlClassifyTypeOptions, IXmlClassifyProcessXml
        {
            public void XmlPreprocess(XElement xml)
            {
                xml.Add(new XElement("SetThisToXyz", "Xyz"));
            }

            public void XmlPostprocess(XElement xml)
            {
                xml.Element("SetThisToXyz").Remove();
            }
        }

        [Test]
        public void TestXmlPrePostprocess()
        {
            var opt = new XmlClassifyOptions().AddTypeOptions(typeof(SerializeThis), new serializeThisOptions());

            var instance = new SerializeThis { KeepThis = "Keep", SetThisToXyz = "abc" };
            var xml = XmlClassify.ObjectToXElement(instance, opt);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><KeepThis>Keep</KeepThis></item>")));

            var reconstructed = XmlClassify.ObjectFromXElement<SerializeThis>(xml, opt);
            Assert.IsNotNull(reconstructed);
            Assert.AreEqual(reconstructed.KeepThis, "Keep");
            Assert.AreEqual(reconstructed.SetThisToXyz, "Xyz");
        }

        private sealed class explicitlyImplementedList : IList<string>
        {
            private List<string> _inner = new List<string>();

            int IList<string>.IndexOf(string item) { return _inner.IndexOf(item); }
            void IList<string>.Insert(int index, string item) { _inner.Insert(index, item); }
            void IList<string>.RemoveAt(int index) { _inner.RemoveAt(index); }
            string IList<string>.this[int index] { get { return _inner[index]; } set { _inner[index] = value; } }
            void ICollection<string>.Add(string item) { _inner.Add(item); }
            void ICollection<string>.Clear() { _inner.Clear(); }
            bool ICollection<string>.Contains(string item) { return _inner.Contains(item); }
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) { _inner.CopyTo(array, arrayIndex); }
            int ICollection<string>.Count { get { return _inner.Count; } }
            bool ICollection<string>.IsReadOnly { get { return false; } }
            bool ICollection<string>.Remove(string item) { return _inner.Remove(item); }
            IEnumerator<string> IEnumerable<string>.GetEnumerator() { return _inner.GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _inner.GetEnumerator(); }
        }

        private sealed class classWithExplicitlyImplementedList
        {
            public IList<string> List1 = new explicitlyImplementedList();
            public explicitlyImplementedList List2 = new explicitlyImplementedList();
        }

        [Test]
        public void TestClassWithExplicitlyImplementedList()
        {
            IList<string> weird1 = new explicitlyImplementedList();
            weird1.Add("X");
            var weird2 = new explicitlyImplementedList();
            ((IList<string>) weird2).Add("Y");

            var xml = XmlClassify.ObjectToXElement(new classWithExplicitlyImplementedList { List1 = weird1, List2 = weird2 });
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><List1><item>X</item></List1><List2><item>Y</item></List2></item>")));

            var reconstructed = XmlClassify.ObjectFromXElement<classWithExplicitlyImplementedList>(xml);
            Assert.IsNotNull(reconstructed);
            Assert.IsNotNull(reconstructed.List1);
            Assert.AreEqual(1, reconstructed.List1.Count);
            Assert.AreEqual("X", reconstructed.List1[0]);
            Assert.IsNotNull(reconstructed.List2);
            Assert.AreEqual(1, ((IList<string>) reconstructed.List2).Count);
            Assert.AreEqual("Y", ((IList<string>) reconstructed.List2)[0]);
        }

        private sealed class explicitlyImplementedDictionary : IDictionary<string, string>
        {
            private Dictionary<string, string> _inner = new Dictionary<string, string>();

            void IDictionary<string, string>.Add(string key, string value) { _inner.Add(key, value); }
            bool IDictionary<string, string>.ContainsKey(string key) { return _inner.ContainsKey(key); }
            ICollection<string> IDictionary<string, string>.Keys { get { return _inner.Keys; } }
            bool IDictionary<string, string>.Remove(string key) { return _inner.Remove(key); }
            bool IDictionary<string, string>.TryGetValue(string key, out string value) { return _inner.TryGetValue(key, out value); }
            ICollection<string> IDictionary<string, string>.Values { get { return _inner.Values; } }
            string IDictionary<string, string>.this[string key] { get { return _inner[key]; } set { _inner[key] = value; } }
            void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) { ((ICollection<KeyValuePair<string, string>>) _inner).Add(item); }
            void ICollection<KeyValuePair<string, string>>.Clear() { ((ICollection<KeyValuePair<string, string>>) _inner).Clear(); }
            bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) { return ((ICollection<KeyValuePair<string, string>>) _inner).Contains(item); }
            void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) { ((ICollection<KeyValuePair<string, string>>) _inner).CopyTo(array, arrayIndex); }
            int ICollection<KeyValuePair<string, string>>.Count { get { return ((ICollection<KeyValuePair<string, string>>) _inner).Count; } }
            bool ICollection<KeyValuePair<string, string>>.IsReadOnly { get { return false; } }
            bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) { return ((ICollection<KeyValuePair<string, string>>) _inner).Remove(item); }
            IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() { return _inner.GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _inner.GetEnumerator(); }
        }

        private sealed class classWithExplicitlyImplementedDictionary
        {
            public IDictionary<string, string> Dictionary1 = new explicitlyImplementedDictionary();
            public explicitlyImplementedDictionary Dictionary2 = new explicitlyImplementedDictionary();
        }

        [Test]
        public void TestClassWithExplicitlyImplementedDictionary()
        {
            IDictionary<string, string> weird1 = new explicitlyImplementedDictionary();
            weird1.Add("X", "Y");
            var weird2 = new explicitlyImplementedDictionary();
            ((IDictionary<string, string>) weird2).Add("Y", "Z");

            var xml = XmlClassify.ObjectToXElement(new classWithExplicitlyImplementedDictionary { Dictionary1 = weird1, Dictionary2 = weird2 });
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><Dictionary1><item key='X'>Y</item></Dictionary1><Dictionary2><item key='Y'>Z</item></Dictionary2></item>")));

            var reconstructed = XmlClassify.ObjectFromXElement<classWithExplicitlyImplementedDictionary>(xml);
            Assert.IsNotNull(reconstructed);
            Assert.IsNotNull(reconstructed.Dictionary1);
            Assert.AreEqual(1, reconstructed.Dictionary1.Count);
            Assert.AreEqual("Y", reconstructed.Dictionary1["X"]);
            Assert.IsNotNull(reconstructed.Dictionary2);
            Assert.AreEqual(1, ((IDictionary<string, string>) reconstructed.Dictionary2).Count);
            Assert.AreEqual("Z", ((IDictionary<string, string>) reconstructed.Dictionary2)["Y"]);
        }

        sealed class xmlIntoObjectReferenceEqualityTester
        {
            public List<blankClass> List;
            public blankClass Specified;
        }

        [Test]
        public void TestXmlIntoObjectReferenceEquality()
        {
            var inner = new blankClass();
            var tester = new xmlIntoObjectReferenceEqualityTester
            {
                List = new List<blankClass> { inner },
                Specified = inner
            };
            var classified = XmlClassify.ObjectToXElement(tester);

            Assert.IsNotNull(classified.Element("Specified"));
            Assert.IsNotNull(classified.Element("Specified").Attribute("ref"));
            Assert.IsNotNull(classified.Element("List"));
            Assert.IsNotNull(classified.Element("List").Element("item"));
            Assert.IsNotNull(classified.Element("List").Element("item").Attribute("refid"));
            Assert.IsTrue(classified.Element("Specified").Attribute("ref").Value == classified.Element("List").Element("item").Attribute("refid").Value);
            var tmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmpFile, classified.ToString());

                var newTester = new xmlIntoObjectReferenceEqualityTester();
                XmlClassify.ReadXmlFileIntoObject(tmpFile, newTester);

                Assert.IsNotNull(newTester.List);
                Assert.AreEqual(1, newTester.List.Count);
                Assert.IsTrue(object.ReferenceEquals(newTester.List[0], newTester.Specified));
            }
            finally
            {
                try { File.Delete(tmpFile); }
                catch { }
            }
        }

        sealed class referenceEqualityInArraysTester
        {
            public blankClass[] Array;
            public blankClass Specified;
        }

        [Test]
        public void TestReferenceEqualityInArrays()
        {
            var inner = new blankClass();
            var tester = new referenceEqualityInArraysTester
            {
                Array = new blankClass[] { inner, inner },
                Specified = inner
            };
            var classified = XmlClassify.ObjectToXElement(tester);

            Assert.IsNotNull(classified.Element("Specified"));
            Assert.IsNotNull(classified.Element("Specified").Attribute("ref"));
            Assert.IsNotNull(classified.Element("Array"));
            Assert.IsNotNull(classified.Element("Array").Element("item"));
            Assert.IsNotNull(classified.Element("Array").Element("item").Attribute("refid"));
            Assert.IsTrue(classified.Element("Specified").Attribute("ref").Value == classified.Element("Array").Element("item").Attribute("refid").Value);

            var newTester = XmlClassify.ObjectFromXElement<referenceEqualityInArraysTester>(classified);

            Assert.IsNotNull(newTester.Array);
            Assert.AreEqual(2, newTester.Array.Length);
            Assert.IsTrue(object.ReferenceEquals(newTester.Array[0], newTester.Specified));
            Assert.IsTrue(object.ReferenceEquals(newTester.Array[1], newTester.Specified));
        }
    }
}

#pragma warning restore 618
