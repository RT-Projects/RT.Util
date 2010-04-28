using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    [TestFixture]
    public sealed class NormalizeNewlinesStreamTests
    {
        [Test]
        public void TestNormalizeNewlinesStream()
        {
            var newlinePermutations = new byte[][] { new byte[] { 10 }, new byte[] { 13 }, new byte[] { 13, 10 } }.Permutations();
            foreach (var permutation in newlinePermutations)
            {
                var permutationArr = permutation.ToArray();
                var bytes =
                    new byte[] { 0x4C, 0x69, 0x6E, 0x65, 0x20, 0x31 }                       // "Line 1"
                    .Concat(permutationArr[0])
                    .Concat(new byte[] { 0x4C, 0x69, 0x6E, 0x65, 0x20, 0x32 })       // "Line 2"
                    .Concat(permutationArr[1])
                    .Concat(new byte[] { 0x4C, 0x69, 0x6E, 0x65, 0x20, 0x33 })       // "Line 3"
                    .Concat(permutationArr[2])
                    .Concat(new byte[] { 0x4C, 0x69, 0x6E, 0x65, 0x20, 0x34 })        // "Line 4"
                    .ToArray();

                for (int chunkSize1 = 1; chunkSize1 <= bytes.Length; chunkSize1++)
                {
                    for (int chunkSize2 = 1; chunkSize2 <= bytes.Length; chunkSize2++)
                    {
                        using (var str = new MemoryStream(bytes))
                        using (var slowMaker1 = new SlowStream(str, chunkSize1))
                        using (var normaliser = new NormalizeNewlinesStream(slowMaker1, "<NL>".ToUtf8()))
                        using (var slowMaker2 = new SlowStream(normaliser, chunkSize2))
                        {
                            var text = slowMaker2.ReadAllText(Encoding.UTF8);
                            Assert.AreEqual("Line 1<NL>Line 2<NL>Line 3<NL>Line 4", text);
                        }
                    }
                }
            }
        }
    }
}