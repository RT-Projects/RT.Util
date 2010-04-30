using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace RT.Util.Streams
{
    [TestFixture]
    public sealed class BinaryStreamTests
    {
        [Test]
        public void TestAllOperations()
        {
            var ms = new MemoryStream();
            var bs = new BinaryStream(ms);
            bs.WriteBool(true);
            bs.WriteBool(false);
            bs.WriteByte(byte.MinValue);
            bs.WriteByte(byte.MaxValue);
            bs.WriteSByte(sbyte.MinValue);
            bs.WriteSByte(sbyte.MaxValue);
            bs.WriteShort(short.MinValue);
            bs.WriteShort(short.MaxValue);
            bs.WriteUShort(ushort.MinValue);
            bs.WriteUShort(ushort.MaxValue);
            bs.WriteInt(int.MinValue);
            bs.WriteInt(int.MaxValue);
            bs.WriteUInt(uint.MinValue);
            bs.WriteUInt(uint.MaxValue);
            bs.WriteLong(long.MinValue);
            bs.WriteLong(long.MaxValue);
            bs.WriteULong(ulong.MinValue);
            bs.WriteULong(ulong.MaxValue);
            bs.WriteFloat(float.MinValue);
            bs.WriteFloat(float.MaxValue);
            bs.WriteFloat(float.NaN);
            bs.WriteFloat(float.NegativeInfinity);
            bs.WriteFloat(float.PositiveInfinity);
            bs.WriteFloat(float.Epsilon);
            bs.WriteDouble(double.MinValue);
            bs.WriteDouble(double.MaxValue);
            bs.WriteDouble(double.NaN);
            bs.WriteDouble(double.NegativeInfinity);
            bs.WriteDouble(double.PositiveInfinity);
            bs.WriteDouble(double.Epsilon);
            bs.WriteDecimal(decimal.MinValue);
            bs.WriteDecimal(decimal.MaxValue);
            bs.WriteVarInt(int.MinValue);
            bs.WriteVarInt(int.MaxValue);
            bs.WriteVarUInt(uint.MinValue);
            bs.WriteVarUInt(uint.MaxValue);
            bs.WriteVarLong(long.MinValue);
            bs.WriteVarLong(long.MaxValue);
            bs.WriteVarULong(ulong.MinValue);
            bs.WriteVarULong(ulong.MaxValue);
            bs.WriteChar(char.MinValue);
            bs.WriteChar(char.MaxValue);
            long pos = ms.Position;
            bs.WriteString("");
            bs.WriteString("test");
            bs.WriteString("проверка");
            bs.WriteString("a\U0001D41Aa");
            bs.WriteDateTime(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Utc));
            bs.WriteDateTime(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Local));
            bs.WriteTimeSpan(TimeSpan.MinValue);
            bs.WriteTimeSpan(TimeSpan.MaxValue);

            ms.Position = 0;

            Assert.AreEqual(true, bs.ReadBool());
            Assert.AreEqual(false, bs.ReadBool());
            Assert.AreEqual(byte.MinValue, bs.ReadByte());
            Assert.AreEqual(byte.MaxValue, bs.ReadByte());
            Assert.AreEqual(sbyte.MinValue, bs.ReadSByte());
            Assert.AreEqual(sbyte.MaxValue, bs.ReadSByte());
            Assert.AreEqual(short.MinValue, bs.ReadShort());
            Assert.AreEqual(short.MaxValue, bs.ReadShort());
            Assert.AreEqual(ushort.MinValue, bs.ReadUShort());
            Assert.AreEqual(ushort.MaxValue, bs.ReadUShort());
            Assert.AreEqual(int.MinValue, bs.ReadInt());
            Assert.AreEqual(int.MaxValue, bs.ReadInt());
            Assert.AreEqual(uint.MinValue, bs.ReadUInt());
            Assert.AreEqual(uint.MaxValue, bs.ReadUInt());
            Assert.AreEqual(long.MinValue, bs.ReadLong());
            Assert.AreEqual(long.MaxValue, bs.ReadLong());
            Assert.AreEqual(ulong.MinValue, bs.ReadULong());
            Assert.AreEqual(ulong.MaxValue, bs.ReadULong());
            Assert.AreEqual(float.MinValue, bs.ReadFloat());
            Assert.AreEqual(float.MaxValue, bs.ReadFloat());
            Assert.AreEqual(float.NaN, bs.ReadFloat());
            Assert.AreEqual(float.NegativeInfinity, bs.ReadFloat());
            Assert.AreEqual(float.PositiveInfinity, bs.ReadFloat());
            Assert.AreEqual(float.Epsilon, bs.ReadFloat());
            Assert.AreEqual(double.MinValue, bs.ReadDouble());
            Assert.AreEqual(double.MaxValue, bs.ReadDouble());
            Assert.AreEqual(double.NaN, bs.ReadDouble());
            Assert.AreEqual(double.NegativeInfinity, bs.ReadDouble());
            Assert.AreEqual(double.PositiveInfinity, bs.ReadDouble());
            Assert.AreEqual(double.Epsilon, bs.ReadDouble());
            Assert.AreEqual(decimal.MinValue, bs.ReadDecimal());
            Assert.AreEqual(decimal.MaxValue, bs.ReadDecimal());
            Assert.AreEqual(int.MinValue, bs.ReadVarInt());
            Assert.AreEqual(int.MaxValue, bs.ReadVarInt());
            Assert.AreEqual(uint.MinValue, bs.ReadVarUInt());
            Assert.AreEqual(uint.MaxValue, bs.ReadVarUInt());
            Assert.AreEqual(long.MinValue, bs.ReadVarLong());
            Assert.AreEqual(long.MaxValue, bs.ReadVarLong());
            Assert.AreEqual(ulong.MinValue, bs.ReadVarULong());
            Assert.AreEqual(ulong.MaxValue, bs.ReadVarULong());
            Assert.AreEqual(char.MinValue, bs.ReadChar());
            Assert.AreEqual(char.MaxValue, bs.ReadChar());
            Assert.AreEqual("", bs.ReadString());
            Assert.AreEqual("test", bs.ReadString());
            Assert.AreEqual("проверка", bs.ReadString());
            Assert.AreEqual("a\U0001D41Aa", bs.ReadString());
            Assert.AreEqual(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Utc), bs.ReadDateTime());
            Assert.AreEqual(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Local), bs.ReadDateTime());
            Assert.AreEqual(TimeSpan.MinValue, bs.ReadTimeSpan());
            Assert.AreEqual(TimeSpan.MaxValue, bs.ReadTimeSpan());

            ms.Position = pos;

            Assert.AreEqual("", bs.ReadString());
            Assert.AreEqual("test", bs.ReadString());
            Assert.AreEqual("проверка", bs.ReadString());
            Assert.AreEqual("a\U0001D41Aa", bs.ReadString());
            Assert.AreEqual(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Utc), bs.ReadDateTime());
            Assert.AreEqual(new DateTime(2001, 12, 31, 12, 34, 56, 789, DateTimeKind.Local), bs.ReadDateTime());
            Assert.AreEqual(TimeSpan.MinValue, bs.ReadTimeSpan());
            Assert.AreEqual(TimeSpan.MaxValue, bs.ReadTimeSpan());
        }

        [Test]
        public void TestNoBuffering()
        {
        }
    }
}
