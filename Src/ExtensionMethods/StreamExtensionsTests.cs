using System.IO;
using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class StreamExtensionsTests
    {
        [Test]
        public void TestOptimWrites()
        {
            for (int i = 0; i < 10000; i++)
            {
                testOptimRoundtripU32((uint) i);
                testOptimRoundtripU64((ulong) i);
                testOptimRoundtripS32(i);
                testOptimRoundtripS32(-i);
                testOptimRoundtripS64(i);
                testOptimRoundtripS64(-i);

                testOptimRoundtripU32(uint.MaxValue - (uint) i);
                testOptimRoundtripU64(ulong.MaxValue - (ulong) i);
                testOptimRoundtripS32(int.MinValue + i);
                testOptimRoundtripS32(int.MaxValue - i);
                testOptimRoundtripS64(long.MinValue + i);
                testOptimRoundtripS64(long.MaxValue - i);
            }
        }

        private void testOptimRoundtripU32(uint num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteUInt32Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadUInt32Optim());
            Assert.IsTrue(ms2.Read(new byte[1], 0, 1) == 0);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripU64(ulong num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteUInt64Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadUInt64Optim());
            Assert.IsTrue(ms2.Read(new byte[1], 0, 1) == 0);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripS32(int num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteInt32Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadInt32Optim());
            Assert.IsTrue(ms2.Read(new byte[1], 0, 1) == 0);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripS64(long num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteInt64Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadInt64Optim());
            Assert.IsTrue(ms2.Read(new byte[1], 0, 1) == 0);
            ms1.Dispose();
            ms2.Dispose();
        }
    }
}
