using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.Serialization
{
    [TestFixture]
    public sealed class ClassifyTests
    {
        public XElement VerifyXml(XElement xml, Dictionary<string, bool> refs = null)
        {
            bool outer = refs == null;
            if (refs == null)
                refs = new Dictionary<string, bool>();
            if (xml.Attribute("null") != null)
            {
                if (xml.Attribute("ref") != null || xml.Attribute("refid") != null || xml.HasElements)
                    Assert.Fail("An element with the null attribute must not have a ref or refid attribute, or any sub-elements.");
            }
            else if (xml.Attribute("ref") != null && xml.Attribute("refid") != null)
                Assert.Fail("An element must not have both a ref and a refid attribute.");
            else if (xml.Attribute("refid") != null)
            {
                string refid = xml.Attribute("refid").Value;
                if (refid == "")
                    Assert.Fail("An empty string is not a valid refid.");
                if (refs.ContainsKey(refid))
                    Assert.Fail("refid \"{0}\" has already been specified on another element.".Fmt(refid));
                refs.Add(refid, false);
            }
            else if (xml.Attribute("ref") != null)
            {
                if (xml.HasElements)
                    Assert.Fail("An element with a ref attribute must not have any sub-elements.");
                string rf = xml.Attribute("ref").Value;
                if (!refs.ContainsKey(rf))
                    Assert.Fail("ref \"{0}\" is used before it is declared, in depth-first order.".Fmt(rf));
                refs[rf] = true;
            }
            foreach (var el in xml.Elements())
                VerifyXml(el, refs);
            if (outer)
                if (refs.Values.Any(referenced => !referenced))
                    Assert.Fail("The following refs were redundant as they were never used: {0}. This is not invalid as such, but Classify should not generate them for no reason.".Fmt(refs.Where(r => !r.Value).Select(r => r.Key).JoinString(", ")));
            return xml;
        }

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
            public object boxedInt = int.MinValue;
            public object boxedLong = long.MinValue;

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
                Assert.AreEqual(boxedInt.NullOr(b => (int) b), actual.boxedInt.NullOr(b => (int) b));
                Assert.AreEqual(boxedLong.NullOr(b => (long) b), actual.boxedLong.NullOr(b => (long) b));
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
            XElement xel = VerifyXml(ClassifyXml.Serialize(new blankClass()));
            ClassifyXml.Deserialize<blankClass>(xel);
        }

        [Test]
        public void TestRootElementName()
        {
            XElement xel = VerifyXml(ClassifyXml.Serialize(new blankClass(), format: ClassifyXmlFormat.Create(rootTagName: "BlankClass")));
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
                boxedInt = int.MinValue,
                boxedLong = long.MinValue,
            };
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<basicClass>(xel);

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
            Assert.AreEqual((int) clsEx.boxedInt, (int) clsAc.boxedInt);
            Assert.AreEqual((long) clsEx.boxedLong, (long) clsAc.boxedLong);
        }

        [Test]
        public void TestStringNull()
        {
            var clsEx = new basicClass() { AString = null };
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<basicClass>(xel);

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
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<classWithList>(xel);

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
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<classWithDict>(xel);

            assertDict(clsEx.Dict, clsAc.Dict);
            assertDictList(clsEx.DictLists, clsAc.DictLists);
        }

        [Test]
        public void TestDictSubclass()
        {
            var clsEx = new classWithDict();
            clsEx.DictClasses.Add("test1", new basicClass());
            clsEx.DictClasses.Add("test2", new basicClass() { AnInt = 63827, key = 429745 });
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<classWithDict>(xel);

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
            var xel = VerifyXml(ClassifyXml.Serialize(clsEx));
            var clsAc = ClassifyXml.Deserialize<xmlClass>(xel);

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
            var xel = VerifyXml(ClassifyXml.Serialize(nestedEx));
            var nestedAc = ClassifyXml.Deserialize<nestedClass>(xel);

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
            var loaded = ClassifyXml.Deserialize<basicClass>(elem);

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
            var loaded1 = ClassifyXml.Deserialize<classWithPrivateCtor>(elem);
            Assert.AreEqual(9867, loaded1.Field);

            try
            {
                var loaded2 = ClassifyXml.Deserialize<classWithThrowingCtor>(elem);
                Assert.Fail("Expected exception");
            }
            catch (Exception) { }

            try
            {
                var loaded3 = ClassifyXml.Deserialize<classWithNoCtor>(elem);
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

            var xml = VerifyXml(ClassifyXml.Serialize(x));
            var x2 = ClassifyXml.Deserialize<classWithAdvancedTypes>(xml);

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

            var xml = VerifyXml(ClassifyXml.Serialize(co));
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

            var cn = ClassifyXml.Deserialize<classRefTest1>(XElement.Parse(xml.ToString()));
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

            var xml = VerifyXml(ClassifyXml.Serialize(co));
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

            var cn = ClassifyXml.Deserialize<classRefTest2>(XElement.Parse(xml.ToString()));
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

            public class Options : ClassifyTypeOptions, IClassifySubstitute<Version, substituteVersion>
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

        private class classWithGuidSubAttribute
        {
            [ClassifySubstitute(typeof(guidSubstituteConverter))]
            public Guid Guid;
            [ClassifySubstitute(typeof(guidSubstituteConverter))]
            public Guid? GuidNullableNull;
            [ClassifySubstitute(typeof(guidSubstituteConverter))]
            public Guid? GuidNullableNotNull;
        }

        // Test that Classify can handle converters that convert multiple types.
        // Note one interface is implemented implicitly, one explicitly. Classify must use the correct method for each type.
        private class guidSubstituteConverter : IClassifySubstitute<Guid, string>, IClassifySubstitute<Guid?, string>
        {
            public string ToSubstitute(Guid instance)
            {
                return instance.ToString();
            }

            public Guid FromSubstitute(string instance)
            {
                return Guid.Parse(instance);
            }

            string IClassifySubstitute<Guid?, string>.ToSubstitute(Guid? instance)
            {
                return instance.NullOr(i => i.ToString());
            }

            Guid? IClassifySubstitute<Guid?, string>.FromSubstitute(string instance)
            {
                return instance.NullOr(i => Guid.Parse(i));
            }
        }

        private class substituteGuid
        {
            public string Value;

            public class Options : ClassifyTypeOptions, IClassifySubstitute<Guid, substituteGuid>
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

            public class Options : ClassifyTypeOptions, IClassifySubstitute<stringWrapper, string>
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

        private class optionsVersionToString : ClassifyTypeOptions, IClassifySubstitute<Version, string>
        {
            public string ToSubstitute(Version version) { return version.NullOr(v => v.ToString()); }
            public Version FromSubstitute(string str) { return str.NullOr(s => Version.Parse(s)); }
        }
        private class optionsGuidToString : ClassifyTypeOptions, IClassifySubstitute<Guid, string>
        {
            public string ToSubstitute(Guid guid) { return guid.ToString(); }
            public Guid FromSubstitute(string str) { return Guid.Parse(str); }
        }

        [Test]
        public void TestTypeSubstitutionAttribute()
        {
            var inst = new classWithGuidSubAttribute();
            var guid = _testGuid;
            inst.Guid = guid;
            inst.GuidNullableNotNull = guid;
            inst.GuidNullableNull = null;

            var xml = ClassifyXml.Serialize(inst);
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Guid>" + guid.ToString() + @"</Guid>
                  <GuidNullableNull null=""1"" />
                  <GuidNullableNotNull>" + guid.ToString() + @"</GuidNullableNotNull>
                </item>")));

            var parsed = ClassifyXml.Deserialize<classWithGuidSubAttribute>(xml);
            Assert.AreEqual(parsed.Guid.ToString(), guid.ToString());
            Assert.AreEqual(parsed.GuidNullableNotNull.GetValueOrDefault().ToString(), guid.ToString());
            Assert.IsNull(parsed.GuidNullableNull);
        }

        [Test]
        public void TestTypeSubstitution()
        {
            var inst = new classWithVersion { Version = new Version(5, 3, 1) };
            inst.Version2 = inst.Version;
            var opts = new ClassifyOptions().AddTypeOptions(typeof(Version), new substituteVersion.Options());
            var xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Version refid=""0"">
                    <Value>5.3.1</Value>
                  </Version>
                  <Version2 ref=""0"" />
                  <VersionNull null=""1"" />
                </item>")));

            assertSubstVersion(inst, opts, xml);

            opts = new ClassifyOptions().AddTypeOptions(typeof(Version), new optionsVersionToString());
            xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Version refid=""0"">5.3.1</Version>
                  <Version2 ref=""0"" />
                  <VersionNull null=""1"" />
                </item>")));

            assertSubstVersion(inst, opts, xml);
        }

        private static void assertSubstVersion(classWithVersion inst, ClassifyOptions opts, XElement xml)
        {
            var inst2 = ClassifyXml.Deserialize<classWithVersion>(xml, opts);
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
            var opts = new ClassifyOptions().AddTypeOptions(typeof(Guid), new substituteGuid.Options());
            var xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
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

            opts = new ClassifyOptions().AddTypeOptions(typeof(Guid), new optionsGuidToString());
            xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"
                <item>
                  <Guid>d3909698-14cb-4af2-886b-739c6dc567eb</Guid>
                  <GuidNullableNull null=""1"" />
                  <GuidNullableNotNull>d3909698-14cb-4af2-886b-739c6dc567eb</GuidNullableNotNull>
                </item>")));
            assertSubstGuid(opts, xml);
        }

        private static void assertSubstGuid(ClassifyOptions opts, XElement xml)
        {
            var inst2 = ClassifyXml.Deserialize<classWithGuid>(xml, opts);
            Assert.IsTrue(inst2.Guid.Equals(_testGuid));
            Assert.IsTrue(inst2.GuidNullableNotNull.Value.Equals(_testGuid));
            Assert.IsTrue(inst2.GuidNullableNull == null);
        }

        [Test]
        public void TestTypeSubstitutionStruct()
        {
            var inst = new stringWrapper { Value = "val" };
            var opts = new ClassifyOptions().AddTypeOptions(typeof(stringWrapper), new stringWrapper.Options());
            var xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item>val</item>")));
            var inst2 = ClassifyXml.Deserialize<stringWrapper>(xml, opts);
            Assert.AreEqual("val", inst2.Value);

            inst = new stringWrapper { Value = null };
            xml = VerifyXml(ClassifyXml.Serialize(inst, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item null='1'/>")));
            inst2 = ClassifyXml.Deserialize<stringWrapper>(xml, opts);
            Assert.IsNull(inst2.Value);
        }

        private sealed class settingsClass
        {
            public setting S1 = new setting();
            public setting S2 = new setting { Something = "FOO", Other = "BAR" };
            public setting S3 = null;
            public List<setting> L = new List<setting> { new setting { Other = "1", Something = "1" }, new setting { Other = "2" } };
            public x X = new x { S0 = new setting { Other = "1", Something = "1" }, S1 = new setting { Other = "2" } }; // observe that it's just like the list L
            public bunch B = new bunch { C1 = new bunchDerivedClass { Derived = "TEST" } };
        }

        private sealed class setting
        {
            public string Something = "foo";
            public string Other = "bar";
        }

        private sealed class x
        {
            public setting S0;
            public setting S1;
        }

        private class bunch
        {
            public bunchBaseClass C1 = new bunchBaseClass { Base = "BASE" };
            public bunchBaseClass C2 = new bunchBaseClass { Base = "BASE" };
            public bunchBaseClass C3 = new bunchDerivedClass { Base = "BASE" };
            public bunchBaseClass C4 = new bunchDerivedClass { Base = "BASE", Derived = "DERIVED" };
            public bunchBaseClass C5 = new bunchDerivedClass();
            public bunchBaseClass C6 = null;
        }

        private class bunchBaseClass
        {
            public string Base = "base";
        }

        private class bunchDerivedClass : bunchBaseClass
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
            var settings = ClassifyXml.Deserialize<settingsClass>(xml);
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
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
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
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual("foo", settings.S1.Something);
            Assert.AreEqual("bar", settings.S1.Other);
            Assert.AreEqual("FOO", settings.S2.Something);
            Assert.AreEqual("BAR", settings.S2.Other);

            xml = XElement.Parse(@"
                <item>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual("foo", settings.S1.Something);
            Assert.AreEqual("bar", settings.S1.Other);
            Assert.AreEqual("FOO", settings.S2.Something);
            Assert.AreEqual("BAR", settings.S2.Other);

            // Nested initializer
            xml = XElement.Parse(@"
                <item>
                    <X>
                        <S0>
                            <Other>a</Other>
                        </S0>
                        <S1>
                            <Other>b</Other>
                        </S1>
                    </X>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual("a", settings.X.S0.Other);
            Assert.AreEqual("b", settings.X.S1.Other);
            Assert.AreEqual("1", settings.X.S0.Something); // note: different to how it's done for lists; see next test.
            Assert.AreEqual("foo", settings.X.S1.Something);

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
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(2, settings.L.Count);
            Assert.AreEqual("a", settings.L[0].Other);
            Assert.AreEqual("b", settings.L[1].Other);
            Assert.AreEqual("foo", settings.L[0].Something); // note: different to how it's done for plain classes; see previous test
            Assert.AreEqual("foo", settings.L[1].Something);

            xml = XElement.Parse(@"
                <item>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
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
            var settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C1.GetType());
            Assert.AreEqual(typeof(bunchBaseClass), settings.B.C2.GetType());
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C3.GetType());
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C4.GetType());
            Assert.AreEqual("base", settings.B.C1.Base);
            Assert.AreEqual("TEST", (settings.B.C1 as bunchDerivedClass).Derived);
            Assert.AreEqual("BASE", settings.B.C2.Base);
            Assert.AreEqual("BASE", settings.B.C3.Base);
            Assert.AreEqual("derived", (settings.B.C3 as bunchDerivedClass).Derived);
            Assert.AreEqual("BASE", settings.B.C4.Base);
            Assert.AreEqual("DERIVED", (settings.B.C4 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C1 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C1>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C1.GetType());
            Assert.AreEqual("XML", settings.B.C1.Base);
            Assert.AreEqual("TEST", (settings.B.C1 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C2 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C2>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C2.GetType());
            Assert.AreEqual("XML", settings.B.C2.Base);
            Assert.AreEqual("derived", (settings.B.C2 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C3 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C3>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C3.GetType());
            Assert.AreEqual("BASE", settings.B.C3.Base);
            Assert.AreEqual("XML", (settings.B.C3 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C4 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Base>XML</Base>
                        </C4>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C4.GetType());
            Assert.AreEqual("XML", settings.B.C4.Base);
            Assert.AreEqual("DERIVED", (settings.B.C4 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C5 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C5>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C5.GetType());
            Assert.AreEqual("base", settings.B.C5.Base);
            Assert.AreEqual("XML", (settings.B.C5 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C6 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C6>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C6.GetType());
            Assert.AreEqual("base", settings.B.C6.Base);
            Assert.AreEqual("XML", (settings.B.C6 as bunchDerivedClass).Derived);

            xml = XElement.Parse(@"
                <item>
                    <B>
                        <C1 fulltype=""RT.Util.Serialization.ClassifyTests+bunchDerivedClass, RT.UtilTests, Version=1.0.9999.9999, Culture=neutral, PublicKeyToken=null"">
                            <Derived>XML</Derived>
                        </C1>
                    </B>
                </item>
            ");
            settings = ClassifyXml.Deserialize<settingsClass>(xml);
            Assert.AreEqual(typeof(bunchDerivedClass), settings.B.C1.GetType());
            Assert.AreEqual("base", settings.B.C1.Base); // not BASE because it's a different type, even if a subtype
            Assert.AreEqual("XML", (settings.B.C1 as bunchDerivedClass).Derived);
        }

        private sealed class TestPrePostProcess : IClassifyObjectProcessor<XElement>
        {
            [ClassifyIgnore]
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

            public void BeforeSerialize()
            {
                _colorAsString = "{0:X2}{1:X2}{2:X2}".Fmt(_color.R, _color.G, _color.B);
            }

            public void AfterSerialize(XElement element)
            {
            }

            public void BeforeDeserialize(XElement element)
            {
            }

            public void AfterDeserialize(XElement element)
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
            var xml = VerifyXml(ClassifyXml.Serialize(instance));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><colorAsString>ABCDEF</colorAsString><list><item refid='0'/></list><specified ref='0'/></item>")));

            var reconstructed = ClassifyXml.Deserialize<TestPrePostProcess>(xml);
            Assert.AreEqual(reconstructed.Color, Color.FromArgb(171, 205, 239));
        }

        private sealed class SerializeThis
        {
            public string KeepThis;
            public string SetThisToXyz;
        }

        private sealed class serializeThisOptions : ClassifyTypeOptions, IClassifyTypeProcessor<XElement>
        {
            public void BeforeSerialize(object obj)
            {
            }

            public void AfterSerialize(object obj, XElement element)
            {
                element.Element("SetThisToXyz").Remove();
            }

            public void BeforeDeserialize(XElement element)
            {
                element.Add(new XElement("SetThisToXyz", "Xyz"));
            }

            public void AfterDeserialize(object obj, XElement element)
            {
            }
        }

        [Test]
        public void TestXmlPrePostprocess()
        {
            var opt = new ClassifyOptions().AddTypeOptions(typeof(SerializeThis), new serializeThisOptions());

            var instance = new SerializeThis { KeepThis = "Keep", SetThisToXyz = "abc" };
            var xml = VerifyXml(ClassifyXml.Serialize(instance, opt));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><KeepThis>Keep</KeepThis></item>")));

            var reconstructed = ClassifyXml.Deserialize<SerializeThis>(xml, opt);
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

            var xml = VerifyXml(ClassifyXml.Serialize(new classWithExplicitlyImplementedList { List1 = weird1, List2 = weird2 }));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><List1><item>X</item></List1><List2><item>Y</item></List2></item>")));

            var reconstructed = ClassifyXml.Deserialize<classWithExplicitlyImplementedList>(xml);
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

            var xml = VerifyXml(ClassifyXml.Serialize(new classWithExplicitlyImplementedDictionary { Dictionary1 = weird1, Dictionary2 = weird2 }));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><Dictionary1><item key='X'>Y</item></Dictionary1><Dictionary2><item key='Y'>Z</item></Dictionary2></item>")));

            var reconstructed = ClassifyXml.Deserialize<classWithExplicitlyImplementedDictionary>(xml);
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
            var classified = VerifyXml(ClassifyXml.Serialize(tester));

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
                ClassifyXml.DeserializeFileIntoObject(tmpFile, newTester);

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
            var classified = VerifyXml(ClassifyXml.Serialize(tester));

            Assert.IsNotNull(classified.Element("Specified"));
            Assert.IsNotNull(classified.Element("Specified").Attribute("ref"));
            Assert.IsNotNull(classified.Element("Array"));
            Assert.IsNotNull(classified.Element("Array").Element("item"));
            Assert.IsNotNull(classified.Element("Array").Element("item").Attribute("refid"));
            Assert.IsTrue(classified.Element("Specified").Attribute("ref").Value == classified.Element("Array").Element("item").Attribute("refid").Value);

            var newTester = ClassifyXml.Deserialize<referenceEqualityInArraysTester>(classified);

            Assert.IsNotNull(newTester.Array);
            Assert.AreEqual(2, newTester.Array.Length);
            Assert.IsTrue(object.ReferenceEquals(newTester.Array[0], newTester.Specified));
            Assert.IsTrue(object.ReferenceEquals(newTester.Array[1], newTester.Specified));
        }

        sealed class circularReferenceTester
        {
            public circularReferenceTester Tester;
        }

        [Test]
        public void TestCircularReference()
        {
            var tester = new circularReferenceTester();
            tester.Tester = tester;

            var classified = VerifyXml(ClassifyXml.Serialize(tester));
            Assert.IsTrue(XNode.DeepEquals(classified, XElement.Parse(@"<item refid=""0""><Tester ref=""0"" /></item>")));

            Assert.IsNotNull(classified.Element("Tester"));
            Assert.IsNotNull(classified.Attribute("refid"));
            Assert.IsNotNull(classified.Element("Tester").Attribute("ref"));
            Assert.IsTrue(classified.Element("Tester").Attribute("ref").Value == classified.Attribute("refid").Value);

            var newTester = ClassifyXml.Deserialize<circularReferenceTester>(classified);

            Assert.IsNotNull(newTester.Tester);
            Assert.IsTrue(object.ReferenceEquals(newTester, newTester.Tester));
        }

        sealed class circularReferenceArrayTester
        {
            public circularReferenceArrayTester[] Tester;
        }

        [Test]
        public void TestCircularReferenceArray()
        {
            var tester = new circularReferenceArrayTester[1];
            tester[0] = new circularReferenceArrayTester();
            tester[0].Tester = tester;

            var classified = VerifyXml(ClassifyXml.Serialize(tester));
            Assert.IsTrue(XNode.DeepEquals(classified, XElement.Parse(@"<item refid=""0""><item><Tester ref=""0"" /></item></item>")));

            Assert.IsNotNull(classified.Element("item"));
            Assert.IsNotNull(classified.Element("item").Element("Tester"));
            Assert.IsNotNull(classified.Element("item").Element("Tester").Attribute("ref"));
            Assert.IsNotNull(classified.Attribute("refid"));
            Assert.IsTrue(classified.Element("item").Element("Tester").Attribute("ref").Value == classified.Attribute("refid").Value);

            var newTester = ClassifyXml.Deserialize<circularReferenceArrayTester[]>(classified);

            Assert.IsNotNull(newTester);
            Assert.Greater(newTester.Length, 0);
            Assert.IsNotNull(newTester[0].Tester);
            Assert.IsTrue(object.ReferenceEquals(newTester, newTester[0].Tester));
        }

        sealed class tupleReferenceTester
        {
            public Tuple<object> Tester;
        }

        [Test]
        public void TestTupleReference()
        {
            var tester = new tupleReferenceTester();
            tester.Tester = Tuple.Create((object) tester);

            var xml = VerifyXml(ClassifyXml.Serialize(tester));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item refid=""0""><Tester><item1 ref=""0"" /></Tester></item>")));

            var newTester = ClassifyXml.Deserialize<tupleReferenceTester>(xml);
            Assert.IsTrue(newTester.Tester != null);
            Assert.IsTrue(object.ReferenceEquals(newTester, newTester.Tester.Item1));

            xml = VerifyXml(ClassifyXml.Serialize(tester.Tester));
            var fulltype = typeof(tupleReferenceTester).AssemblyQualifiedName;
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item refid=""0""><item1 fulltype=""{0}""><Tester ref=""0"" /></item1></item>".Fmt(fulltype))));

            var newTuple = ClassifyXml.Deserialize<Tuple<object>>(xml);
            Assert.IsTrue(newTuple.Item1 != null);
            Assert.IsTrue(object.ReferenceEquals(newTuple, ((tupleReferenceTester) newTuple.Item1).Tester));
        }

        sealed class listSubstitutionTester
        {
            public List<int> AList;
        }

        sealed class listSubstOpts : ClassifyTypeOptions, IClassifySubstitute<List<int>, string>
        {
            public string ToSubstitute(List<int> instance)
            {
                return instance.NullOr(i => i.JoinString("|"));
            }

            public List<int> FromSubstitute(string instance)
            {
                return instance.NullOr(i => i.Split("|").Select(p => int.Parse(p)).ToList());
            }
        }

        [Test]
        public void TestListSubstitution()
        {
            var tester = new listSubstitutionTester { AList = new List<int> { 7, 5, 3, 1 } };
            var opts = new ClassifyOptions().AddTypeOptions(typeof(List<int>), new listSubstOpts());

            var xml = VerifyXml(ClassifyXml.Serialize(tester, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item><AList>7|5|3|1</AList></item>")));

            var newTester = ClassifyXml.Deserialize<listSubstitutionTester>(xml, opts);
            Assert.IsTrue(newTester.AList.SequenceEqual(new[] { 7, 5, 3, 1 }));

            xml = VerifyXml(ClassifyXml.Serialize(tester.AList, opts));
            Assert.IsTrue(XNode.DeepEquals(xml, XElement.Parse(@"<item>7|5|3|1</item>")));

            var newList = ClassifyXml.Deserialize<List<int>>(xml, opts);
            Assert.IsTrue(newList.SequenceEqual(new[] { 7, 5, 3, 1 }));
        }

        class refSubstClass<T> where T : refSubstClass<T>, new()
        {
            public string Name; public T Ref1; public T Ref2;
            public override string ToString() { return Name; }
        }
        sealed class refSubstClass1 : refSubstClass<refSubstClass1> { public int Marker1 = 1; }
        sealed class refSubstClass2 : refSubstClass<refSubstClass2> { public int Marker2 = 2; }

        sealed class refSubstOpts : ClassifyTypeOptions, IClassifySubstitute<refSubstClass1, refSubstClass2>
        {
            public refSubstClass2 ToSubstitute(refSubstClass1 instance)
            {
                return subst(instance, new Dictionary<refSubstClass1, refSubstClass2>());
            }

            public refSubstClass1 FromSubstitute(refSubstClass2 instance)
            {
                return subst(instance, new Dictionary<refSubstClass2, refSubstClass1>());
            }

            private T2 subst<T1, T2>(T1 instance, Dictionary<T1, T2> dictionary)
                where T1 : refSubstClass<T1>, new()
                where T2 : refSubstClass<T2>, new()
            {
                if (dictionary.ContainsKey(instance))
                    return dictionary[instance];
                dictionary[instance] = new T2 { Name = instance.Name }; // must assign first!
                dictionary[instance].Ref1 = subst(instance.Ref1, dictionary);
                dictionary[instance].Ref2 = subst(instance.Ref2, dictionary);
                return dictionary[instance];
            }
        }

        private void assertRefsWithSubs(refSubstClass1 a, refSubstClass1 b, refSubstClass1 c, refSubstClass1 d)
        {
            Assert.AreEqual("A", a.Name);
            Assert.AreEqual("B", b.Name);
            Assert.AreEqual("C", c.Name);
            Assert.AreEqual("D", d.Name);
            // First chain: a -> [b -> c -> d -> b]
            Assert.IsTrue(object.ReferenceEquals(a.Ref1, b));
            Assert.IsTrue(object.ReferenceEquals(b.Ref1, c));
            Assert.IsTrue(object.ReferenceEquals(c.Ref1, d));
            Assert.IsTrue(object.ReferenceEquals(d.Ref1, b));
            // Second chain: b -> d -> [a -> c -> a]
            Assert.IsTrue(object.ReferenceEquals(b.Ref2, d));
            Assert.IsTrue(object.ReferenceEquals(d.Ref2, a));
            Assert.IsTrue(object.ReferenceEquals(a.Ref2, c));
            Assert.IsTrue(object.ReferenceEquals(c.Ref2, a));
        }

        [Test]
        public void TestReferencesWithSubstitution()
        {
            refSubstClass1 a = new refSubstClass1 { Name = "A" }, b = new refSubstClass1 { Name = "B" }, c = new refSubstClass1 { Name = "C" }, d = new refSubstClass1 { Name = "D" };
            // First chain: a -> [b -> c -> d -> b]
            a.Ref1 = b;
            b.Ref1 = c;
            c.Ref1 = d;
            d.Ref1 = b;
            // Second chain: b -> d -> [a -> c -> a]
            b.Ref2 = d;
            d.Ref2 = a;
            a.Ref2 = c;
            c.Ref2 = a;

            var opts = new ClassifyOptions().AddTypeOptions(typeof(refSubstClass1), new refSubstOpts());

            var aXml = VerifyXml(ClassifyXml.Serialize(a, opts));
            var aNew = ClassifyXml.Deserialize<refSubstClass1>(aXml, opts);
            assertRefsWithSubs(aNew, aNew.Ref1, aNew.Ref2, aNew.Ref1.Ref2);
            Assert.IsTrue(XNode.DeepEquals(aXml, XElement.Parse(@"<item refid=""1"">
  <Name>A</Name>
  <Ref1 refid=""0"">
    <Name>B</Name>
    <Ref1 refid=""3"">
      <Name>C</Name>
      <Ref1 refid=""2"">
        <Name>D</Name>
        <Ref1 ref=""0"" />
        <Ref2 ref=""1"" />
        <Marker2>2</Marker2>
      </Ref1>
      <Ref2 ref=""1"" />
      <Marker2>2</Marker2>
    </Ref1>
    <Ref2 ref=""2"" />
    <Marker2>2</Marker2>
  </Ref1>
  <Ref2 ref=""3"" />
  <Marker2>2</Marker2>
</item>")));

            var bXml = VerifyXml(ClassifyXml.Serialize(b, opts));
            var bNew = ClassifyXml.Deserialize<refSubstClass1>(bXml, opts);
            assertRefsWithSubs(bNew.Ref2.Ref2, bNew, bNew.Ref1, bNew.Ref2);
            Assert.IsTrue(XNode.DeepEquals(bXml, XElement.Parse(@"<item refid=""0"">
  <Name>B</Name>
  <Ref1 refid=""1"">
    <Name>C</Name>
    <Ref1 refid=""3"">
      <Name>D</Name>
      <Ref1 ref=""0"" />
      <Ref2 refid=""2"">
        <Name>A</Name>
        <Ref1 ref=""0"" />
        <Ref2 ref=""1"" />
        <Marker2>2</Marker2>
      </Ref2>
      <Marker2>2</Marker2>
    </Ref1>
    <Ref2 ref=""2"" />
    <Marker2>2</Marker2>
  </Ref1>
  <Ref2 ref=""3"" />
  <Marker2>2</Marker2>
</item>")));

            var cXml = VerifyXml(ClassifyXml.Serialize(c, opts));
            var cNew = ClassifyXml.Deserialize<refSubstClass1>(cXml, opts);
            assertRefsWithSubs(cNew.Ref2, cNew.Ref1.Ref1, cNew, cNew.Ref1);
            Assert.IsTrue(XNode.DeepEquals(cXml, XElement.Parse(@"<item refid=""0"">
  <Name>C</Name>
  <Ref1 refid=""1"">
    <Name>D</Name>
    <Ref1 refid=""2"">
      <Name>B</Name>
      <Ref1 ref=""0"" />
      <Ref2 ref=""1"" />
      <Marker2>2</Marker2>
    </Ref1>
    <Ref2 refid=""3"">
      <Name>A</Name>
      <Ref1 ref=""2"" />
      <Ref2 ref=""0"" />
      <Marker2>2</Marker2>
    </Ref2>
    <Marker2>2</Marker2>
  </Ref1>
  <Ref2 ref=""3"" />
  <Marker2>2</Marker2>
</item>")));

            var dXml = VerifyXml(ClassifyXml.Serialize(d, opts));
            var dNew = ClassifyXml.Deserialize<refSubstClass1>(dXml, opts);
            assertRefsWithSubs(dNew.Ref2, dNew.Ref1, dNew.Ref1.Ref1, dNew);
            Assert.IsTrue(XNode.DeepEquals(dXml, XElement.Parse(@"<item refid=""0"">
  <Name>D</Name>
  <Ref1 refid=""1"">
    <Name>B</Name>
    <Ref1 refid=""2"">
      <Name>C</Name>
      <Ref1 ref=""0"" />
      <Ref2 refid=""3"">
        <Name>A</Name>
        <Ref1 ref=""1"" />
        <Ref2 ref=""2"" />
        <Marker2>2</Marker2>
      </Ref2>
      <Marker2>2</Marker2>
    </Ref1>
    <Ref2 ref=""0"" />
    <Marker2>2</Marker2>
  </Ref1>
  <Ref2 ref=""3"" />
  <Marker2>2</Marker2>
</item>")));
        }

        private class hiddenFieldBaseClass
        {
            private string _hiddenField = "47";
            public string BaseHidden { get { return _hiddenField; } }

            public string NormalField = "normal";
        }
        private class hiddenFieldDerivedClass : hiddenFieldBaseClass
        {
            private string _hiddenField = "42";
            public string DerivedHidden { get { return _hiddenField; } }
        }

        [Test]
        public void TestHiddenFields()
        {
            var derived = new hiddenFieldDerivedClass();
            {
                // XML
                var serialized = ClassifyXml.Serialize(derived);
                var deserialized = ClassifyXml.Deserialize<hiddenFieldDerivedClass>(serialized);

                Assert.AreEqual("47", deserialized.BaseHidden);
                Assert.AreEqual("42", deserialized.DerivedHidden);
                Assert.AreEqual("normal", deserialized.NormalField);
            }
            {
                // JSON
                var serialized = ClassifyJson.Serialize(derived);
                var deserialized = ClassifyJson.Deserialize<hiddenFieldDerivedClass>(serialized);

                Assert.AreEqual("47", deserialized.BaseHidden);
                Assert.AreEqual("42", deserialized.DerivedHidden);
                Assert.AreEqual("normal", deserialized.NormalField);
            }
        }

        private class nonNullableTester
        {
            public string NullableString = "default";
            [ClassifyNotNull]
            public string NonNullableString = "default";
        }

        [Test]
        public void TestNonNullableFields()
        {
            var json = ClassifyJson.Serialize(new nonNullableTester { NullableString = null, NonNullableString = null });
            var obj = ClassifyJson.Deserialize<nonNullableTester>(json);
            Assert.IsNull(obj.NullableString);
            Assert.IsNotNull(obj.NonNullableString);

            var xml = ClassifyXml.Serialize(new nonNullableTester { NullableString = null, NonNullableString = null });
            obj = ClassifyXml.Deserialize<nonNullableTester>(xml);
            Assert.IsNull(obj.NullableString);
            Assert.IsNotNull(obj.NonNullableString);
        }

        private enum nonFlagsEnum { Zero = 0, One = 1, Two = 2 }

        [Flags]
        private enum flagsEnum { One = 1, Two = 2 }

        private class enforceEnumsTester
        {
            public nonFlagsEnum NonFlagsNonEnforce;
            [ClassifyEnforceEnum]
            public nonFlagsEnum NonFlagsEnforce;
            public flagsEnum FlagsNonEnforce;
            [ClassifyEnforceEnum]
            public flagsEnum FlagsEnforce;
        }

        private class enforceEnumsArraysTester
        {
            public nonFlagsEnum[] NonFlagsNonEnforce;
            [ClassifyEnforceEnum]
            public nonFlagsEnum[] NonFlagsEnforce;
            public flagsEnum[] FlagsNonEnforce;
            [ClassifyEnforceEnum]
            public flagsEnum[] FlagsEnforce;
        }

        private class enforceEnumsListsTester
        {
            public List<nonFlagsEnum> NonFlagsNonEnforce;
            [ClassifyEnforceEnum]
            public List<nonFlagsEnum> NonFlagsEnforce;
            public List<flagsEnum> FlagsNonEnforce;
            [ClassifyEnforceEnum]
            public List<flagsEnum> FlagsEnforce;
        }

        private class enforceEnumsDictionariesTester
        {
            public Dictionary<nonFlagsEnum, string> NonFlagsNonEnforce;
            [ClassifyEnforceEnum]
            public Dictionary<nonFlagsEnum, string> NonFlagsEnforce;
            public Dictionary<flagsEnum, string> FlagsNonEnforce;
            [ClassifyEnforceEnum]
            public Dictionary<flagsEnum, string> FlagsEnforce;
        }

        [Test]
        public void TestEnforceEnumsFields()
        {
            nonFlagsEnum[] allowedNonFlagsValues = new[] { 0, 1, 2 }.Select(i => (nonFlagsEnum) i).ToArray();
            flagsEnum[] allowedFlagsValues = new[] { 0, 1, 2, 3 }.Select(i => (flagsEnum) i).ToArray();

            foreach (var enforce in new[] { false, true })
            {
                for (int i = 0; i < 6; i++)
                {
                    var iFlags = (flagsEnum) i;
                    var iNonFlags = (nonFlagsEnum) i;

                    Assert.AreEqual(
                        allowedFlagsValues.Contains(iFlags) || !enforce ? iFlags : 0,
                        roundTrip(iFlags, enforce));
                    Assert.AreEqual(
                        allowedNonFlagsValues.Contains(iNonFlags) || !enforce ? iNonFlags : 0,
                        roundTrip(iNonFlags, enforce));

                    var test1 = roundTrip(new enforceEnumsTester
                    {
                        FlagsEnforce = iFlags,
                        FlagsNonEnforce = iFlags,
                        NonFlagsEnforce = iNonFlags,
                        NonFlagsNonEnforce = iNonFlags
                    }, enforce);
                    Assert.AreEqual(allowedFlagsValues.Contains(iFlags) ? iFlags : 0, test1.FlagsEnforce);
                    Assert.AreEqual(iFlags, test1.FlagsNonEnforce);
                    Assert.AreEqual(allowedNonFlagsValues.Contains(iNonFlags) ? iNonFlags : 0, test1.NonFlagsEnforce);
                    Assert.AreEqual(iNonFlags, test1.NonFlagsNonEnforce);
                }

                var test2 = roundTrip(new enforceEnumsArraysTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToArray(),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToArray(),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToArray(),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToArray()
                }, enforce);
                Assert.IsTrue(test2.FlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 0, 0));
                Assert.IsTrue(test2.FlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test2.NonFlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 0, 0, 0));
                Assert.IsTrue(test2.NonFlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                var test3 = roundTrip(new enforceEnumsListsTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToList(),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToList(),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToList(),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToList()
                }, enforce);
                Assert.IsTrue(test3.FlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3));
                Assert.IsTrue(test3.FlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test3.NonFlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2));
                Assert.IsTrue(test3.NonFlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                var test4 = roundTrip(new enforceEnumsDictionariesTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToDictionary(k => k, k => k.ToString())
                }, enforce);
                Assert.IsTrue(test4.FlagsEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3));
                Assert.IsTrue(test4.FlagsNonEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test4.NonFlagsEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2));
                Assert.IsTrue(test4.NonFlagsNonEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                Assert.IsTrue(test4.FlagsEnforce.Values.SequenceEqual("0", "One", "Two", "One, Two"));
                Assert.IsTrue(test4.FlagsNonEnforce.Values.SequenceEqual("0", "One", "Two", "One, Two", "4", "5"));
                Assert.IsTrue(test4.NonFlagsEnforce.Values.SequenceEqual("Zero", "One", "Two"));
                Assert.IsTrue(test4.NonFlagsNonEnforce.Values.SequenceEqual("Zero", "One", "Two", "3", "4", "5"));
            }
        }

        private class aipEnforceEnumsTester
        {
            public nonFlagsEnum NonFlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public nonFlagsEnum NonFlagsEnforce { get; set; }
            public flagsEnum FlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public flagsEnum FlagsEnforce { get; set; }
        }

        private class aipEnforceEnumsArraysTester
        {
            public nonFlagsEnum[] NonFlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public nonFlagsEnum[] NonFlagsEnforce { get; set; }
            public flagsEnum[] FlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public flagsEnum[] FlagsEnforce { get; set; }
        }

        private class aipEnforceEnumsListsTester
        {
            public List<nonFlagsEnum> NonFlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public List<nonFlagsEnum> NonFlagsEnforce { get; set; }
            public List<flagsEnum> FlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public List<flagsEnum> FlagsEnforce { get; set; }
        }

        private class aipEnforceEnumsDictionariesTester
        {
            public Dictionary<nonFlagsEnum, string> NonFlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public Dictionary<nonFlagsEnum, string> NonFlagsEnforce { get; set; }
            public Dictionary<flagsEnum, string> FlagsNonEnforce { get; set; }
            [ClassifyEnforceEnum]
            public Dictionary<flagsEnum, string> FlagsEnforce { get; set; }
        }

        [Test]
        public void TestEnforceEnumsProperties()
        {
            nonFlagsEnum[] allowedNonFlagsValues = new[] { 0, 1, 2 }.Select(i => (nonFlagsEnum) i).ToArray();
            flagsEnum[] allowedFlagsValues = new[] { 0, 1, 2, 3 }.Select(i => (flagsEnum) i).ToArray();

            foreach (var enforce in new[] { false, true })
            {
                for (int i = 0; i < 6; i++)
                {
                    var iFlags = (flagsEnum) i;
                    var iNonFlags = (nonFlagsEnum) i;

                    Assert.AreEqual(
                        allowedFlagsValues.Contains(iFlags) || !enforce ? iFlags : 0,
                        roundTrip(iFlags, enforce));
                    Assert.AreEqual(
                        allowedNonFlagsValues.Contains(iNonFlags) || !enforce ? iNonFlags : 0,
                        roundTrip(iNonFlags, enforce));

                    var test1 = roundTrip(new aipEnforceEnumsTester
                    {
                        FlagsEnforce = iFlags,
                        FlagsNonEnforce = iFlags,
                        NonFlagsEnforce = iNonFlags,
                        NonFlagsNonEnforce = iNonFlags
                    }, enforce);
                    Assert.AreEqual(allowedFlagsValues.Contains(iFlags) ? iFlags : 0, test1.FlagsEnforce);
                    Assert.AreEqual(iFlags, test1.FlagsNonEnforce);
                    Assert.AreEqual(allowedNonFlagsValues.Contains(iNonFlags) ? iNonFlags : 0, test1.NonFlagsEnforce);
                    Assert.AreEqual(iNonFlags, test1.NonFlagsNonEnforce);
                }

                var test2 = roundTrip(new aipEnforceEnumsArraysTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToArray(),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToArray(),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToArray(),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToArray()
                }, enforce);
                Assert.IsTrue(test2.FlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 0, 0));
                Assert.IsTrue(test2.FlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test2.NonFlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 0, 0, 0));
                Assert.IsTrue(test2.NonFlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                var test3 = roundTrip(new aipEnforceEnumsListsTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToList(),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToList(),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToList(),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToList()
                }, enforce);
                Assert.IsTrue(test3.FlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3));
                Assert.IsTrue(test3.FlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test3.NonFlagsEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2));
                Assert.IsTrue(test3.NonFlagsNonEnforce.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                var test4 = roundTrip(new aipEnforceEnumsDictionariesTester
                {
                    FlagsEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    FlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (flagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    NonFlagsEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToDictionary(k => k, k => k.ToString()),
                    NonFlagsNonEnforce = Enumerable.Range(0, 6).Select(i => (nonFlagsEnum) i).ToDictionary(k => k, k => k.ToString())
                }, enforce);
                Assert.IsTrue(test4.FlagsEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3));
                Assert.IsTrue(test4.FlagsNonEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));
                Assert.IsTrue(test4.NonFlagsEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2));
                Assert.IsTrue(test4.NonFlagsNonEnforce.Keys.Select(en => (int) en).SequenceEqual(0, 1, 2, 3, 4, 5));

                Assert.IsTrue(test4.FlagsEnforce.Values.SequenceEqual("0", "One", "Two", "One, Two"));
                Assert.IsTrue(test4.FlagsNonEnforce.Values.SequenceEqual("0", "One", "Two", "One, Two", "4", "5"));
                Assert.IsTrue(test4.NonFlagsEnforce.Values.SequenceEqual("Zero", "One", "Two"));
                Assert.IsTrue(test4.NonFlagsNonEnforce.Values.SequenceEqual("Zero", "One", "Two", "3", "4", "5"));
            }
        }

        private T roundTrip<T>(T testObject, bool enforce)
        {
            return ClassifyJson.Deserialize<T>(ClassifyJson.Serialize(testObject), new ClassifyOptions { EnforceEnums = enforce });
        }

        private class missingCtorMain
        {
            public missingCtorVal FooVal = new missingCtorVal(47);
            public missingCtorColl FooColl = new missingCtorColl(25);
        }

        private class missingCtorVal
        {
            public int Val { get; set; }
            public missingCtorVal(int val) { Val = val; }
        }

        private class missingCtorColl : ICollection<missingCtorVal>
        {
            private List<missingCtorVal> _list = new List<missingCtorVal>();
            public int Param { get; set; }
            public missingCtorColl(int param) { Param = param; }

            public void Add(missingCtorVal item) { _list.Add(item); }
            public void Clear() { _list.Clear(); }
            public bool Contains(missingCtorVal item) { return _list.Contains(item); }
            public void CopyTo(missingCtorVal[] array, int arrayIndex) { _list.CopyTo(array, arrayIndex); }
            public int Count { get { return _list.Count; } }
            public bool IsReadOnly { get { return false; } }
            public bool Remove(missingCtorVal item) { return _list.Remove(item); }
            public IEnumerator<missingCtorVal> GetEnumerator() { return _list.GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
        }

        [Test]
        public void TestMissingConstructors()
        {
            // Constructorless class at the top-level
            var xml = ClassifyXml.Serialize(new missingCtorVal(47));
            var val = new missingCtorVal(0);
            ClassifyXml.DeserializeIntoObject(xml, val);
            Assert.AreEqual(47, val.Val);

            // Constructorless class and collection at a nested level
            xml = ClassifyXml.Serialize(new missingCtorMain());
            var coll = ClassifyXml.Deserialize<missingCtorMain>(xml);
            Assert.AreEqual(47, coll.FooVal.Val);
            Assert.AreEqual(25, coll.FooColl.Param);

            coll.FooVal = new missingCtorVal(48);
            coll.FooColl = new missingCtorColl(26);
            xml = ClassifyXml.Serialize(coll);
            coll = ClassifyXml.Deserialize<missingCtorMain>(xml);
            Assert.AreEqual(48, coll.FooVal.Val);
            // Assert.AreEqual(26, coll.FooColl.Param);  // missing feature: this requires us to support classifying the properties of custom collections
        }

        [Test]
        public void TestMissingTypeSubstitution()
        {
            var xml = XElement.Parse(@"<item>
  <Items>
    <item type=""MisTypeMisDerived"" />
  </Items>
</item>");
            try
            {
                ClassifyXml.Deserialize<MisTypeOuter>(xml);
            }
            catch (Exception e)
            {
                Assert.IsFalse(e.Message.Contains("abstract class"));
                Assert.IsTrue(e.Message.Contains("MisTypeMisDerived"));
            }

            var opts = new ClassifyOptions()
                .AddTypeOptions(typeof(ObservableCollection<MisTypeBase>), new misTypeSubstOpts());
            misTypeSubstOpts.HasBeenCalled = false;
            var result = ClassifyXml.Deserialize<MisTypeOuter>(xml, opts);
            Assert.IsTrue(misTypeSubstOpts.HasBeenCalled);
            Assert.IsTrue(result.Items.Count == 1);
            Assert.IsTrue(result.Items[0].GetType() == typeof(MisTypeDerived));
        }
        class misTypeSubstOpts : ClassifyTypeOptions, IClassifyXmlTypeProcessor
        {
            void IClassifyTypeProcessor<XElement>.AfterSerialize(object obj, XElement element) { }
            void IClassifyTypeProcessor<XElement>.AfterDeserialize(object obj, XElement element) { }
            void IClassifyTypeProcessor<XElement>.BeforeSerialize(object obj) { }
            void IClassifyTypeProcessor<XElement>.BeforeDeserialize(XElement element)
            {
                HasBeenCalled = true;
                foreach (var el in element.Elements("item"))
                    if (el.Attribute("type").Value == "MisTypeMisDerived")
                        el.Attribute("type").Value = "MisTypeDerived";
            }
            public static bool HasBeenCalled = false;
        }
    }

    class MisTypeOuter
    {
        public ObservableCollection<MisTypeBase> Items = new ObservableCollection<MisTypeBase>();
    }
    abstract class MisTypeBase { }
    class MisTypeDerived : MisTypeBase { }
}
