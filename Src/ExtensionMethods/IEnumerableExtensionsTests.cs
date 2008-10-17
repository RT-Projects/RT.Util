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
        public void TestIEnumerableJoin()
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
        public void TestIEnumerableUniquePairs()
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
    }
}
