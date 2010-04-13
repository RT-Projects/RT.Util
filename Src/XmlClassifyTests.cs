using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;

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
            private sealed classWithPrivateCtor()
            {
                Field = 9867;
            }

            public int Field = 7462;
        }

        private sealed class classWithNoCtor
        {
            private sealed classWithNoCtor(int dummy)
            {
            }
        }

        private sealed class classWithThrowingCtor
        {
            private sealed classWithThrowingCtor()
            {
                throw new Exception("Test exception");
            }
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
    }
}
