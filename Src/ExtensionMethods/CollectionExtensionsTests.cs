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
        private sealed class GenericParameter
        {
            public string SomeString;
        }

        [Test]
        public void TestAddSafe()
        {
            // **
            // **  void AddSafe<K, V>(this IDictionary<K, List<V>> dic, K key, V value)
            // **
            {
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>((IDictionary<string, List<string>>) null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>((IDictionary<string, List<string>>) null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>((IDictionary<string, HashSet<string>>) null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>((IDictionary<string, HashSet<string>>) null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>(new Dictionary<string, List<string>>(), null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string>(new Dictionary<string, HashSet<string>>(), null, null); });
                Assert.DoesNotThrow(() => { CollectionExtensions.AddSafe<string, string>(new Dictionary<string, List<string>>(), "", null); });
                Assert.DoesNotThrow(() => { CollectionExtensions.AddSafe<string, string>(new Dictionary<string, HashSet<string>>(), "", null); });

                var dicWithList = new Dictionary<string, List<GenericParameter>>();

                Assert.AreEqual(0, dicWithList.Count);
                Assert.Throws<KeyNotFoundException>(() => { var x = dicWithList["someKey"]; });
                dicWithList.AddSafe("someKey", new GenericParameter { SomeString = "someValue" });
                Assert.AreEqual(1, dicWithList.Count);
                var xList = dicWithList["someKey"];
                Assert.AreEqual(1, xList.Count);

                dicWithList.AddSafe("someKey", new GenericParameter { SomeString = "someOtherValue" });
                Assert.AreEqual(1, dicWithList.Count);
                Assert.AreEqual(2, xList.Count);

                Assert.IsTrue(xList.Select(g => g.SomeString).SequenceEqual(new string[] { "someValue", "someOtherValue" }));


                var dicWithHash = new Dictionary<string, HashSet<GenericParameter>>();

                Assert.AreEqual(0, dicWithHash.Count);
                Assert.Throws<KeyNotFoundException>(() => { var x = dicWithHash["someKey"]; });
                dicWithHash.AddSafe("someKey", new GenericParameter { SomeString = "someValue" });
                Assert.AreEqual(1, dicWithHash.Count);
                var xHash = dicWithHash["someKey"];
                Assert.AreEqual(1, xHash.Count);

                dicWithHash.AddSafe("someKey", new GenericParameter { SomeString = "someOtherValue" });
                Assert.AreEqual(1, dicWithHash.Count);
                Assert.AreEqual(2, xHash.Count);

                Assert.IsTrue(xHash.Select(g => g.SomeString).OrderBy(e => e).SequenceEqual(new string[] { "someOtherValue", "someValue" }));
            }

            // **
            // **  void AddSafe<K1, K2, V>(this IDictionary<K1, Dictionary<K2, V>> dic, K1 key1, K2 key2, V value)
            // **
            {
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, string>>) null, null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, string>>) null, null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, string>>) null, "", null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, string>>) null, "", "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, string>>(), null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, string>>(), null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, string>>(), "", null, null); });
                Assert.DoesNotThrow(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, string>>(), "", "", null); });

                var dic = new Dictionary<string, Dictionary<string, GenericParameter>>();

                Assert.AreEqual(0, dic.Count);
                Assert.Throws<KeyNotFoundException>(() => { var x = dic["someKey"]; });

                dic.AddSafe("someKey", "someOtherKey", new GenericParameter { SomeString = "someValue" });
                Assert.AreEqual(1, dic.Count);
                var x2 = dic["someKey"];
                Assert.AreEqual(1, x2.Count);
                var x3 = dic["someKey"]["someOtherKey"];
                Assert.AreEqual("someValue", x3.SomeString);

                dic.AddSafe("someKey", "someOtherKey", new GenericParameter { SomeString = "someOtherValue" });
                Assert.AreEqual(1, dic.Count);
                Assert.AreEqual(1, x2.Count);
                Assert.AreEqual("someValue", x3.SomeString);
            }

            // **
            // **  void AddSafe<K1, K2, V>(this IDictionary<K1, Dictionary<K2, List<V>>> dic, K1 key1, K2 key2, V value)
            // **
            {
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, List<string>>>) null, null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, List<string>>>) null, null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, List<string>>>) null, "", null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>((IDictionary<string, Dictionary<string, List<string>>>) null, "", "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, List<string>>>(), null, null, null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, List<string>>>(), null, "", null); });
                Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, List<string>>>(), "", null, null); });
                Assert.DoesNotThrow(() => { CollectionExtensions.AddSafe<string, string, string>(new Dictionary<string, Dictionary<string, List<string>>>(), "", "", null); });

                var dic = new Dictionary<string, Dictionary<string, List<GenericParameter>>>();

                Assert.AreEqual(0, dic.Count);
                Assert.Throws<KeyNotFoundException>(() => { var x = dic["someKey"]; });

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
        }

        [Test]
        public void TestIncSafe()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IncSafe<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IncSafe<string>(new Dictionary<string, int>(), null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IncSafe<string>(null, ""); });
            Assert.DoesNotThrow(() => { CollectionExtensions.IncSafe<string>(new Dictionary<string, int>(), ""); });

            var dic = new Dictionary<string, int>();
            Assert.AreEqual(0, dic.Count);
            Assert.Throws<KeyNotFoundException>(() => { var x = dic["someKey"]; });

            dic.IncSafe("someKey");
            Assert.AreEqual(1, dic.Count);
            Assert.DoesNotThrow(() => { { var x = dic["someKey"]; };});
            Assert.AreEqual(1, dic["someKey"]);

            dic.IncSafe("someKey", 47);
            Assert.AreEqual(1, dic.Count);
            Assert.DoesNotThrow(() => { { var x = dic["someKey"]; };});
            Assert.AreEqual(48, dic["someKey"]);

            dic.IncSafe("someOtherKey", 47);
            Assert.AreEqual(2, dic.Count);
            Assert.DoesNotThrow(() => { { var x = dic["someOtherKey"]; };});
            Assert.AreEqual(47, dic["someOtherKey"]);

            dic.IncSafe("someOtherKey");
            Assert.AreEqual(2, dic.Count);
            Assert.DoesNotThrow(() => { { var x = dic["someOtherKey"]; };});
            Assert.AreEqual(48, dic["someOtherKey"]);
        }

        [Test]
        public void TestShuffle()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Shuffle<string>(null); });

            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.DoesNotThrow(() => { { list.Shuffle(); };});
            Assert.AreEqual(10, list.Count);
            for (int i = 1; i <= 10; i++)
                Assert.IsTrue(list.Contains(i));
        }

        [Test]
        public void TestDictionaryEqual()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.DictionaryEqual<string, string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.DictionaryEqual<string, string>(new Dictionary<string, string>(), null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.DictionaryEqual<string, string>(null, new Dictionary<string, string>()); });

            var dic1 = new Dictionary<string, string>();
            var dic2 = new Dictionary<string, string>();
            Assert.IsTrue(dic1.DictionaryEqual(dic2));
            Assert.IsTrue(dic2.DictionaryEqual(dic1));

            dic1.Add("key", "value");
            Assert.IsFalse(dic1.DictionaryEqual(dic2));
            Assert.IsFalse(dic2.DictionaryEqual(dic1));

            dic2.Add("key", "value");
            Assert.IsTrue(dic1.DictionaryEqual(dic2));
            Assert.IsTrue(dic2.DictionaryEqual(dic1));
        }

        [Test]
        public void TestBinarySearch()
        {
            Assert.Throws<ArgumentNullException>(() => { int i1, i2; CollectionExtensions.BinarySearch<string, string>(null, null, out i1, out i2); });
            Assert.Throws<ArgumentNullException>(() => { int i1, i2; CollectionExtensions.BinarySearch<string, string>(new SortedList<string, string>(), null, out i1, out i2); });
            Assert.Throws<ArgumentNullException>(() => { int i1, i2; CollectionExtensions.BinarySearch<string, string>(null, "", out i1, out i2); });
            Assert.DoesNotThrow(() => { int i1, i2; CollectionExtensions.BinarySearch<string, string>(new SortedList<string, string>(), "", out i1, out i2); });

            SortedList<int, string> list = new SortedList<int, string>();

            Action<int, int, int> assert = (int key, int index1, int index2) =>
            {
                int i1, i2;
                list.BinarySearch(key, out i1, out i2);
                Assert.AreEqual(index1, i1);
                Assert.AreEqual(index2, i2);
            };

            assert(5, int.MinValue, int.MaxValue);

            list.Add(5, "five"); // 5
            assert(4, int.MinValue, 0);
            assert(5, 0, 0);
            assert(6, 0, int.MaxValue);

            list.Add(6, "six"); // 5, 6
            assert(4, int.MinValue, 0);
            assert(5, 0, 0);
            assert(6, 1, 1);
            assert(7, 1, int.MaxValue);

            list.Add(3, "three"); // 3, 5, 6
            assert(2, int.MinValue, 0);
            assert(3, 0, 0);
            assert(4, 0, 1);
            assert(5, 1, 1);
            assert(6, 2, 2);
            assert(7, 2, int.MaxValue);

            list.Add(15, "fifteen"); // 3, 5, 6, 15
            assert(2, int.MinValue, 0);
            assert(3, 0, 0);
            assert(4, 0, 1);
            assert(5, 1, 1);
            assert(6, 2, 2);
            assert(7, 2, 3);
            assert(11, 2, 3);
            assert(14, 2, 3);
            assert(15, 3, 3);
            assert(16, 3, int.MaxValue);

            assert(-999999999, int.MinValue, 0);
            assert(999999999, 3, int.MaxValue);
        }

        [Test]
        public void TestGet()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Get<string, string>(null, null, null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Get<string, string>(new Dictionary<string, string>(), null, null); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Get<string, string>(null, "", null); });
            Assert.DoesNotThrow(() => { CollectionExtensions.Get<string, string>(new Dictionary<string, string>(), "", null); });

            var dic = new Dictionary<string, string>();
            Assert.AreEqual(null, dic.Get("key", null));
            Assert.AreEqual("default", dic.Get("key", "default"));

            dic["key"] = "value";
            Assert.AreEqual("value", dic.Get("key", null));
            Assert.AreEqual("value", dic.Get("key", "default"));
            Assert.AreEqual(null, dic.Get("key2", null));
            Assert.AreEqual("default", dic.Get("key2", "default"));
        }

        [Test]
        public void TestSubarray()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Subarray<string>(null, 0); });
            Assert.DoesNotThrow(() => { CollectionExtensions.Subarray(new string[0], 0); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.Subarray<string>(null, 0, 0); });
            Assert.DoesNotThrow(() => { CollectionExtensions.Subarray(new string[0], 0, 0); });

            var inputArr = new int[] { 1, 2, 3, 4 };

            Assert.IsTrue(inputArr.Subarray(0).SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(inputArr.Subarray(1).SequenceEqual(new[] { 2, 3, 4 }));
            Assert.IsTrue(inputArr.Subarray(2).SequenceEqual(new[] { 3, 4 }));
            Assert.IsTrue(inputArr.Subarray(3).SequenceEqual(new[] { 4 }));
            Assert.IsTrue(inputArr.Subarray(4).SequenceEqual(new int[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(5); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1); });

            Assert.IsTrue(inputArr.Subarray(0, 0).SequenceEqual(new int[0]));
            Assert.IsTrue(inputArr.Subarray(1, 0).SequenceEqual(new int[0]));
            Assert.IsTrue(inputArr.Subarray(4, 0).SequenceEqual(new int[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(5, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 0); });

            Assert.IsTrue(inputArr.Subarray(0, 1).SequenceEqual(new[] { 1 }));
            Assert.IsTrue(inputArr.Subarray(3, 1).SequenceEqual(new[] { 4 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(4, 1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 1); });

            Assert.IsTrue(inputArr.Subarray(0, 2).SequenceEqual(new[] { 1, 2 }));
            Assert.IsTrue(inputArr.Subarray(2, 2).SequenceEqual(new[] { 3, 4 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(3, 2); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 2); });

            Assert.IsTrue(inputArr.Subarray(0, 3).SequenceEqual(new[] { 1, 2, 3 }));
            Assert.IsTrue(inputArr.Subarray(1, 3).SequenceEqual(new[] { 2, 3, 4 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(2, 3); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 3); });

            Assert.IsTrue(inputArr.Subarray(0, 4).SequenceEqual(new[] { 1, 2, 3, 4 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(1, 4); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 4); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(-1, 5); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(0, 5); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = inputArr.Subarray(1, 5); });
        }

        [Test]
        public void TestSubarrayEquals()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.SubarrayEquals<string>(null, 0, null, 0, 0); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.SubarrayEquals<string>(new string[0], 0, null, 0, 0); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.SubarrayEquals<string>(null, 0, new string[0], 0, 0); });
            Assert.DoesNotThrow(() => { CollectionExtensions.SubarrayEquals<string>(new string[0], 0, new string[0], 0, 0); });

            Assert.IsTrue(new string[0].SubarrayEquals(0, new string[0], 0, 0));

            var arr = new int[] { 1, 2, 3, 4, 5 };
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(-1, new int[0], 0, 0); });
            Assert.IsTrue(arr.SubarrayEquals(0, new int[0], 0, 0));
            Assert.IsTrue(arr.SubarrayEquals(1, new int[0], 0, 0));
            Assert.IsTrue(arr.SubarrayEquals(2, new int[0], 0, 0));
            Assert.IsTrue(arr.SubarrayEquals(3, new int[0], 0, 0));
            Assert.IsTrue(arr.SubarrayEquals(4, new int[0], 0, 0));
            Assert.IsTrue(arr.SubarrayEquals(5, new int[0], 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(6, new int[0], 0, 0); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(-1, new int[] { 0 }, 0, 1); });
            Assert.IsTrue(arr.SubarrayEquals(0, new int[] { 1 }, 0, 1));
            Assert.IsTrue(arr.SubarrayEquals(1, new int[] { 2 }, 0, 1));
            Assert.IsTrue(arr.SubarrayEquals(2, new int[] { 3 }, 0, 1));
            Assert.IsTrue(arr.SubarrayEquals(3, new int[] { 4 }, 0, 1));
            Assert.IsTrue(arr.SubarrayEquals(4, new int[] { 5 }, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(5, new int[] { 6 }, 0, 1); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(-1, new int[] { 0, 1 }, 0, 2); });
            Assert.IsTrue(arr.SubarrayEquals(0, new int[] { 1, 2 }, 0, 2));
            Assert.IsTrue(arr.SubarrayEquals(1, new int[] { 2, 3 }, 0, 2));
            Assert.IsTrue(arr.SubarrayEquals(2, new int[] { 3, 4 }, 0, 2));
            Assert.IsTrue(arr.SubarrayEquals(3, new int[] { 4, 5 }, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(4, new int[] { 5, 6 }, 0, 2); });

            Assert.IsTrue(arr.SubarrayEquals(0, new int[] { 1, 2, 3, 4, 5 }, 0, 5));
            Assert.IsTrue(arr.SubarrayEquals(0, new int[] { 0, 1, 2, 3, 4, 5, 6 }, 1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(0, new int[] { 0, 1 }, -1, 2); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(0, new int[] { 0, 1 }, 1, 2); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(0, new int[] { 0, 1 }, 2, 2); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = arr.SubarrayEquals(0, new int[] { 0, 1 }, 3, 2); });
        }

        [Test]
        public void TestIndexOfSubarray()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IndexOfSubarray<string>(null, null, 0, 0); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IndexOfSubarray<string>(new string[0], null, 0, 0); });
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.IndexOfSubarray<string>(null, new string[0], 0, 0); });
            Assert.DoesNotThrow(() => { CollectionExtensions.IndexOfSubarray<string>(new string[0], new string[0], 0, 0); });

            Assert.AreEqual(0, new int[0].IndexOfSubarray(new int[0], 0, 0));
            Assert.AreEqual(-1, new int[0].IndexOfSubarray(new int[] { 1 }, 0, 0));
            Assert.AreEqual(0, new int[] { 1 }.IndexOfSubarray(new int[] { 1 }, 0, 1));
            Assert.AreEqual(-1, new int[] { 1, 2 }.IndexOfSubarray(new int[] { 1 }, 1, 1));
            Assert.AreEqual(1, new int[] { 1 }.IndexOfSubarray(new int[0], 1, 0));

            Assert.AreEqual(2, new[] { 1, 2, 3, 4, 5 }.IndexOfSubarray(new[] { 3, 4 }, 0, 5));
            Assert.AreEqual(2, new[] { 1, 2, 3, 4, 5 }.IndexOfSubarray(new[] { 3, 4 }, 0, 4));
            Assert.AreEqual(-1, new[] { 1, 2, 3, 4, 5 }.IndexOfSubarray(new[] { 3, 4 }, 0, 3));
            Assert.AreEqual(-1, new[] { 1, 2, 3, 4, 5 }.IndexOfSubarray(new[] { 3, 4 }, 0, 2));
            Assert.AreEqual(-1, new[] { 1, 2, 3, 4, 5 }.IndexOfSubarray(new[] { 3, 4 }, 0, 1));
        }

        [Test]
        public void TestToHex()
        {
            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.ToHex((byte[]) null); });
            Assert.AreEqual("", new byte[0].ToHex());
            Assert.AreEqual("20", " ".ToUtf8().ToHex());
            Assert.AreEqual("E28692", "→".ToUtf8().ToHex().ToUpperInvariant());

            Assert.Throws<ArgumentNullException>(() => { CollectionExtensions.ToHex((uint[]) null); });
            Assert.AreEqual("", new uint[0].ToHex());
            Assert.AreEqual("DEADBEEF", new uint[] { 0xDEADBEEF }.ToHex().ToUpperInvariant());
            Assert.AreEqual("000000010000000200000003", new uint[] { 1, 2, 3 }.ToHex().ToUpperInvariant());
        }

        [Test]
        public void TestSumUnchecked()
        {
            var list = new[] { int.MaxValue, int.MaxValue - 1 };
            Assert.AreEqual(-3, list.SumUnchecked());

            var longList = new[] { long.MaxValue, long.MaxValue - 1 };
            Assert.AreEqual(-3, longList.SumUnchecked(lng => unchecked((int) lng)));
        }
    }
}
