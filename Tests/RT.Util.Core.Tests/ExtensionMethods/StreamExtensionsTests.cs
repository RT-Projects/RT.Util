using NUnit.Framework;

namespace RT.Util.ExtensionMethods;

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

        testOptimRoundtripDecimal(decimal.MaxValue);
        testOptimRoundtripDecimal(decimal.MinValue);

        for (int i = 0; i < 10000; i++)
        {
            decimal dec1 = randomDecimalInt();
            decimal dec2 = randomDecimalInt();
            testOptimRoundtripDecimal(dec1 - dec2);
            testOptimRoundtripDecimal(dec1 + dec2);
            testOptimRoundtripDecimal(dec1 * dec2);
            if (dec2 != 0)
                testOptimRoundtripDecimal(dec1 / dec2);
            if (dec1 != 0)
                testOptimRoundtripDecimal(dec2 / dec1);
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
        testOptimRoundtripDecimal(num);
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
        testOptimRoundtripDecimal(num);
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
        testOptimRoundtripDecimal(num);
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
        testOptimRoundtripDecimal(num);
    }

    private void testOptimRoundtripDecimal(decimal num)
    {
        var ms1 = new MemoryStream();
        ms1.WriteDecimalOptim(num);
        var ms2 = new MemoryStream(ms1.ToArray());
        var read = ms2.ReadDecimalOptim();
        Assert.AreEqual(num, read); // assert semantic equality
        Assert.AreEqual(decimal.GetBits(num), decimal.GetBits(read)); // assert binary equality
        Assert.IsTrue(ms2.Read(new byte[1], 0, 1) == 0);
        ms1.Dispose();
        ms2.Dispose();
    }

    private static decimal randomDecimalInt()
    {
        if (Rnd.NextDouble() < 0.1)
            return Rnd.NextDouble() < 0.5 ? Rnd.Next() : -Rnd.Next();
        else
            return Rnd.NextDouble() < 0.5 ? Rnd.Next(65536) : -Rnd.Next(65536);
    }
}
