using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.Collections;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class IEnumerableExtensionsTests
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
            Assert.IsTrue(aSorted.SequenceEqual(new List<int>() { 1, 2, 2, 2, 3, 4, 5, 9 }));

            List<string> s = new List<string>() { "some", "blah", "stuff", "apple" };
            List<string> sSorted = new List<string>(s.Order());
            Assert.IsTrue(sSorted.SequenceEqual(new List<string>() { "apple", "blah", "some", "stuff" }));
        }

        public sealed class StringIntTupleComparer : IComparer<Tuple<string, int>>
        {
            public int Compare(Tuple<string, int> x, Tuple<string, int> y)
            {
                return x.E1.CompareTo(y.E1);
            }
        }

        [Test]
        public void TestOrderLazy()
        {
            Random rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                var lst = new List<Tuple<string, int>>();
                lst.Add(new Tuple<string, int>("one", 1));
                lst.Add(new Tuple<string, int>("two", 1));
                lst.Add(new Tuple<string, int>("three", 1));
                for (int j = 2; j <= 100; j++)
                {
                    int r = rnd.Next(1, 4);
                    lst.Add(new Tuple<string, int>(i == 1 ? "one" : i == 2 ? "two" : "three", j));
                }
                var lstSorted = lst.OrderLazy(new StringIntTupleComparer());
                string lastString = null;
                int lastInt = 0;
                foreach (var a in lstSorted)
                {
                    if (a.E1 != lastString)
                    {
                        Assert.IsTrue((lastString == null && a.E1 == "one") || (lastString == "one" && a.E1 == "three") || (lastString == "three" && a.E1 == "two"));
                        lastString = a.E1;
                        lastInt = a.E2;
                    }
                    else
                    {
                        Assert.IsTrue(a.E2 > lastInt);
                        lastInt = a.E2;
                    }
                }
            }
        }
    }
}
