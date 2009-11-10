using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.KitchenSink.Collections.Tests
{
    [TestFixture]
    public class SetTests
    {
        [Test]
        public void SetTests1()
        {
            Set<int> intSet = new Set<int>();
            Assert.IsTrue(intSet.IsEmpty);
            
            intSet.Add(1);
            Assert.IsFalse(intSet.IsEmpty);
            intSet.Add(2);
            intSet.Add(2);
            intSet.Add(3);
            Assert.AreEqual(3, intSet.Count);
            Assert.IsTrue(intSet.Contains(1));
            Assert.IsTrue(intSet.Contains(2));
            Assert.IsTrue(intSet.Contains(3));
            Assert.IsFalse(intSet.Contains(4));
            
            intSet.Remove(2);
            Assert.AreEqual(2, intSet.Count);
            Assert.IsTrue(intSet.Contains(1));
            Assert.IsFalse(intSet.Contains(2));

            Set<int> otherSet = new Set<int>();
            otherSet.Add(1);
            otherSet.Add(2);
            otherSet.Add(10);
            otherSet.Add(11);
            otherSet.Add(12);
            otherSet.Union(intSet);
            Assert.AreEqual(6, otherSet.Count);
            Assert.IsTrue(otherSet.Contains(1));
            Assert.IsTrue(otherSet.Contains(2));
            Assert.IsTrue(otherSet.Contains(3));
            Assert.IsTrue(otherSet.Contains(10));
            Assert.IsTrue(otherSet.Contains(11));
            Assert.IsTrue(otherSet.Contains(12));
            Assert.IsFalse(otherSet.Contains(0));
            Assert.IsFalse(otherSet.Contains(4));

            Set<int> anotherSet = new Set<int>();
            anotherSet.Add(1);
            otherSet.Intersect(anotherSet);
            Assert.AreEqual(1, otherSet.Count);
            Assert.IsTrue(otherSet.Contains(1));
            Assert.IsFalse(otherSet.Contains(2));

            foreach (int i in anotherSet)
                Assert.AreEqual(1, i);

            intSet.Clear();
            Assert.AreEqual(0, intSet.Count);
            Assert.IsFalse(intSet.Contains(1));
            Assert.IsTrue(intSet.IsEmpty);
            
            Assert.IsFalse(intSet.IsReadOnly);
        }
    }
}
