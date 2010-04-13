using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class CollectionExtensionsTests
    {
        [Test]
        public void TestBinarySearch()
        {
            SortedList<int, string> list = new SortedList<int, string>();

            list.Add(5, "five"); // 5
            AssertSearch(list, 4, int.MinValue, 0);
            AssertSearch(list, 5, 0, 0);
            AssertSearch(list, 6, 0, int.MaxValue);

            list.Add(6, "six"); // 5, 6
            AssertSearch(list, 4, int.MinValue, 0);
            AssertSearch(list, 5, 0, 0);
            AssertSearch(list, 6, 1, 1);
            AssertSearch(list, 7, 1, int.MaxValue);

            list.Add(3, "three"); // 3, 5, 6
            AssertSearch(list, 2, int.MinValue, 0);
            AssertSearch(list, 3, 0, 0);
            AssertSearch(list, 4, 0, 1);
            AssertSearch(list, 5, 1, 1);
            AssertSearch(list, 6, 2, 2);
            AssertSearch(list, 7, 2, int.MaxValue);

            list.Add(15, "fifteen"); // 3, 5, 6, 15
            AssertSearch(list, 2, int.MinValue, 0);
            AssertSearch(list, 3, 0, 0);
            AssertSearch(list, 4, 0, 1);
            AssertSearch(list, 5, 1, 1);
            AssertSearch(list, 6, 2, 2);
            AssertSearch(list, 7, 2, 3);
            AssertSearch(list, 11, 2, 3);
            AssertSearch(list, 14, 2, 3);
            AssertSearch(list, 15, 3, 3);
            AssertSearch(list, 16, 3, int.MaxValue);

            AssertSearch(list, -999999999, int.MinValue, 0);
            AssertSearch(list, 999999999, 3, int.MaxValue);
        }

        private void AssertSearch(SortedList<int, string> list, int key, int index1, int index2)
        {
            int i1, i2;
            list.BinarySearch(key, out i1, out i2);
            Assert.AreEqual(index1, i1);
            Assert.AreEqual(index2, i2);
        }

        private sealed class GenericParameter
        {
            public string SomeString;
        }

        [Test]
        public void TestAddSafe1()
        {
            var dic = new Dictionary<string, List<GenericParameter>>();

            Assert.AreEqual(0, dic.Count);
            try
            {
                var x = dic["someKey"];
                Assert.Fail();
            }
            catch (KeyNotFoundException) { }

            dic.AddSafe("someKey", new GenericParameter { SomeString = "someValue" });
            Assert.AreEqual(1, dic.Count);
            var x2 = dic["someKey"];
            Assert.AreEqual(1, x2.Count);

            dic.AddSafe("someKey", new GenericParameter { SomeString = "someOtherValue" });
            Assert.AreEqual(1, dic.Count);
            Assert.AreEqual(2, x2.Count);

            Assert.IsTrue(x2.Select(g => g.SomeString).SequenceEqual(new string[] { "someValue", "someOtherValue" }));
        }

        [Test]
        public void TestAddSafe2()
        {
            var dic = new Dictionary<string, Dictionary<string, List<GenericParameter>>>();

            Assert.AreEqual(0, dic.Count);
            try
            {
                var x = dic["someKey"];
                Assert.Fail();
            }
            catch (KeyNotFoundException) { }

            dic.AddSafe("someKey", "someOtherKey", new GenericParameter { SomeString = "someValue" });
            Assert.AreEqual(1, dic.Count);
            var x2 = dic["someKey"];
            Assert.AreEqual(1, x2.Count);
            var x3 = dic["someKey"]["someOtherKey"];
            Assert.AreEqual(1, x3.Count);

            dic.AddSafe("someKey", "someOtherKey", new GenericParameter { SomeString = "someOtherValue" });
            Assert.AreEqual(1, dic.Count);
            Assert.AreEqual(1, x2.Count);
            Assert.AreEqual(2, x3.Count);

            Assert.IsTrue(x3.Select(g => g.SomeString).SequenceEqual(new string[] { "someValue", "someOtherValue" }));
        }

        [Test]
        public void TestIncSafe()
        {
            var dic = new Dictionary<string, int>();
            Assert.AreEqual(0, dic.Count);
            try
            {
                var x = dic["someKey"];
                Assert.Fail();
            }
            catch (KeyNotFoundException) { }

            dic.IncSafe("someKey");
            Assert.AreEqual(1, dic.Count);
            DoVoid(dic["someKey"]);
            Assert.AreEqual(1, dic["someKey"]);

            dic.IncSafe("someKey", 47);
            Assert.AreEqual(1, dic.Count);
            DoVoid(dic["someKey"]);
            Assert.AreEqual(48, dic["someKey"]);

            dic.IncSafe("someOtherKey", 47);
            Assert.AreEqual(2, dic.Count);
            DoVoid(dic["someOtherKey"]);
            Assert.AreEqual(47, dic["someOtherKey"]);

            dic.IncSafe("someOtherKey");
            Assert.AreEqual(2, dic.Count);
            DoVoid(dic["someOtherKey"]);
            Assert.AreEqual(48, dic["someOtherKey"]);
        }

        public void DoVoid(params object[] parameters) { }
    }
}
