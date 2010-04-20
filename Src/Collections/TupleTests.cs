using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.Collections
{
    [TestFixture]
    public sealed class TupleTests
    {
        sealed class dummyClass
        {
        }

        [Test]
        public void TestComparison()
        {
            // Tuple 2
            TestTuple2(40, 50, 40, 50, 0);

            TestTuple2(40, 49, 40, 50, -1);
            TestTuple2(41, 49, 40, 50, 1);

            TestTuple2(40, 51, 40, 50, 1);
            TestTuple2(39, 51, 40, 50, -1);

            // Tuple 3
            TestTuple3(30, 40, 50, 30, 40, 50, 0);

            TestTuple3(30, 40, 49, 30, 40, 50, -1);
            TestTuple3(30, 41, 49, 30, 40, 50, 1);
            TestTuple3(31, 40, 49, 30, 40, 50, 1);

            TestTuple3(30, 40, 51, 30, 40, 50, 1);
            TestTuple3(30, 39, 51, 30, 40, 50, -1);
            TestTuple3(29, 40, 51, 30, 40, 50, -1);

            // Tuple 4
            TestTuple4(20, 30, 40, 50, 20, 30, 40, 50, 0);

            TestTuple4(20, 30, 40, 49, 20, 30, 40, 50, -1);
            TestTuple4(20, 30, 41, 49, 20, 30, 40, 50, 1);
            TestTuple4(20, 31, 40, 49, 20, 30, 40, 50, 1);
            TestTuple4(21, 30, 40, 49, 20, 30, 40, 50, 1);

            TestTuple4(20, 30, 40, 51, 20, 30, 40, 50, 1);
            TestTuple4(20, 30, 39, 51, 20, 30, 40, 50, -1);
            TestTuple4(20, 29, 40, 51, 20, 30, 40, 50, -1);
            TestTuple4(19, 30, 40, 51, 20, 30, 40, 50, -1);

            // Exceptions
            try { (new RT.Util.ObsoleteTuple.Tuple<dummyClass, int>()).CompareTo(new RT.Util.ObsoleteTuple.Tuple<dummyClass, int>()); }
            catch (RTException e)
            {
                Assert.IsTrue(e.Message.Contains("T1"));
                Assert.IsTrue(e.Message.Contains("dummyClass"));
                Assert.IsTrue(e.Message.Contains("IComparable"));
            }
        }

        private void TestTuple2(int e1a, int e2a, int e1b, int e2b, int expected)
        {
            RT.Util.ObsoleteTuple.Tuple<int, int> a = new RT.Util.ObsoleteTuple.Tuple<int, int>(e1a, e2a);
            RT.Util.ObsoleteTuple.Tuple<int, int> b = new RT.Util.ObsoleteTuple.Tuple<int, int>(e1b, e2b);
            Assert.AreEqual(expected, a.CompareTo(b));
        }

        private void TestTuple3(int e1a, int e2a, int e3a, int e1b, int e2b, int e3b, int expected)
        {
            RT.Util.ObsoleteTuple.Tuple<int, int, int> a = new RT.Util.ObsoleteTuple.Tuple<int, int, int>(e1a, e2a, e3a);
            RT.Util.ObsoleteTuple.Tuple<int, int, int> b = new RT.Util.ObsoleteTuple.Tuple<int, int, int>(e1b, e2b, e3b);
            Assert.AreEqual(expected, a.CompareTo(b));
        }

        private void TestTuple4(int e1a, int e2a, int e3a, int e4a, int e1b, int e2b, int e3b, int e4b, int expected)
        {
            RT.Util.ObsoleteTuple.Tuple<int, int, int, int> a = new RT.Util.ObsoleteTuple.Tuple<int, int, int, int>(e1a, e2a, e3a, e4a);
            RT.Util.ObsoleteTuple.Tuple<int, int, int, int> b = new RT.Util.ObsoleteTuple.Tuple<int, int, int, int>(e1b, e2b, e3b, e4b);
            Assert.AreEqual(expected, a.CompareTo(b));
        }
    }
}
