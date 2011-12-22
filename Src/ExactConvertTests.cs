using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RT.Util
{
    [TestFixture]
    public sealed class ExactConvertTests
    {
        #region ExactTry

        public struct AnUnsupportedStruct
        {
            public int aField;
            private string anotherField;
            public string suppressWarning { get { return anotherField; } set { anotherField = value; } }
        }

        public sealed class AnUnsupportedClass
        {
            private int aField;
            public string anotherField;
            public int suppressWarning { get { return aField; } set { aField = value; } }
        }

        #region Speed tests

        /// <summary>
        /// Note that this is not actually marked as a test. However since the code has already been
        /// written it seemed unreasonable to delete it, so instead it's been placed here. The speed
        /// of conversion on a Core 2 Duo 2.2 GHz averaged to a few microseconds so it's ok.
        /// </summary>
        public void TestExactToByteSpeed()
        {
            object[] objs = new object[10000];
            Random rnd = new Random();
            for (int i = 0; i < objs.Length; i++)
            {
                int R = rnd.Next(0, 8);
                switch (R)
                {
                    case 0: // byte
                        objs[i] = (byte) (rnd.NextDouble() * ((double) byte.MaxValue - byte.MinValue) + byte.MinValue);
                        break;
                    case 1: // sbyte
                        objs[i] = (sbyte) (rnd.NextDouble() * ((double) sbyte.MaxValue - sbyte.MinValue) + sbyte.MinValue);
                        break;
                    case 2: // int
                        objs[i] = (int) (rnd.NextDouble() * ((double) int.MaxValue - int.MinValue) + int.MinValue);
                        break;
                    case 3: // float
                        objs[i] = (float) (rnd.NextDouble() * ((double) float.MaxValue - float.MinValue) + float.MinValue);
                        break;
                    case 4: // decimal
                        objs[i] = (decimal) (rnd.NextDouble() * ((double) decimal.MaxValue - (double) decimal.MinValue) + (double) decimal.MinValue);
                        break;
                    case 5:
                        objs[i] = (rnd.NextDouble() * 1000).ToString("R");
                        break;
                    case 6:
                        objs[i] = rnd.Next(0, int.MaxValue).ToString();
                        break;
                    case 7:
                        objs[i] = (bool) (rnd.NextDouble() > 0.5);
                        break;
                }
            }
            int total = 0;
            for (int iters = 0; iters < 10000; iters++)
            {
                for (int i = 0; i < objs.Length; i++)
                {
                    byte result;
                    if (ExactConvert.Try(objs[i], out result))
                        total += (int) result;
                }
            }
            Console.WriteLine(total);
        }

        #endregion

        #region To integer/standard

        #region Unsigned

        [Test]
        public void TestExactToByte()
        {
            byte/**/ result;
            byte/**/ failValue = default(byte/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 147, out result)); Assert.AreEqual(147, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); Assert.AreEqual(118, result);
            Assert.IsTrue(ExactConvert.Try((short) 149, out result)); Assert.AreEqual(149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 150, out result)); Assert.AreEqual(150, result);
            Assert.IsTrue(ExactConvert.Try((int) 151, out result)); Assert.AreEqual(151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 152, out result)); Assert.AreEqual(152, result);
            Assert.IsTrue(ExactConvert.Try((long) 153, out result)); Assert.AreEqual(153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 154, out result)); Assert.AreEqual(154, result);
            Assert.IsTrue(ExactConvert.Try('A', out result)); Assert.AreEqual(65, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(156), out result)); Assert.AreEqual(156, result);
            Assert.IsTrue(ExactConvert.Try("157", out result)); Assert.AreEqual(157, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)217, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ushort) 15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -115018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 115018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-11111111111115018L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(11111111111115018UL, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(356), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("357", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToUShort()
        {
            ushort/**/ result;
            ushort/**/ failValue = default(ushort/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); Assert.AreEqual(118, result);
            Assert.IsTrue(ExactConvert.Try((short) 21149, out result)); Assert.AreEqual(21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) 41151, out result)); Assert.AreEqual(41151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 41152, out result)); Assert.AreEqual(41152, result);
            Assert.IsTrue(ExactConvert.Try((long) 41153, out result)); Assert.AreEqual(41153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 41154, out result)); Assert.AreEqual(41154, result);
            Assert.IsTrue(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(0x4747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(41156), out result)); Assert.AreEqual(41156, result);
            Assert.IsTrue(ExactConvert.Try("41157", out result)); Assert.AreEqual(41157, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) 91118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 91152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((long) 91153, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ulong) 91154, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(91155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("91156", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToUInt()
        {
            uint/**/ result;
            uint/**/ failValue = default(uint/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); Assert.AreEqual(118, result);
            Assert.IsTrue(ExactConvert.Try((short) 21149, out result)); Assert.AreEqual(21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) 311111151, out result)); Assert.AreEqual(311111151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 4111111152, out result)); Assert.AreEqual(4111111152, result);
            Assert.IsTrue(ExactConvert.Try((long) 4111111153, out result)); Assert.AreEqual(4111111153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 4111111154, out result)); Assert.AreEqual(4111111154, result);
            Assert.IsTrue(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(0x4747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(311111156), out result)); Assert.AreEqual(311111156, result);
            Assert.IsTrue(ExactConvert.Try("4111111157", out result)); Assert.AreEqual(4111111157, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((sbyte)-118, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((short)-15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -115018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((uint)4111111152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(91111111111154UL, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("91111111111156", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToULong()
        {
            ulong/**/ result;
            ulong/**/ failValue = default(ulong/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(ulong.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(long.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(DateTime.MaxValue.Ticks, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); Assert.AreEqual(118, result);
            Assert.IsTrue(ExactConvert.Try((short) 21149, out result)); Assert.AreEqual(21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) 311111151, out result)); Assert.AreEqual(311111151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 4111111152, out result)); Assert.AreEqual(4111111152, result);
            Assert.IsTrue(ExactConvert.Try(9111111111111111153L, out result)); Assert.AreEqual(9111111111111111153L, result);
            Assert.IsTrue(ExactConvert.Try(9911111111111111154UL, out result)); Assert.AreEqual(9911111111111111154UL, result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(0xA747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156L, result);
            Assert.IsTrue(ExactConvert.Try("9911111111111111157", out result)); Assert.AreEqual(9911111111111111157UL, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -115018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((uint)4111111152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact(91111111111154UL, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("18446744073709551657", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #region Signed

        [Test]
        public void TestExactToSByte()
        {
            sbyte/**/ result;
            sbyte/**/ failValue = default(sbyte/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 47, out result)); Assert.AreEqual(47, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 48, out result)); Assert.AreEqual(48, result);
            Assert.IsTrue(ExactConvert.Try((short) 49, out result)); Assert.AreEqual(49, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 50, out result)); Assert.AreEqual(50, result);
            Assert.IsTrue(ExactConvert.Try((int) 51, out result)); Assert.AreEqual(51, result);
            Assert.IsTrue(ExactConvert.Try((uint) 52, out result)); Assert.AreEqual(52, result);
            Assert.IsTrue(ExactConvert.Try((long) 53, out result)); Assert.AreEqual(53, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 54, out result)); Assert.AreEqual(54, result);
            Assert.IsTrue(ExactConvert.Try('A', out result)); Assert.AreEqual(65, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(56), out result)); Assert.AreEqual(56, result);
            Assert.IsTrue(ExactConvert.Try("57", out result)); Assert.AreEqual(57, result);
            // Out-of-range from all integer types
            Assert.IsFalse(ExactConvert.Try((byte) 218, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((sbyte)-118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ushort) 15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -115018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 115018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-11111111111115018L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(11111111111115018UL, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(356), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("-197", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToShort()
        {
            short/**/ result;
            short/**/ failValue = default(short/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 31150, out result)); Assert.AreEqual(31150, result);
            Assert.IsTrue(ExactConvert.Try((int) -31151, out result)); Assert.AreEqual(-31151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 31152, out result)); Assert.AreEqual(31152, result);
            Assert.IsTrue(ExactConvert.Try((long) -31153, out result)); Assert.AreEqual(-31153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 31154, out result)); Assert.AreEqual(31154, result);
            Assert.IsTrue(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(0x4747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(31156), out result)); Assert.AreEqual(31156, result);
            Assert.IsTrue(ExactConvert.Try("-31157", out result)); Assert.AreEqual(-31157, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((sbyte)-118, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((short)-15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ushort) 45051, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -2111115052, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 4111111152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(91111111111154UL, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("91111111111156", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToInt()
        {
            int/**/ result;
            int/**/ failValue = default(int/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(int.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) -311111151, out result)); Assert.AreEqual(-311111151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); Assert.AreEqual(311111152, result);
            Assert.IsTrue(ExactConvert.Try((long) -311111153, out result)); Assert.AreEqual(-311111153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 311111154, out result)); Assert.AreEqual(311111154, result);
            Assert.IsTrue(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(0x4747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(311111156), out result)); Assert.AreEqual(311111156, result);
            Assert.IsTrue(ExactConvert.Try("311111157", out result)); Assert.AreEqual(311111157, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((sbyte)-118, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((short)-15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((int)-115018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 4111111152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(91111111111154UL, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("91111111111156", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToLong()
        {
            long/**/ result;
            long/**/ failValue = default(long/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(int.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(long.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(long.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(DateTime.MaxValue.Ticks, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) -311111151, out result)); Assert.AreEqual(-311111151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); Assert.AreEqual(311111152, result);
            Assert.IsTrue(ExactConvert.Try(-9111111111111111153L, out result)); Assert.AreEqual(-9111111111111111153L, result);
            Assert.IsTrue(ExactConvert.Try(9111111111111111154UL, out result)); Assert.AreEqual(9111111111111111154UL, result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(0xA747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156L, result);
            Assert.IsTrue(ExactConvert.Try("-9111111111111111157", out result)); Assert.AreEqual(-9111111111111111157L, result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((sbyte)-118, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((short)-15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((int)-115018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((uint)4111111152, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(9911111111111111154UL, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("-9911111111111111157UL", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #endregion

        #region To integer/bool

        [Test]
        public void TestExactToBool()
        {
            bool/**/ result;
            bool/**/ failValue = default(bool/**/);
            // In-range from all integer types, for both values
            Assert.IsTrue(ExactConvert.Try((byte) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((byte) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((uint) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((uint) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((short) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((short) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((int) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((int) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try((long) 0, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try((long) 1, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try('\u0000', out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try('\u0001', out result)); Assert.AreEqual(true, result);
            Assert.IsTrue(ExactConvert.Try("fAlse", out result)); Assert.AreEqual(false, result);
            Assert.IsTrue(ExactConvert.Try("trUe", out result)); Assert.AreEqual(true, result);
            // Out-of-range from all integer types
            Assert.IsFalse(ExactConvert.Try((byte) 2, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ushort) 2, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 2, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ulong) 2, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -1, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -1, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) -1, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((long) -1, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try('\u0002', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("2", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("falsefalse", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("1", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(" true", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #region To integer/char

        [Test]
        public void TestExactToChar()
        {
            char/**/ result;
            char/**/ failValue = default(char/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual('\u0000', result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual('\u0001', result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual(char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual(char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsFalse(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); Assert.AreEqual(118, result);
            Assert.IsTrue(ExactConvert.Try((short) 21149, out result)); Assert.AreEqual(21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) 41151, out result)); Assert.AreEqual(41151, result);
            Assert.IsTrue(ExactConvert.Try((uint) 41152, out result)); Assert.AreEqual(41152, result);
            Assert.IsTrue(ExactConvert.Try((long) 41153, out result)); Assert.AreEqual(41153, result);
            Assert.IsTrue(ExactConvert.Try((ulong) 41154, out result)); Assert.AreEqual(41154, result);
            Assert.IsTrue(ExactConvert.Try('\u4747', out result)); Assert.AreEqual(0x4747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(41156), out result)); Assert.AreEqual(41156, result);
            Assert.IsTrue(ExactConvert.Try("á", out result)); Assert.AreEqual('á', result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((int) 91118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((uint) 91152, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((long) 91153, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((ulong) 91154, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new DateTime(91155), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("91156", out result)); Assert.AreEqual(failValue, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #region To integer/datetime

        public void AssertDateTimeUtcAndTicks(long value, ref DateTime result)
        {
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(value, result.Ticks);
        }

        public void AssertDateTimeUtcAndTicks(ulong value, ref DateTime result)
        {
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.AreEqual(value, result.Ticks);
        }

        [Test]
        public void TestExactToDateTime()
        {
            DateTime/**/ result;
            DateTime/**/ failValue = default(DateTime/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); AssertDateTimeUtcAndTicks(0, ref result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); AssertDateTimeUtcAndTicks(1, ref result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); AssertDateTimeUtcAndTicks(byte.MinValue, ref result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); AssertDateTimeUtcAndTicks(byte.MaxValue, ref result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); AssertDateTimeUtcAndTicks(ushort.MinValue, ref result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); AssertDateTimeUtcAndTicks(ushort.MaxValue, ref result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); AssertDateTimeUtcAndTicks(uint.MinValue, ref result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); AssertDateTimeUtcAndTicks(uint.MaxValue, ref result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); AssertDateTimeUtcAndTicks(ulong.MinValue, ref result);
            Assert.IsFalse(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); AssertDateTimeUtcAndTicks(char.MinValue, ref result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); AssertDateTimeUtcAndTicks(char.MaxValue, ref result);
            // Extremes of signed integers
            Assert.IsFalse(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); AssertDateTimeUtcAndTicks(sbyte.MaxValue, ref result);
            Assert.IsFalse(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); AssertDateTimeUtcAndTicks(short.MaxValue, ref result);
            Assert.IsFalse(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); AssertDateTimeUtcAndTicks(int.MaxValue, ref result);
            Assert.IsFalse(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(DateTime.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue.Ticks, out result)); Assert.AreEqual(DateTime.MaxValue, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); AssertDateTimeUtcAndTicks(247, ref result);
            Assert.IsTrue(ExactConvert.Try((sbyte) 118, out result)); AssertDateTimeUtcAndTicks(118, ref result);
            Assert.IsTrue(ExactConvert.Try((short) 21149, out result)); AssertDateTimeUtcAndTicks(21149, ref result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); AssertDateTimeUtcAndTicks(51150, ref result);
            Assert.IsTrue(ExactConvert.Try((int) 311111151, out result)); AssertDateTimeUtcAndTicks(311111151, ref result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); AssertDateTimeUtcAndTicks(311111152, ref result);
            Assert.IsTrue(ExactConvert.Try(3155378975999999999L, out result)); AssertDateTimeUtcAndTicks(3155378975999999999L, ref result);
            Assert.IsTrue(ExactConvert.Try(3155378975999999999UL, out result)); AssertDateTimeUtcAndTicks(3155378975999999999UL, ref result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); AssertDateTimeUtcAndTicks(0xA747, ref result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156L, result.Ticks);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(new DateTime(91111111111111156L), result);
            // Out-of-range from all integer types
            //impossible: Assert.IsFalse(ExactConvert.Exact((byte)218, out result)); AssertDateTimeUtcAndTicks(failValue, ref result);
            Assert.IsFalse(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try((short) -15018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((ushort)15018, out result)); AssertDateTimeUtcAndTicks(failValue, ref result);
            Assert.IsFalse(ExactConvert.Try((int) -115018, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact((uint)4111111152, out result)); AssertDateTimeUtcAndTicks(failValue, ref result);
            Assert.IsFalse(ExactConvert.Try(-91111111111153L, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(9911111111111111154UL, out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact('\u4747', out result)); Assert.AreEqual(failValue, result);
            //impossible: Assert.IsFalse(ExactConvert.Exact(new DateTime(91111111111155), out result)); Assert.AreEqual(failValue, result);

            // Valid strings (more extensive coverage done in the tests for the TryParseIso function used for this)
            Assert.IsTrue(ExactConvert.Try("2008-12-31 22:35:56.123Z", out result));
            Assert.AreEqual(new DateTime(2008, 12, 31, 22, 35, 56, 123), result); Assert.AreEqual(DateTimeKind.Utc, result.Kind);
            Assert.IsTrue(ExactConvert.Try("2008-12-31 22:30+01:00", out result));
            Assert.AreEqual(new DateTime(2008, 12, 31, 21, 30, 0), result); Assert.AreEqual(DateTimeKind.Local, result.Kind);
            Assert.IsTrue(ExactConvert.Try("2008-12-31", out result));
            Assert.AreEqual(new DateTime(2008, 12, 31), result); Assert.AreEqual(DateTimeKind.Unspecified, result.Kind);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("2008-12-32", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("2008-12-30X", out result)); Assert.AreEqual(failValue, result);
            // From fractional
            Assert.IsFalse(ExactConvert.Try(1.0f, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0d, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(1.0m, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(failValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #region To fractional

        [Test]
        public void TestExactToFloat()
        {
            float/**/ result;
            float/**/ failValue = default(float/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual((float/**/) uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual((float/**/) ulong.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual((float/**/) ulong.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual((float) char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual((float) char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual((float/**/) int.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual((float/**/) int.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual((float/**/) long.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual((float/**/) long.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual((float/**/) DateTime.MaxValue.Ticks, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) -311111151, out result)); Assert.AreEqual(-311111151f, result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); Assert.AreEqual(311111152f, result);
            Assert.IsTrue(ExactConvert.Try(-9111111111111111153L, out result)); Assert.AreEqual(-9111111111111111153f, result);
            Assert.IsTrue(ExactConvert.Try(9111111111111111154UL, out result)); Assert.AreEqual(9111111111111111154f, result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(0xA747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156f, result);
            // Valid strings
            Assert.IsTrue(ExactConvert.Try("-9111111111111111157", out result)); Assert.AreEqual(-9111111111111111157f, result);
            Assert.IsTrue(ExactConvert.Try("3.141592653589793", out result)); Assert.AreEqual(3.141592653589793f, result);
            Assert.IsTrue(ExactConvert.Try("1.401298E-45", out result)); Assert.AreEqual(float/**/.Epsilon, result);
            Assert.IsTrue(ExactConvert.Try("-3.40282347E+38", out result)); Assert.AreEqual(float/**/.MinValue, result);
            Assert.IsTrue(ExactConvert.Try("3.40282347E+38", out result)); Assert.AreEqual(float/**/.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try("-340282347000000000000000000000000000000", out result)); Assert.AreEqual(float/**/.MinValue, result);
            Assert.IsTrue(ExactConvert.Try("340282347000000000000000000000000000000", out result)); Assert.AreEqual(float/**/.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try("Inf", out result)); Assert.AreEqual(float/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try("-Inf", out result)); Assert.AreEqual(float/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try("NaN", out result)); Assert.AreEqual(float/**/.NaN, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional - float
            Assert.IsTrue(ExactConvert.Try(3.1415926f, out result)); Assert.AreEqual(3.1415926f, result);
            Assert.IsTrue(ExactConvert.Try(float.MinValue, out result)); Assert.AreEqual(float.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(float.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(float.Epsilon, out result)); Assert.AreEqual(float.Epsilon, result);
            Assert.IsTrue(ExactConvert.Try(float.NegativeInfinity, out result)); Assert.AreEqual(float/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try(float.PositiveInfinity, out result)); Assert.AreEqual(float/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try(float.NaN, out result)); Assert.AreEqual(float.NaN, result);
            // From fractional - double
            Assert.IsTrue(ExactConvert.Try(3.1415926d, out result)); Assert.AreEqual(3.1415926f, result);
            Assert.IsTrue(ExactConvert.Try(double.MinValue, out result)); Assert.AreEqual(float/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(float/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.Epsilon, out result)); Assert.AreEqual(0f, result);
            Assert.IsTrue(ExactConvert.Try(double.NegativeInfinity, out result)); Assert.AreEqual(float/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.PositiveInfinity, out result)); Assert.AreEqual(float/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.NaN, out result)); Assert.AreEqual(float/**/.NaN, result);
            // From fractional - decimal
            Assert.IsTrue(ExactConvert.Try(3.1415926m, out result)); Assert.AreEqual(3.1415926f, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MinValue, out result)); Assert.AreEqual(decimal.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(decimal.MaxValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToDouble()
        {
            double/**/ result;
            double/**/ failValue = default(double/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(ulong.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual((float) char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual((float) char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(int.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(long.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(long.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(DateTime.MaxValue.Ticks, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) -311111151, out result)); Assert.AreEqual(-311111151d, result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); Assert.AreEqual(311111152d, result);
            Assert.IsTrue(ExactConvert.Try(-9111111111111111153L, out result)); Assert.AreEqual(-9111111111111111153d, result);
            Assert.IsTrue(ExactConvert.Try(9111111111111111154UL, out result)); Assert.AreEqual(9111111111111111154d, result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(0xA747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156d, result);
            // Valid strings
            Assert.IsTrue(ExactConvert.Try("-9111111111111111157", out result)); Assert.AreEqual(-9111111111111111157d, result);
            Assert.IsTrue(ExactConvert.Try("3.141592653589793", out result)); Assert.AreEqual(3.141592653589793d, result);
            Assert.IsTrue(ExactConvert.Try("4.94065645841247E-324", out result)); Assert.AreEqual(double/**/.Epsilon, result);
            Assert.IsTrue(ExactConvert.Try("-1.7976931348623157E+308", out result)); Assert.AreEqual(double/**/.MinValue, result);
            Assert.IsTrue(ExactConvert.Try("1.7976931348623157E+308", out result)); Assert.AreEqual(double/**/.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try("-1797693134862315700E+290", out result)); Assert.AreEqual(double/**/.MinValue, result);
            Assert.IsTrue(ExactConvert.Try("1797693134862315700E+290", out result)); Assert.AreEqual(double/**/.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try("Inf", out result)); Assert.AreEqual(double/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try("-Inf", out result)); Assert.AreEqual(double/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try("NaN", out result)); Assert.AreEqual(double/**/.NaN, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            // From fractional - float
            Assert.IsTrue(ExactConvert.Try(3.141592f, out result)); Assert.AreEqual((double) 3.141592f, result);
            Assert.IsTrue(ExactConvert.Try(float.MinValue, out result)); Assert.AreEqual(float.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(float.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(float.Epsilon, out result)); Assert.AreEqual(float.Epsilon, result);
            Assert.IsTrue(ExactConvert.Try(float.NegativeInfinity, out result)); Assert.AreEqual(double/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try(float.PositiveInfinity, out result)); Assert.AreEqual(double/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try(float.NaN, out result)); Assert.AreEqual(float.NaN, result);
            // From fractional - double
            Assert.IsTrue(ExactConvert.Try(3.1415926d, out result)); Assert.AreEqual(3.1415926d, result);
            Assert.IsTrue(ExactConvert.Try(double.MinValue, out result)); Assert.AreEqual(double/**/.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(double/**/.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(double.Epsilon, out result)); Assert.AreEqual(double.Epsilon, result);
            Assert.IsTrue(ExactConvert.Try(double.NegativeInfinity, out result)); Assert.AreEqual(double/**/.NegativeInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.PositiveInfinity, out result)); Assert.AreEqual(double/**/.PositiveInfinity, result);
            Assert.IsTrue(ExactConvert.Try(double.NaN, out result)); Assert.AreEqual(double/**/.NaN, result);
            // From fractional - decimal
            Assert.IsTrue(ExactConvert.Try(3.1415926m, out result)); Assert.AreEqual(3.1415926d, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MinValue, out result)); Assert.AreEqual(decimal.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(decimal.MaxValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        [Test]
        public void TestExactToDecimal()
        {
            decimal/**/ result;
            decimal/**/ failValue = default(decimal/**/);
            // From bool (so we don't need to bother with this simple type further down)
            Assert.IsTrue(ExactConvert.Try(false, out result)); Assert.AreEqual(0, result);
            Assert.IsTrue(ExactConvert.Try(true, out result)); Assert.AreEqual(1, result);
            // Extremes of unsigned integers
            Assert.IsTrue(ExactConvert.Try(byte.MinValue, out result)); Assert.AreEqual(byte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(byte.MaxValue, out result)); Assert.AreEqual(byte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MinValue, out result)); Assert.AreEqual(ushort.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ushort.MaxValue, out result)); Assert.AreEqual(ushort.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MinValue, out result)); Assert.AreEqual(uint.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(uint.MaxValue, out result)); Assert.AreEqual(uint.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MinValue, out result)); Assert.AreEqual(ulong.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(ulong.MaxValue, out result)); Assert.AreEqual(ulong.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MinValue, out result)); Assert.AreEqual((float) char.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(char.MaxValue, out result)); Assert.AreEqual((float) char.MaxValue, result);
            // Extremes of signed integers
            Assert.IsTrue(ExactConvert.Try(sbyte.MinValue, out result)); Assert.AreEqual(sbyte.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(sbyte.MaxValue, out result)); Assert.AreEqual(sbyte.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MinValue, out result)); Assert.AreEqual(short.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(short.MaxValue, out result)); Assert.AreEqual(short.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MinValue, out result)); Assert.AreEqual(int.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(int.MaxValue, out result)); Assert.AreEqual(int.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MinValue, out result)); Assert.AreEqual(long.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(long.MaxValue, out result)); Assert.AreEqual(long.MaxValue, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MinValue, out result)); Assert.AreEqual(DateTime.MinValue.Ticks, result);
            Assert.IsTrue(ExactConvert.Try(DateTime.MaxValue, out result)); Assert.AreEqual(DateTime.MaxValue.Ticks, result);
            // In-range from all integer types
            Assert.IsTrue(ExactConvert.Try((byte) 247, out result)); Assert.AreEqual(247, result);
            Assert.IsTrue(ExactConvert.Try((sbyte) -118, out result)); Assert.AreEqual(-118, result);
            Assert.IsTrue(ExactConvert.Try((short) -21149, out result)); Assert.AreEqual(-21149, result);
            Assert.IsTrue(ExactConvert.Try((ushort) 51150, out result)); Assert.AreEqual(51150, result);
            Assert.IsTrue(ExactConvert.Try((int) -311111151, out result)); Assert.AreEqual(-311111151d, result);
            Assert.IsTrue(ExactConvert.Try((uint) 311111152, out result)); Assert.AreEqual(311111152d, result);
            Assert.IsTrue(ExactConvert.Try(-9111111111111111153L, out result)); Assert.AreEqual(-9111111111111111153d, result);
            Assert.IsTrue(ExactConvert.Try(9111111111111111154UL, out result)); Assert.AreEqual(9111111111111111154d, result);
            Assert.IsTrue(ExactConvert.Try('\uA747', out result)); Assert.AreEqual(0xA747, result);
            Assert.IsTrue(ExactConvert.Try(new DateTime(91111111111111156L), out result)); Assert.AreEqual(91111111111111156d, result);
            // Valid strings
            Assert.IsTrue(ExactConvert.Try("-9111111111111111157", out result)); Assert.AreEqual(-9111111111111111157d, result);
            Assert.IsTrue(ExactConvert.Try("3.141592653589793", out result)); Assert.AreEqual(3.141592653589793d, result);
            // Invalid strings
            Assert.IsFalse(ExactConvert.Try("", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("8s", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("0x20", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("-1797693134862315700E+290", out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try("1797693134862315700E+290", out result)); Assert.AreEqual(failValue, result);
            // From fractional - float
            Assert.IsTrue(ExactConvert.Try(3.141592f, out result)); Assert.AreEqual(3.141592m, result);
            Assert.IsFalse(ExactConvert.Try(float.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(float.Epsilon, out result)); Assert.AreEqual(0m, result);
            Assert.IsFalse(ExactConvert.Try(float.NegativeInfinity, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.PositiveInfinity, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(float.NaN, out result)); Assert.AreEqual(failValue, result);
            // From fractional - double
            Assert.IsTrue(ExactConvert.Try(3.1415926d, out result)); Assert.AreEqual(3.1415926d, result);
            Assert.IsFalse(ExactConvert.Try(double.MinValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.MaxValue, out result)); Assert.AreEqual(failValue, result);
            Assert.IsTrue(ExactConvert.Try(double.Epsilon, out result)); Assert.AreEqual(0m, result);
            Assert.IsFalse(ExactConvert.Try(double.NegativeInfinity, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.PositiveInfinity, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(double.NaN, out result)); Assert.AreEqual(failValue, result);
            // From fractional - decimal
            Assert.IsTrue(ExactConvert.Try(3.1415926m, out result)); Assert.AreEqual(3.1415926d, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MinValue, out result)); Assert.AreEqual(decimal.MinValue, result);
            Assert.IsTrue(ExactConvert.Try(decimal.MaxValue, out result)); Assert.AreEqual(decimal.MaxValue, result);
            // From unsupported
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);
        }

        #endregion

        #region To string & roundtrip

        [Test]
        public void TestExactToStringAndRoundtrip()
        {
            // Conversion to string is tested implicitly by converting to string and back,
            // as well as testing conversion from string to all other types (which is covered
            // by all the other tests in this file).

            // From unsupported to string
            string result;
            string failValue = default(string);
            Assert.IsFalse(ExactConvert.Try(null, out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedStruct(), out result)); Assert.AreEqual(failValue, result);
            Assert.IsFalse(ExactConvert.Try(new AnUnsupportedClass(), out result)); Assert.AreEqual(failValue, result);

            // Roundtrip corner cases
            CheckRoundtrip(true);
            CheckRoundtrip(false);
            CheckRoundtrip("");
            // Min/max of all types
            CheckRoundtrip(byte.MinValue); CheckRoundtrip(byte.MaxValue);
            CheckRoundtrip(sbyte.MinValue); CheckRoundtrip(sbyte.MaxValue);
            CheckRoundtrip(short.MinValue); CheckRoundtrip(short.MaxValue);
            CheckRoundtrip(ushort.MinValue); CheckRoundtrip(ushort.MaxValue);
            CheckRoundtrip(int.MinValue); CheckRoundtrip(int.MaxValue);
            CheckRoundtrip(uint.MinValue); CheckRoundtrip(uint.MaxValue);
            CheckRoundtrip(long.MinValue); CheckRoundtrip(long.MaxValue);
            CheckRoundtrip(ulong.MinValue); CheckRoundtrip(ulong.MaxValue);
            CheckRoundtrip(char.MinValue); CheckRoundtrip(char.MaxValue);
            CheckRoundtrip(DateTime.MinValue); CheckRoundtrip(DateTime.MaxValue);
            CheckRoundtrip(float.MinValue); CheckRoundtrip(float.MaxValue);
            CheckRoundtrip(double.MinValue); CheckRoundtrip(double.MaxValue);
            CheckRoundtrip(decimal.MinValue); CheckRoundtrip(decimal.MaxValue);
            // Special values of floating-point types
            CheckRoundtrip(float.NegativeInfinity);
            CheckRoundtrip(float.PositiveInfinity);
            CheckRoundtrip(float.NaN);
            CheckRoundtrip(float.Epsilon);
            CheckRoundtrip(double.NegativeInfinity);
            CheckRoundtrip(double.PositiveInfinity);
            CheckRoundtrip(double.NaN);
            CheckRoundtrip(double.Epsilon);

            // Roundtrip a few random values
            CheckRoundtrip((byte) 179);
            CheckRoundtrip((sbyte) -79);
            CheckRoundtrip((short) -13279);
            CheckRoundtrip((ushort) 53279);
            CheckRoundtrip((int) -279473972);
            CheckRoundtrip((uint) 279473972);
            CheckRoundtrip((long) -24729379473972);
            CheckRoundtrip((ulong) 27947334897972);
            CheckRoundtrip('\u1234');
            CheckRoundtrip(DateTime.Now);
            CheckRoundtrip(DateTime.UtcNow);
            CheckRoundtrip(new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Unspecified));
            CheckRoundtrip(-24729379473972000f);
            CheckRoundtrip(279473348979720000000d);
            CheckRoundtrip(27947334897972.2769862m);
        }

        public void CheckRoundtrip(object value)
        {
            string str;
            object rtripd;

            Assert.IsTrue(ExactConvert.Try(value, out str));

            TypeCode code = Type.GetTypeCode(value.GetType());
            switch (code)
            {
                case TypeCode.Boolean:
                    {
                        bool val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Byte:
                    {
                        byte val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.SByte:
                    {
                        sbyte val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        short val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        ushort val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        int val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        uint val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Int64:
                    {
                        long val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        ulong val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Single:
                    {
                        float val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Double:
                    {
                        double val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Decimal:
                    {
                        decimal val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.String:
                    {
                        string val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.Char:
                    {
                        char val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val);
                        break;
                    }
                case TypeCode.DateTime:
                    {
                        DateTime val;
                        Assert.IsTrue(ExactConvert.Try(str, out val)); rtripd = val;
                        Assert.AreEqual(value, val); Assert.AreEqual(((DateTime) value).Kind, ((DateTime) val).Kind);
                        break;
                    }
                default:
                    Assert.Fail("Unexpected TypeCode.");
                    return;
            }

            Assert.IsTrue(value.GetType() == rtripd.GetType());
        }

        #endregion

        #region ExactToType

        [Test]
        public void QuickTestExactToType()
        {
            // "Quick" refers to the apparent lack of proper coverage - we know the implementation of these methods,
            // and we know that the main method used in the implementation has been tested extensively above.

            Assert.AreEqual(true, ExactConvert.ToBool("true"));
            try { ExactConvert.ToBool("abc"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(255, ExactConvert.ToByte("255"));
            try { ExactConvert.ToByte("257"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(-128, ExactConvert.ToSByte("-128"));
            try { ExactConvert.ToSByte("-129"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(-32768, ExactConvert.ToShort("-32768"));
            try { ExactConvert.ToShort("-32769"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(65535, ExactConvert.ToUShort("65535"));
            try { ExactConvert.ToUShort("65536"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(-283734643, ExactConvert.ToInt("-283734643"));
            try { ExactConvert.ToInt("-23987492837432"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(2837492837, ExactConvert.ToUInt("2837492837"));
            try { ExactConvert.ToUInt("4298374928734987"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(-23984792837492873, ExactConvert.ToLong("-23984792837492873"));
            try { ExactConvert.ToLong("-184729387492836198764376"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(23984093284092834, ExactConvert.ToULong("23984093284092834"));
            try { ExactConvert.ToULong("2938749270938743987434"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(3.1415926f, ExactConvert.ToFloat("3.1415926"));
            try { ExactConvert.ToFloat("1e+40"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(3.141592612345678d, ExactConvert.ToDouble("3.141592612345678"));
            try { ExactConvert.ToDouble("1e+400"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(314159268283.198273987213m, ExactConvert.ToDecimal("314159268283.198273987213"));
            try { ExactConvert.ToDecimal("79228162514264337593543950336"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual(new DateTime(0), ExactConvert.ToDateTime("0001-01-01"));
            try { ExactConvert.ToDateTime("-987654321"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual('a', ExactConvert.ToChar("a"));
            try { ExactConvert.ToChar("aa"); Assert.Fail(); }
            catch (ExactConvertException) { }

            Assert.AreEqual("123", ExactConvert.ToString(123));
            try { ExactConvert.ToString(null); Assert.Fail(); }
            catch (ExactConvertException) { }
        }

        #endregion

        #endregion

        [Test]
        public void TestNullable()
        {
            Assert.AreEqual(ExactConvert.ToNullable.Int("-872").Value, -872);
            Assert.IsNull(ExactConvert.ToNullable.Int(null));

            Assert.AreEqual(ExactConvert.ToNullable.ULong("872").Value, 872);
            Assert.IsNull(ExactConvert.ToNullable.ULong(null));

            try
            {
                Assert.IsNull(ExactConvert.ToNullable.Int("blah"));
                Assert.Fail("Exception expected");
            }
            catch { }
            try
            {
                Assert.IsNull(ExactConvert.ToNullable.ULong("-872"));
                Assert.Fail("Exception expected");
            }
            catch { }
        }
    }
}
