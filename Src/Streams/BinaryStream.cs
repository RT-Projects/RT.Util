using System;
using System.IO;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    public interface IBinaryStreamSerializable
    {
        void WriteToBinaryStream(BinaryStream stream);
        void ReadFromBinaryStream(BinaryStream stream);
    }

    /// <summary>
    /// Does not use any buffering of its own. The underlying stream may be directly seeked, read, written etc
    /// without breaking the ordering of commands.
    /// </summary>
    public sealed class BinaryStream : Stream
    {
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private Stream _stream;
        private byte[] _buffer = new byte[8];

        public BinaryStream(Stream underlyingStream)
        {
            if (underlyingStream == null) throw new ArgumentNullException("underlyingStream");
            _stream = underlyingStream;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null) _reader.Dispose();
                _reader = null;
                if (_writer != null) _writer.Dispose();
                _writer = null;
                if (_stream != null) _stream.Dispose();
                _stream = null;
                _buffer = null;
            }
            base.Dispose(disposing);
        }

        private BinaryReader reader
        {
            get
            {
                if (_reader == null)
                    _reader = new BinaryReader(_stream, Encoding.UTF8);
                return _reader;
            }
        }

        private BinaryWriter writer
        {
            get
            {
                if (_writer == null)
                    _writer = new BinaryWriter(_stream, Encoding.UTF8);
                return _writer;
            }
        }

        private void readToBuffer(int bytes)
        {
            int read = _stream.Read(_buffer, 0, bytes);
            if (read != bytes) throw new EndOfStreamException("Unexpected end of stream.");
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("data");
            _stream.Write(buffer, offset, count);
        }


        public byte[] ReadBytes(int count)
        {
            byte[] buffer = new byte[count];
            int read = _stream.Read(buffer, 0, count);
            if (read != count) throw new EndOfStreamException("Attempted to read more bytes than are currently available in the stream.");
            return buffer;
        }

        public void WriteBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            _stream.Write(data, 0, data.Length);
        }


        public bool ReadBool()
        {
            readToBuffer(1);
            if (_buffer[0] == 0)
                return false;
            else if (_buffer[0] == 1)
                return true;
            else
                throw new InvalidDataException("The value read from stream is not a valid bool.");
        }

        public void WriteBool(bool value)
        {
            _buffer[0] = value ? (byte) 1 : (byte) 0;
            _stream.Write(_buffer, 0, 1);
        }


        public new byte ReadByte()
        {
            readToBuffer(1);
            return _buffer[0];
        }

        public override void WriteByte(byte value)
        {
            _buffer[0] = value;
            _stream.Write(_buffer, 0, 1);
        }


        public sbyte ReadSByte()
        {
            readToBuffer(1);
            return (sbyte) _buffer[0];
        }

        public void WriteSByte(sbyte value)
        {
            _buffer[0] = (byte) value;
            _stream.Write(_buffer, 0, 1);
        }


        public short ReadShort()
        {
            readToBuffer(2);
            return (short) ((_buffer[1] << 8) | _buffer[0]);
        }

        public void WriteShort(short value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _stream.Write(_buffer, 0, 2);
        }


        public ushort ReadUShort()
        {
            readToBuffer(2);
            return (ushort) ((_buffer[1] << 8) | _buffer[0]);
        }

        public void WriteUShort(ushort value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _stream.Write(_buffer, 0, 2);
        }


        public int ReadInt()
        {
            readToBuffer(4);
            return (int) (
                (_buffer[3] << 24) |
                (_buffer[2] << 16) |
                (_buffer[1] << 8) |
                _buffer[0]);
        }

        public void WriteInt(int value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _stream.Write(_buffer, 0, 4);
        }


        public uint ReadUInt()
        {
            readToBuffer(4);
            return (uint) (
                (_buffer[3] << 24) |
                (_buffer[2] << 16) |
                (_buffer[1] << 8) |
                _buffer[0]);
        }

        public void WriteUInt(uint value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _stream.Write(_buffer, 0, 4);
        }


        public long ReadLong()
        {
            readToBuffer(8);
            return
                ((long) ((_buffer[7] << 24) | (_buffer[6] << 16) | (_buffer[5] << 8) | (_buffer[4])) << 32)
                | ((long) ((_buffer[3] << 24) | (_buffer[2] << 16) | (_buffer[1] << 8) | _buffer[0]));
        }

        public void WriteLong(long value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 0x08);
            _buffer[2] = (byte) (value >> 0x10);
            _buffer[3] = (byte) (value >> 0x18);
            _buffer[4] = (byte) (value >> 0x20);
            _buffer[5] = (byte) (value >> 0x28);
            _buffer[6] = (byte) (value >> 0x30);
            _buffer[7] = (byte) (value >> 0x38);
            _stream.Write(_buffer, 0, 8);
        }


        public ulong ReadULong()
        {
            readToBuffer(8);
            return
                ((ulong) ((_buffer[7] << 24) | (_buffer[6] << 16) | (_buffer[5] << 8) | (_buffer[4])) << 32)
                | ((ulong) ((_buffer[3] << 24) | (_buffer[2] << 16) | (_buffer[1] << 8) | _buffer[0]));
        }

        public void WriteULong(ulong value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 0x08);
            _buffer[2] = (byte) (value >> 0x10);
            _buffer[3] = (byte) (value >> 0x18);
            _buffer[4] = (byte) (value >> 0x20);
            _buffer[5] = (byte) (value >> 0x28);
            _buffer[6] = (byte) (value >> 0x30);
            _buffer[7] = (byte) (value >> 0x38);
            _stream.Write(_buffer, 0, 8);
        }


        public float ReadFloat()
        {
            return reader.ReadSingle();
        }

        public void WriteFloat(float value)
        {
            writer.Write(value);
        }


        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        public void WriteDouble(double value)
        {
            writer.Write(value);
        }


        public decimal ReadDecimal()
        {
            return reader.ReadDecimal();
        }

        public void WriteDecimal(decimal value)
        {
            writer.Write(value);
        }


        public int ReadVarInt()
        {
            return _stream.ReadInt32Optim();
        }

        public void WriteVarInt(int value)
        {
            _stream.WriteInt32Optim(value);
        }


        public uint ReadVarUInt()
        {
            return _stream.ReadUInt32Optim();
        }

        public void WriteVarUInt(uint value)
        {
            _stream.WriteUInt32Optim(value);
        }


        public long ReadVarLong()
        {
            return _stream.ReadInt64Optim();
        }

        public void WriteVarLong(long value)
        {
            _stream.WriteInt64Optim(value);
        }


        public ulong ReadVarULong()
        {
            return _stream.ReadUInt64Optim();
        }

        public void WriteVarULong(ulong value)
        {
            _stream.WriteUInt64Optim(value);
        }


        public char ReadChar()
        {
            return reader.ReadChar();
        }

        public void WriteChar(char value)
        {
            writer.Write(value);
        }


        public string ReadString()
        {
            return reader.ReadString();
        }

        public void WriteString(string value)
        {
            writer.Write(value);
        }


        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadLong());
        }

        public void WriteDateTime(DateTime value)
        {
            WriteLong(value.ToBinary());
        }


        public TimeSpan ReadTimeSpan()
        {
            return TimeSpan.FromTicks(ReadLong());
        }

        public void WriteTimeSpan(TimeSpan value)
        {
            WriteLong(value.Ticks);
        }
    }
}
