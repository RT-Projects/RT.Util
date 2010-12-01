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
    public sealed class NewlineNormalizerStreamTests
    {
        [Test, Timeout(30000)]
        public void TestNewlineNormalizerStream8bit()
        {
            var newlinePermutations = new byte[][] { "\n".ToUtf8(), "\r".ToUtf8(), "\r\n".ToUtf8() }.Permutations();
            foreach (var permutation in newlinePermutations)
            {
                var permutationArr = permutation.ToArray();
                var bytes = "Line 1".ToUtf8().Concat(permutationArr[0])
                    .Concat("Line 2".ToUtf8()).Concat(permutationArr[1])
                    .Concat("Line 3".ToUtf8()).Concat(permutationArr[2])
                    .Concat("Line 4".ToUtf8()).ToArray();

                for (int chunkSize1 = 1; chunkSize1 <= bytes.Length; chunkSize1++)
                {
                    for (int chunkSize2 = 1; chunkSize2 <= bytes.Length; chunkSize2++)
                    {
                        using (var str = new MemoryStream(bytes))
                        using (var slowMaker1 = new SlowStream(str, chunkSize1))
                        using (var normaliser = new NewlineNormalizerStream8bit(slowMaker1, "<NL>".ToUtf8()))
                        using (var slowMaker2 = new SlowStream(normaliser, chunkSize2))
                        {
                            var text = slowMaker2.ReadAllText(Encoding.UTF8);
                            Assert.AreEqual("Line 1<NL>Line 2<NL>Line 3<NL>Line 4", text);
                        }
                    }
                }
            }
        }

        [Test, Timeout(30000)]
        public void TestNewlineNormalizerStream16bitLittleEndian()
        {
            var newlinePermutations = new byte[][] { "\n".ToUtf16(), "\r".ToUtf16(), "\r\n".ToUtf16() }.Permutations();
            foreach (var permutation in newlinePermutations)
            {
                var permutationArr = permutation.ToArray();
                var bytes = "Line 1".ToUtf16().Concat(permutationArr[0])
                    .Concat("Line 2".ToUtf16()).Concat(permutationArr[1])
                    .Concat("Line 3".ToUtf16()).Concat(permutationArr[2])
                    .Concat("Line 4".ToUtf16()).ToArray();

                for (int chunkSize1 = 1; chunkSize1 <= bytes.Length; chunkSize1++)
                {
                    for (int chunkSize2 = 1; chunkSize2 <= bytes.Length; chunkSize2++)
                    {
                        using (var str = new MemoryStream(bytes))
                        using (var slowMaker1 = new SlowStream(str, chunkSize1))
                        using (var normaliser = new NewlineNormalizerStream16bit(slowMaker1, "<NL>".ToUtf16()))
                        using (var slowMaker2 = new SlowStream(normaliser, chunkSize2))
                        {
                            var text = slowMaker2.ReadAllText(Encoding.Unicode);
                            Assert.AreEqual("Line 1<NL>Line 2<NL>Line 3<NL>Line 4", text);
                        }
                    }
                }
            }
        }

        [Test, Timeout(30000)]
        public void TestNewlineNormalizerStream16bitBigEndian()
        {
            var newlinePermutations = new byte[][] { "\n".ToUtf16BE(), "\r".ToUtf16BE(), "\r\n".ToUtf16BE() }.Permutations();
            foreach (var permutation in newlinePermutations)
            {
                var permutationArr = permutation.ToArray();
                var bytes = "Line 1".ToUtf16BE().Concat(permutationArr[0])
                    .Concat("Line 2".ToUtf16BE()).Concat(permutationArr[1])
                    .Concat("Line 3".ToUtf16BE()).Concat(permutationArr[2])
                    .Concat("Line 4".ToUtf16BE()).ToArray();

                for (int chunkSize1 = 1; chunkSize1 <= bytes.Length; chunkSize1++)
                {
                    for (int chunkSize2 = 1; chunkSize2 <= bytes.Length; chunkSize2++)
                    {
                        using (var str = new MemoryStream(bytes))
                        using (var slowMaker1 = new SlowStream(str, chunkSize1))
                        using (var normaliser = new NewlineNormalizerStream16bit(slowMaker1, "<NL>".ToUtf16BE(), true))
                        using (var slowMaker2 = new SlowStream(normaliser, chunkSize2))
                        {
                            var text = slowMaker2.ReadAllText(Encoding.BigEndianUnicode);
                            Assert.AreEqual("Line 1<NL>Line 2<NL>Line 3<NL>Line 4", text);
                        }
                    }
                }
            }
        }
    }
}