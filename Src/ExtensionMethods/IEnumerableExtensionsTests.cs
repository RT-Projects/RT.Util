using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public class IEnumerableExtensionsTests
    {
        [Test]
        public void TestJoin()
        {
            var one = new int[] { 4, 9, 14, 32, 8, 1, 2, 1001, 93, 529 };
            var two = new string[] { "The", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog" };
            var iter = one.Join(two).GetEnumerator();
            foreach (int i in one)
            {
                foreach (string j in two)
                {
                    Assert.IsTrue(iter.MoveNext());
                    Assert.AreEqual(i, iter.Current.E1);
                    Assert.AreEqual(j, iter.Current.E2);
                }
            }
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void TestUniquePairs()
        {
            var one = new int[] { 4, 9, 14, 32, 8, 1, 2, 1001, 93, 529 };
            var iter = one.UniquePairs().GetEnumerator();
            for (int i = 0; i < 10; i++)
            {
                for (int j = i + 1; j < 10; j++)
                {
                    Assert.IsTrue(iter.MoveNext());
                    Assert.AreEqual(one[i], iter.Current.E1);
                    Assert.AreEqual(one[j], iter.Current.E2);
                }
            }
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void TestSorted()
        {
            List<int> a = new List<int>() { 9, 3, 5, 1, 2, 4, 2, 2 };
            List<int> aSorted = new List<int>(a.Order());
            Assert.IsTrue(aSorted.EqualItems(new List<int>() { 1, 2, 2, 2, 3, 4, 5, 9 }));

            List<string> s = new List<string>() { "some", "blah", "stuff", "apple" };
            List<string> sSorted = new List<string>(s.Order());
            Assert.IsTrue(sSorted.EqualItems(new List<string>() { "apple", "blah", "some", "stuff" }));
        }

        [Test]
        public void TestEqualItems()
        {
            List<string> a, b;

            a = new List<string>();
            b = new List<string>();
            Assert.IsTrue(a.EqualItems(b));

            a = new List<string>() { "blah" };
            b = new List<string>() { "blah" };
            Assert.IsTrue(a.EqualItems(b));
            a = new List<string>() { "blah", "stuff" };
            b = new List<string>() { "blah", "stuff" };
            Assert.IsTrue(a.EqualItems(b));
            a = new List<string>() { "blah", "stuff" };
            b = new List<string>() { "stuff", "blah" };
            Assert.IsFalse(a.EqualItems(b));

            a = new List<string>() { "blah", "stuff" };
            b = new List<string>();
            Assert.IsFalse(a.EqualItems(b));
            a = new List<string>() { "blah", "stuff" };
            b = new List<string>() { "blah" };
            Assert.IsFalse(a.EqualItems(b));
            a = new List<string>() { "blah", "stuff" };
            b = new List<string>() { "blah", "stuff", "apples" };
            Assert.IsFalse(a.EqualItems(b));
        }
    }
}
