using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    [TestFixture]
    public sealed class DiffTests
    {
        [Test]
        public void DiffTest()
        {
            Assert.Throws<ArgumentNullException>(() => { diffTest(null, null); });
            Assert.Throws<ArgumentNullException>(() => { diffTest("", null); });
            Assert.Throws<ArgumentNullException>(() => { diffTest(null, ""); });

            diffTest("", "");
            diffTest("a", "");
            diffTest("", "a");
            diffTest("aba", "b");
            diffTest("abcde", "abXde");
            diffTest("abcdef", "aXcdeY");
            diffTest("XYZAXYZ", "WYZAAXYW");
        }

        private void diffTest(string a, string b)
        {
            var diff = Ut.Diff(a, b);
            var a2 = diff.Where(x => x.Item2 != DiffOp.Ins).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(a2, a);
            var b2 = diff.Where(x => x.Item2 != DiffOp.Del).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(b2, b);
        }

        [Test]
        public void DiffTestPredicate()
        {
            var a = "abcdef";
            var b = "aXcdeY";
            var diff = Ut.Diff(a, b, predicate: c => c != 'd');
            var a2 = diff.Where(x => x.Item2 != DiffOp.Ins).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(a2, a);
            var b2 = diff.Where(x => x.Item2 != DiffOp.Del).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(b2, b);
        }

        [Test]
        public void DiffTestPostprocessor()
        {
            var a = "abcdef";
            var b = "aXcdeY";
            var diff = Ut.Diff(a, b, postProcessor: (aa, bb) =>
            {
                bool one = aa.SequenceEqual(new char[] { 'b' });
                bool two = aa.SequenceEqual(new char[] { 'f' });
                Assert.That(one || two);
                if (one)
                    Assert.That(bb.SequenceEqual(new char[] { 'X' }));
                else
                    Assert.That(bb.SequenceEqual(new char[] { 'Y' }));
                return aa.Select(c => Tuple.Create(c, DiffOp.Del)).Concat(bb.Select(c => Tuple.Create(c, DiffOp.Ins)));
            });
            var a2 = diff.Where(x => x.Item2 != DiffOp.Ins).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(a2, a);
            var b2 = diff.Where(x => x.Item2 != DiffOp.Del).Select(x => x.Item1.ToString()).JoinString();
            Assert.AreEqual(b2, b);
        }
    }
}
