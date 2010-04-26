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
            testOptimRoundtripU32(0);
            testOptimRoundtripU32(1);
            testOptimRoundtripU32(127);
            testOptimRoundtripU32(128);
            testOptimRoundtripU32(129);
            testOptimRoundtripU32(255);
            testOptimRoundtripU32(256);
            testOptimRoundtripU32(uint.MaxValue);
            testOptimRoundtripU32(uint.MaxValue - 1);

            testOptimRoundtripU64(0);
            testOptimRoundtripU64(1);
            testOptimRoundtripU64(127);
            testOptimRoundtripU64(128);
            testOptimRoundtripU64(129);
            testOptimRoundtripU64(255);
            testOptimRoundtripU64(256);
            testOptimRoundtripU64(ulong.MaxValue);
            testOptimRoundtripU64(ulong.MaxValue - 1);

            testOptimRoundtripS32(0);
            testOptimRoundtripS32(1);
            testOptimRoundtripS32(127);
            testOptimRoundtripS32(128);
            testOptimRoundtripS32(129);
            testOptimRoundtripS32(255);
            testOptimRoundtripS32(256);
            testOptimRoundtripS32_(int.MaxValue);
            testOptimRoundtripS32_(int.MaxValue - 1);
            testOptimRoundtripS32_(int.MinValue);
            testOptimRoundtripS32_(int.MinValue + 1);

            testOptimRoundtripS64(0);
            testOptimRoundtripS64(1);
            testOptimRoundtripS64(127);
            testOptimRoundtripS64(128);
            testOptimRoundtripS64(129);
            testOptimRoundtripS64(255);
            testOptimRoundtripS64(256);
            testOptimRoundtripS64_(long.MaxValue);
            testOptimRoundtripS64_(long.MaxValue - 1);
            testOptimRoundtripS64_(long.MinValue);
            testOptimRoundtripS64_(long.MinValue + 1);
        }

        private void testOptimRoundtripU32(uint num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteUInt32Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadUInt32Optim());
            Assert.IsTrue(ms2.Read(1) == null);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripU64(ulong num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteUInt64Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadUInt64Optim());
            Assert.IsTrue(ms2.Read(1) == null);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripS32(int num)
        {
            testOptimRoundtripS32_(num);
            testOptimRoundtripS32_(-num);
        }

        private void testOptimRoundtripS64(long num)
        {
            testOptimRoundtripS64_(num);
            testOptimRoundtripS64_(-num);
        }

        private void testOptimRoundtripS32_(int num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteInt32Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadInt32Optim());
            Assert.IsTrue(ms2.Read(1) == null);
            ms1.Dispose();
            ms2.Dispose();
        }

        private void testOptimRoundtripS64_(long num)
        {
            var ms1 = new MemoryStream();
            ms1.WriteInt64Optim(num);
            var ms2 = new MemoryStream(ms1.ToArray());
            Assert.AreEqual(num, ms2.ReadInt64Optim());
            Assert.IsTrue(ms2.Read(1) == null);
            ms1.Dispose();
            ms2.Dispose();
        }
    }
}
