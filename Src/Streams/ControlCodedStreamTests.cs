using System;
using System.IO;
using NUnit.Framework;
using RT.Util.Streams;

namespace RT.KitchenSink.Streams
{
    [TestFixture]
    class ControlCodedStreamTests
    {
        [Test]
        public void TestControlCodedStream()
        {
            byte[] buffer;
            using (var memoryStream = new MemoryStream())
            using (var peekable = new PeekableStream(memoryStream))
            using (var ccs = new ControlCodedStream(peekable))
            using (var binary = new BinaryStream(ccs))
            {
                binary.WriteString("← ↔ →");
                ccs.WriteControlCode(47);
                binary.WriteChar((char) 255);
                ccs.WriteControlCode(48);
                binary.WriteVarInt(0x1FF);
                ccs.WriteControlCode(49);
                binary.WriteFloat(float.NaN);
                binary.WriteFloat(float.PositiveInfinity);
                binary.WriteFloat(float.NegativeInfinity);
                ccs.WriteControlCode(50);

                Assert.Throws<ArgumentOutOfRangeException>(() => ccs.WriteControlCode(255));

                binary.Close();
                ccs.Close();
                peekable.Close();
                memoryStream.Close();
                buffer = memoryStream.ToArray();
            }

            for (int i = 1; i < buffer.Length; i++)
            {
                using (var memoryStream = new MemoryStream(buffer))
                using (var slowStream = new SlowStream(memoryStream, i))
                using (var peekable = new PeekableStream(slowStream))
                using (var ccs = new ControlCodedStream(peekable))
                using (var binary = new BinaryStream(ccs))
                {
                    Assert.AreEqual("← ↔ →", binary.ReadString());
                    Assert.Throws<InvalidOperationException>(() => ccs.Read(new byte[1], 0, 1));
                    Assert.AreEqual(47, ccs.ReadControlCode());
                    Assert.AreEqual((char) 255, binary.ReadChar());
                    Assert.AreEqual(48, ccs.ReadControlCode());
                    Assert.AreEqual(0x1FF, binary.ReadVarInt());
                    Assert.AreEqual(49, ccs.ReadControlCode());
                    Assert.IsNaN(binary.ReadFloat());
                    Assert.AreEqual(-1, ccs.ReadControlCode());
                    Assert.AreEqual(float.PositiveInfinity, binary.ReadFloat());
                    Assert.AreEqual(float.NegativeInfinity, binary.ReadFloat());
                    Assert.AreEqual(50, ccs.ReadControlCode());
                    Assert.AreEqual(-1, ccs.ReadControlCode());
                    Assert.AreEqual(0, ccs.Read(new byte[1], 0, 1));
                }
            }
        }
    }
}
