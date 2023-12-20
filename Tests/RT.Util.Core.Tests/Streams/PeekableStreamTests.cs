using NUnit.Framework;
using RT.Util.ExtensionMethods;
using RT.Util.Streams;

namespace RT.KitchenSink.Streams;

[TestFixture]
class PeekableStreamTests
{
    [Test]
    public void TestEmptyPeekableStreamRead()
    {
        using (var mem = new MemoryStream(new byte[0]))
        using (var peekable = new PeekableStream(mem))
        {
            using (var peek = peekable.GetPeekStream())
                Assert.AreEqual(0, peek.Read(new byte[1], 0, 1));
            Assert.AreEqual(0, peekable.Read(new byte[1], 0, 1));
        }
    }

    [Test]
    public void TestSingleBytePeekableStreamRead()
    {
        using (var mem = new MemoryStream(new byte[] { 47 }))
        using (var peekable = new PeekableStream(mem))
        {
            var buf = new byte[1];
            using (var peek = peekable.GetPeekStream())
            {
                Assert.AreEqual(1, peek.Read(buf, 0, 1));
                Assert.AreEqual(buf[0], 47);
                Assert.AreEqual(0, peek.Read(buf, 0, 1));
            }
            Assert.AreEqual(1, peekable.Read(buf, 0, 1));
            Assert.AreEqual(buf[0], 47);
            Assert.AreEqual(0, peekable.Read(buf, 0, 1));
            using (var peek = peekable.GetPeekStream())
                Assert.AreEqual(0, peek.Read(buf, 0, 1));
        }
    }

    [Test]
    public void TestPeekableStreamRead()
    {
        var inputStr = "← ↔ →";
        var buffer = inputStr.ToUtf8();
        for (int i = 1; i <= buffer.Length; i++)
        {
            using (var mem = new MemoryStream(buffer))
            using (var slow = new SlowStream(mem, i))
            using (var peekable = new PeekableStream(slow))
            {
                using (var peek = peekable.GetPeekStream())
                {
                    var buf = peek.Read(11);
                    Assert.AreEqual(inputStr, buf.FromUtf8());
                    Assert.AreEqual(0, peek.Read(buf, 0, 1));
                }
                var buf2 = peekable.Read(11);
                Assert.AreEqual(inputStr, buf2.FromUtf8());
                Assert.AreEqual(0, peekable.Read(buf2, 0, 1));
                using (var peek = peekable.GetPeekStream())
                    Assert.AreEqual(0, peek.Read(buf2, 0, 1));
            }
        }
    }
}
