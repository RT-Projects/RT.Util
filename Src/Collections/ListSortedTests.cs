using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.Collections
{
    [TestFixture]
    public sealed class ListSortedTests
    {
        sealed class ReverseComparerInt : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }

        sealed class TestThingy : IComparable<TestThingy>
        {
            private int _val;
            private int _instance;
            private static int instanceCount = 999;
            public TestThingy(int val) { _val = val; _instance = instanceCount--; }
            public int Value { get { return _val; } }
            public int CompareTo(TestThingy other) { return _val.CompareTo(other._val); }
            public override string ToString() { return "<" + _val + " -- " + _instance + ">"; }
        }

        TestThingy A = new TestThingy(15);
        TestThingy B = new TestThingy(20);
        TestThingy C = new TestThingy(20);
        TestThingy D = new TestThingy(20);
        TestThingy E = new TestThingy(25);

        [Test]
        public void TestA()
        {
            var list = new ListSorted<int>();
            assertSeqEquals(list);
            list.Add(20);
            assertSeqEquals(list, 20);
            list.Add(25);
            assertSeqEquals(list, 20, 25);
            list.Add(15);
            assertSeqEquals(list, 15, 20, 25);
            list.Add(20);
            assertSeqEquals(list, 15, 20, 20, 25);

            list = new ListSorted<int>(new ReverseComparerInt());
            list.Add(20); list.Add(25); list.Add(15); list.Add(20);
            assertSeqEquals(list, 25, 20, 20, 15);

            list = new ListSorted<int>(20, new ReverseComparerInt());
            list.Add(20); list.Add(25); list.Add(15); list.Add(20);
            assertSeqEquals(list, 25, 20, 20, 15);

            try
            {
                list.Insert(0, 8);
                Assert.Fail();
            }
            catch { }
            try
            {
                list[0] = 5;
                Assert.Fail();
            }
            catch { }
        }

        [Test]
        public void TestB()
        {
            var list = new ListSorted<TestThingy>(20);
            list.Add(A); list.Add(B); list.Add(C); list.Add(D); list.Add(E);
            assertSeqEquals(list, A, B, C, D, E);
            try
            {
                assertSeqEquals(list, A, C, B, D, E);
                Assert.Fail();
            }
            catch { }
        }

        [Test]
        public void TestC()
        {
            var list = new ListSorted<TestThingy>();
            list.Add(E); list.Add(B); list.Add(D); list.Add(C); list.Add(A);
            assertSeqEquals(list, A, B, D, C, E);
            Assert.AreEqual(0, list.IndexOf(A));
            Assert.AreEqual(1, list.IndexOf(B));
            Assert.AreEqual(1, list.IndexOf(C));
            Assert.AreEqual(1, list.IndexOf(D));
            Assert.AreEqual(4, list.IndexOf(E));
            Assert.AreEqual(0, list.LastIndexOf(A));
            Assert.AreEqual(3, list.LastIndexOf(B));
            Assert.AreEqual(3, list.LastIndexOf(C));
            Assert.AreEqual(3, list.LastIndexOf(D));
            Assert.AreEqual(4, list.LastIndexOf(E));
            list.RemoveAt(2);
            assertSeqEquals(list, A, B, C, E);
        }

        [Test]
        public void TestD()
        {
            var list = new ListSorted<TestThingy>();
            list.AddRange(new TestThingy[] { D, C, B, A, E });
            assertSeqEquals(list, A, D, C, B, E);
            Assert.IsTrue(list.Remove(B));
            assertSeqEquals(list, A, C, B, E);

            list.Clear();
            list.AddRange(new TestThingy[] { D, C, B, A, E });
            assertSeqEquals(list, A, D, C, B, E);
            Assert.IsTrue(list.Contains(B));
            Assert.IsTrue(list.Contains(C));

            Assert.IsTrue(list.RemoveLast(C));
            assertSeqEquals(list, A, D, C, E);
            Assert.IsTrue(list.Contains(B));
            Assert.IsTrue(list.Contains(C));
            Assert.IsTrue(list.Contains(E));

            Assert.IsTrue(list.RemoveLast(E));
            Assert.IsFalse(list.Contains(E));
            Assert.AreEqual(-1, list.IndexOf(E));
            Assert.AreEqual(-1, list.LastIndexOf(E));
            Assert.IsFalse(list.Remove(E));
            Assert.IsFalse(list.RemoveLast(E));

            Assert.IsFalse(list.IsReadOnly);
        }

        [Test]
        public void TestF()
        {
            var list = new ListSorted<TestThingy>();
            int i = 0;
            list.AddRange(new TestThingy[] { D, C, B, A, E });
            foreach (var e in list)
            {
                switch (i)
                {
                    case 0: Assert.AreSame(A, e); break;
                    case 1: Assert.AreSame(D, e); break;
                    case 2: Assert.AreSame(C, e); break;
                    case 3: Assert.AreSame(B, e); break;
                    case 4: Assert.AreSame(E, e); break;
                    case 5: Assert.Fail(); break;
                }
                i++;
            }
            System.Collections.IEnumerable enumerable = list;
            i = 0;
            foreach (TestThingy e in enumerable)
            {
                switch (i)
                {
                    case 0: Assert.AreSame(A, e); break;
                    case 1: Assert.AreSame(D, e); break;
                    case 2: Assert.AreSame(C, e); break;
                    case 3: Assert.AreSame(B, e); break;
                    case 4: Assert.AreSame(E, e); break;
                    case 5: Assert.Fail(); break;
                }
                i++;
            }
        }

        [Test]
        public void TestG()
        {
            var list = new ListSorted<TestThingy>();
            list.Add(C);
            list.AddRange(new[] { E, D, B, A });
            assertSeqEquals(list, A, C, D, B, E);

            TestThingy[] arr = new TestThingy[6];
            list.CopyTo(arr, 1);
            Assert.IsNull(arr[0]);
            Assert.AreSame(arr[1], A);
            Assert.AreSame(arr[2], C);
            Assert.AreSame(arr[3], D);
            Assert.AreSame(arr[4], B);
            Assert.AreSame(arr[5], E);
        }

        private void assertSeqEquals(ListSorted<int> seq1, params int[] seq2)
        {
            Assert.AreEqual(seq1.Count, seq2.Length);
            for (int i = 0; i < seq1.Count; i++)
                Assert.AreEqual(seq1[i], seq2[i]);
        }

        private void assertSeqEquals(ListSorted<TestThingy> seq1, params TestThingy[] seq2)
        {
            Assert.AreEqual(seq1.Count, seq2.Length);
            for (int i = 0; i < seq1.Count; i++)
                Assert.AreSame(seq1[i], seq2[i]);
        }
    }
}
