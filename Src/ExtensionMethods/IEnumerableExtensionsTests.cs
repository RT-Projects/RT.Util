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
        public void TestUniquePairs()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.UniquePairs<string>(null); });

            var one = new int[] { 4, 9, 14, 32, 8, 1, 2, 1001, 93, 529 };
            var iter = one.UniquePairs().GetEnumerator();
            for (int i = 0; i < 10; i++)
            {
                for (int j = i + 1; j < 10; j++)
                {
                    Assert.IsTrue(iter.MoveNext());
                    Assert.AreEqual(one[i], iter.Current.Item1);
                    Assert.AreEqual(one[j], iter.Current.Item2);
                }
            }
            Assert.IsFalse(iter.MoveNext());
        }

        [Test]
        public void TestJoin()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Join<string, string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Join<string, string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Join<string, string>(null, new string[0]); });

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
        public void TestOrder()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Order<string>(null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Order<string>(null, StringComparer.Ordinal); });

            int[] a = new[] { 9, 3, 5, 1, 2, 4, 2, 2 };
            var aSorted = a.Order();
            Assert.IsTrue(aSorted.SequenceEqual(new[] { 1, 2, 2, 2, 3, 4, 5, 9 }));

            var s = new[] { "some", "blah", "Stuff", "apple" };
            var sSorted = s.Order();
            Assert.IsTrue(sSorted.SequenceEqual(new[] { "apple", "blah", "some", "Stuff" }));

            sSorted = s.Order(StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(sSorted.SequenceEqual(new[] { "apple", "blah", "some", "Stuff" }));

            sSorted = s.Order(StringComparer.Ordinal);
            Assert.IsTrue(sSorted.SequenceEqual(new[] { "Stuff", "apple", "blah", "some" }));
        }

        [Test]
        public void TestOrderBy()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderBy<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderBy<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderBy<string>(null, (a, b) => a.CompareTo(b)); });

            var s = new[] { "some", "blah", "Stuff", "apple" };
            var sSorted = s.OrderBy((a, b) => a[1].CompareTo(b[1]));
            Assert.IsTrue(sSorted.SequenceEqual(new[] { "blah", "some", "apple", "Stuff" }));
        }

        [Test]
        public void TestSplit()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Split<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Split<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Split<string>(null, str => str != null); });

            var input = new[] { 1, 47, 4, 47, 5, 6, 1, 47, 47, 47, 0 };
            var result = input.Split(i => i == 47).ToArray();
            Assert.AreEqual(6, result.Length);
            Assert.That(result[0].SequenceEqual(new[] { 1 }));
            Assert.That(result[1].SequenceEqual(new[] { 4 }));
            Assert.That(result[2].SequenceEqual(new[] { 5, 6, 1 }));
            Assert.That(result[3].SequenceEqual(new int[] { }));
            Assert.That(result[4].SequenceEqual(new int[] { }));
            Assert.That(result[5].SequenceEqual(new[] { 0 }));
        }

        [Test]
        public void TestConcat()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Concat<string>((string) null, null); });
            Assert.DoesNotThrow(() => { IEnumerableExtensions.Concat<string>((string) null, new string[0]); });

            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Concat<string>(null, (string) null); });
            Assert.DoesNotThrow(() => { IEnumerableExtensions.Concat<string>(new string[0], (string) null); });

            var input = new[] { 1, 2, 3, 4 };
            Assert.IsTrue(input.Concat(5).SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
            Assert.IsTrue(5.Concat(input).SequenceEqual(new[] { 5, 1, 2, 3, 4 }));
        }

        [Test]
        public void TestOrderLazy()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderLazy<string>(null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderLazy<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderLazy<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.OrderLazy<string>(null, StringComparer.Ordinal); });

            // This tests that OrderLazy() is a _stable_ sort (the integers are for verifying the stability)
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
                var lstSorted = lst.OrderLazy(CustomComparer.Create<Tuple<string, int>>((x, y) => x.Item1.CompareTo(y.Item1)));
                string lastString = null;
                int lastInt = 0;
                foreach (var a in lstSorted)
                {
                    if (a.Item1 != lastString)
                    {
                        Assert.IsTrue((lastString == null && a.Item1 == "one") || (lastString == "one" && a.Item1 == "three") || (lastString == "three" && a.Item1 == "two"));
                        lastString = a.Item1;
                        lastInt = a.Item2;
                    }
                    else
                    {
                        Assert.IsTrue(a.Item2 > lastInt);
                        lastInt = a.Item2;
                    }
                }
            }
        }

        [Test]
        public void TestPermutations()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Permutations<string>(null); });

            var input = new[] { 1, 2, 3 };
            var result = input.Permutations().ToArray();

            Assert.AreEqual(6, result.Length);
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1, 2, 3 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1, 3, 2 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 2, 1, 3 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 2, 3, 1 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 3, 1, 2 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 3, 2, 1 })));
            Assert.IsFalse(result.Any(r => r.SequenceEqual(new[] { 1, 2, 3, 4 })));
        }

        [Test]
        public void TestSubsequences()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Subsequences<string>(null); });

            var input = new[] { 1, 2, 3 };
            var result = input.Subsequences().ToArray();

            Assert.AreEqual(8, result.Length);
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new int[] { })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 2 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 3 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1, 2 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1, 3 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 2, 3 })));
            Assert.IsTrue(result.Any(r => r.SequenceEqual(new[] { 1, 2, 3 })));
            Assert.IsFalse(result.Any(r => r.SequenceEqual(new[] { 1, 2, 3, 4 })));
        }

        [Test]
        public void TestFirstOrDefault()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.FirstOrDefault<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.FirstOrDefault<string>(null, null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.FirstOrDefault<string>(new string[0], null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.FirstOrDefault<string>(null, str => str != null, null); });
            Assert.DoesNotThrow(() => { IEnumerableExtensions.FirstOrDefault<string>(new string[0], str => str != null, null); });

            var input = new[] { "one", "two", "three", "four" };

            Assert.AreEqual("one", input.FirstOrDefault("five"));
            Assert.AreEqual("five", new string[] { }.FirstOrDefault("five"));

            Assert.AreEqual("three", input.FirstOrDefault(str => str.Length == 5, "five"));
            Assert.AreEqual("five", input.FirstOrDefault(str => str.Length == 0, "five"));
            Assert.AreEqual("one", input.FirstOrDefault(str => str.Length == 3, "five"));

            Assert.AreEqual("th", input.FirstOrDefault(str => str.Length == 5, str => str.Substring(0, 2), "five"));
            Assert.AreEqual("five", input.FirstOrDefault(str => str.Length == 0, str => str.Substring(0, 2), "five"));
            Assert.AreEqual("on", input.FirstOrDefault(str => str.Length == 3, str => str.Substring(0, 2), "five"));
        }

        [Test]
        public void TestAtMostOne()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.AtMostOne<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.AtMostOne<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.AtMostOne<string>(null, str => str != null); });

            var input = new[] { "one", "two", "three" };

            Assert.AreEqual(null, input.AtMostOne(str => str.Length == 0));
            Assert.AreEqual("three", input.AtMostOne(str => str.Length == 5));
            Assert.Throws<InvalidOperationException>(() => input.AtMostOne(str => str.Length == 3));
        }

        [Test]
        public void TestZipPadAndTruncate()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Zip<string, string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Zip<string, string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.Zip<string, string>(null, new string[0]); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipPad<string, string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipPad<string, string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipPad<string, string>(null, new string[0]); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipTruncate<string, string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipTruncate<string, string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.ZipTruncate<string, string>(null, new string[0]); });

#warning Add fuller testing to this when it no longer uses the obsolete Tuple<>
        }

        [Test]
        public void TestIndexOf()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.IndexOf<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.IndexOf<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.IndexOf<string>(null, str => str != null); });

            var input = new[] { 1, 2, 3, 4 };
            Assert.AreEqual(2, input.IndexOf(i => i == 3));
            Assert.AreEqual(2, input.IndexOf(i => i > 2));
            Assert.AreEqual(-1, input.IndexOf(i => i > 5));
        }

        [Test]
        public void TestMinElement()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.MinElement<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.MinElement<string>(new[] { "" }, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.MinElement<string>(null, str => str.Length); });
            Assert.Throws<InvalidOperationException>(() => IEnumerableExtensions.MinElement<string>(new string[0], str => str.Length));

            var input = new[] { "one", "two", "three", "four" };
            Assert.AreEqual("one", input.MinElement(str => str.Length));
            Assert.AreEqual("three", input.MinElement(str => -str.Length));
            Assert.AreEqual("three", input.MinElement(str => (int) str[1]));
            Assert.AreEqual("two", input.MinElement(str => -(int) str[0]));
        }

        [Test]
        public void TestSkipLast()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.SkipLast<string>(null, 5); });

            var input = new[] { "one", "two", "three", "four" };
            Assert.IsTrue(input.SkipLast(0).SequenceEqual(input));
            Assert.IsTrue(input.SkipLast(2).SequenceEqual(new[] { "one", "two" }));
            Assert.IsTrue(input.SkipLast(20).SequenceEqual(new string[0]));
        }

        [Test]
        public void TestTakeLast()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.TakeLast<string>(null, 5); });

            var input = new[] { "one", "two", "three", "four" };
            Assert.IsTrue(input.TakeLast(0).SequenceEqual(new string[0]));
            Assert.IsTrue(input.TakeLast(2).SequenceEqual(new[] { "three", "four" }));
            Assert.IsTrue(input.TakeLast(20).SequenceEqual(input));
        }

        [Test]
        public void TestStartsWith()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, new string[0]); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(new string[0], null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, null, StringComparer.Ordinal); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(new string[0], new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(null, new string[0], StringComparer.Ordinal); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.StartsWith<string>(new string[0], null, StringComparer.Ordinal); });

            var input = new[] { "one", "two", "three" };
            Assert.IsTrue(input.StartsWith(new string[] { }));
            Assert.IsTrue(input.StartsWith(new[] { "one" }));
            Assert.IsTrue(input.StartsWith(new[] { "one", "two" }));
            Assert.IsTrue(input.StartsWith(new[] { "one", "two", "three" }));
            Assert.IsFalse(input.StartsWith(new[] { "one", "two", "three", "four" }));
            Assert.IsFalse(input.StartsWith(new[] { "two" }));

            Assert.IsTrue(input.StartsWith(new string[] { }, StringComparer.OrdinalIgnoreCase));
            Assert.IsTrue(input.StartsWith(new[] { "One" }, StringComparer.OrdinalIgnoreCase));
            Assert.IsTrue(input.StartsWith(new[] { "One", "Two" }, StringComparer.OrdinalIgnoreCase));
            Assert.IsTrue(input.StartsWith(new[] { "One", "Two", "Three" }, StringComparer.OrdinalIgnoreCase));
            Assert.IsFalse(input.StartsWith(new[] { "One", "Two", "Three", "Four" }, StringComparer.OrdinalIgnoreCase));
            Assert.IsFalse(input.StartsWith(new[] { "Two" }, StringComparer.OrdinalIgnoreCase));
        }

        [Test]
        public void TestSelectIndexWhere()
        {
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.SelectIndexWhere<string>(null, null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.SelectIndexWhere<string>(new string[0], null); });
            Assert.Throws<ArgumentNullException>(() => { IEnumerableExtensions.SelectIndexWhere<string>(null, s => true); });
            Assert.DoesNotThrow(() => { IEnumerableExtensions.SelectIndexWhere<string>(new string[0], s => true); });

            var test = new[] { "one", "two", "three", "four", "five" };
            Assert.IsTrue(test.SelectIndexWhere(s => s.Length == 3).SequenceEqual(new[] { 0, 1 }));
            Assert.IsTrue(test.SelectIndexWhere(s => s[0] == 't').SequenceEqual(new[] { 1, 2 }));
            Assert.IsTrue(test.SelectIndexWhere(s => s == null).SequenceEqual(new int[0]));

            var test2 = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.IsTrue(test2.SelectIndexWhere(i => i % 3 == 0).SequenceEqual(new[] { 2, 5, 8 }));
            Assert.IsTrue(test2.SelectIndexWhere(i => false).SequenceEqual(new int[0]));
            Assert.IsTrue(test2.SelectIndexWhere(i => true).SequenceEqual(Enumerable.Range(0, 10)));
        }
    }
}
