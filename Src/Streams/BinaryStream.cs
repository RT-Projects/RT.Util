using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    /// <summary>Reads/writes various data types as binary values in a specific encoding.</summary>
    /// <remarks><para>This class does not use any buffering of its own.</para>
    /// <para>It is permissible to seek, read from, and write to the underlying stream.</para></remarks>
    public sealed class BinaryStream : Stream
    {
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private Stream _stream;
        private byte[] _buffer = new byte[8];

        /// <summary>Constructs a <see cref="BinaryStream"/> instance.</summary>
        /// <param name="underlyingStream">Provides an underlying stream from which to read and to which to write.</param>
        public BinaryStream(Stream underlyingStream)
        {
            if (underlyingStream == null)
                throw new ArgumentNullException("underlyingStream");
            _stream = underlyingStream;
        }

        /// <summary>Releases the unmanaged resources used by the stream and optionally releases the managed resources.</summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
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

        /// <summary>Gets an instance of <see cref="BinaryReader"/> for the underlying stream.</summary>
        private BinaryReader reader
        {
            get
            {
                if (_reader == null)
                    _reader = new BinaryReader(_stream, Encoding.UTF8);
                return _reader;
            }
        }

        /// <summary>Gets an instance of <see cref="BinaryWriter"/> for the underlying stream.</summary>
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
            int read = _stream.FillBuffer(_buffer, 0, bytes);
            if (read != bytes)
                throw new EndOfStreamException("Unexpected end of stream.");
        }

        /// <summary>Gets a value indicating whether the underlying stream supports reading.</summary>
        public override bool CanRead { get { return _stream.CanRead; } }
        /// <summary>Gets a value indicating whether the underlying stream supports seeking.</summary>
        public override bool CanSeek { get { return _stream.CanSeek; } }
        /// <summary>Gets a value indicating whether the underlying stream supports writing.</summary>
        public override bool CanWrite { get { return _stream.CanWrite; } }
        /// <summary>Flushes the underlying stream. Note that <see cref="BinaryStream"/> does not use any buffering of its own that requires flushing.</summary>
        public override void Flush() { _stream.Flush(); }
        /// <summary>Gets the length in bytes of the underlying stream.</summary>
        public override long Length { get { return _stream.Length; } }
        /// <summary>Gets or sets the position within the underlying stream.</summary>
        public override long Position { get { return _stream.Position; } set { _stream.Position = value; } }
        /// <summary>Sets the position within the underlying stream.</summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value indicating the reference point used.</param>
        /// <returns>The new position within the underlying stream.</returns>
        public override long Seek(long offset, SeekOrigin origin) { return _stream.Seek(offset, origin); }
        /// <summary>Sets the length of the underlying stream.</summary>
        /// <param name="value">The desired length of the underlying stream in bytes.</param>
        public override void SetLength(long value) { _stream.SetLength(value); }

        /// <summary>Reads a sequence of bytes from the underlying stream and advances the position
        /// within the stream by the number of bytes read.</summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified
        /// byte array with the values between offset and (offset + count - 1) replaced
        /// by the bytes read from the underlying source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        /// from the underlying stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the underlying stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes
        /// requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count) { return _stream.Read(buffer, offset, count); }

        /// <summary>Writes a sequence of bytes to the current stream and advances the current position within
        /// this stream by the number of bytes written.</summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from
        /// <paramref name="buffer"/> to the underlying stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the
        /// underlying stream.</param>
        /// <param name="count">The number of bytes to be written to the underlying stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            _stream.Write(buffer, offset, count);
        }

        /// <summary>Reads the specified number of bytes from the stream.</summary>
        /// <param name="count">Number of bytes to read from the stream.</param>
        /// <returns>A byte array containing exactly the number of bytes requested.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream was reached before the requested number of bytes could be read.</exception>
        public byte[] ReadBytes(int count)
        {
            byte[] buf = new byte[count];
            int read = _stream.FillBuffer(buf, 0, count);
            if (read != count)
                throw new EndOfStreamException("Unexpected end of stream encountered.");
            return buf;
        }

        /// <summary>Writes the specified byte array into the stream.</summary>
        /// <param name="data">Data to write to the stream.</param>
        public void WriteBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            _stream.Write(data, 0, data.Length);
        }

        /// <summary>Reads a boolean value from the stream.</summary>
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

        /// <summary>Writes a boolean value to the stream.</summary>
        public void WriteBool(bool value)
        {
            _buffer[0] = value ? (byte) 1 : (byte) 0;
            _stream.Write(_buffer, 0, 1);
        }

        /// <summary>Reads a byte from the stream.</summary>
        public new byte ReadByte()
        {
            readToBuffer(1);
            return _buffer[0];
        }

        /// <summary>Writes a byte to the stream.</summary>
        public override void WriteByte(byte value)
        {
            _buffer[0] = value;
            _stream.Write(_buffer, 0, 1);
        }

        /// <summary>Reads a signed byte from the stream.</summary>
        public sbyte ReadSByte()
        {
            readToBuffer(1);
            return (sbyte) _buffer[0];
        }

        /// <summary>Writes a signed byte to the stream.</summary>
        public void WriteSByte(sbyte value)
        {
            _buffer[0] = (byte) value;
            _stream.Write(_buffer, 0, 1);
        }

        /// <summary>Reads a 16-bit signed integer from the stream.</summary>
        public short ReadShort()
        {
            readToBuffer(2);
            return (short) ((_buffer[1] << 8) | _buffer[0]);
        }

        /// <summary>Writes a 16-bit signed integer to the stream.</summary>
        public void WriteShort(short value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        /// <summary>Reads a 16-bit unsigned integer from the stream.</summary>
        public ushort ReadUShort()
        {
            readToBuffer(2);
            return (ushort) ((_buffer[1] << 8) | _buffer[0]);
        }

        /// <summary>Writes a 16-bit unsigned integer to the stream.</summary>
        public void WriteUShort(ushort value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        /// <summary>Reads a 32-bit signed integer from the stream.</summary>
        public int ReadInt()
        {
            readToBuffer(4);
            return (int) (
                (_buffer[3] << 24) |
                (_buffer[2] << 16) |
                (_buffer[1] << 8) |
                _buffer[0]);
        }

        /// <summary>Writes a 32-bit signed integer to the stream.</summary>
        public void WriteInt(int value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        /// <summary>Reads a 32-bit unsigned integer from the stream.</summary>
        public uint ReadUInt()
        {
            readToBuffer(4);
            return (uint) (
                (_buffer[3] << 24) |
                (_buffer[2] << 16) |
                (_buffer[1] << 8) |
                _buffer[0]);
        }

        /// <summary>Writes a 32-bit unsigned integer to the stream.</summary>
        public void WriteUInt(uint value)
        {
            _buffer[0] = (byte) value;
            _buffer[1] = (byte) (value >> 8);
            _buffer[2] = (byte) (value >> 16);
            _buffer[3] = (byte) (value >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        /// <summary>Reads a 64-bit signed integer from the stream.</summary>
        public long ReadLong()
        {
            readToBuffer(8);
            return
                (((long) _buffer[7] << 24) | ((long) _buffer[6] << 16) | ((long) _buffer[5] << 8) | ((long) _buffer[4])) << 32
                | ((long) _buffer[3] << 24) | ((long) _buffer[2] << 16) | ((long) _buffer[1] << 8) | (long) _buffer[0];
        }

        /// <summary>Writes a 64-bit signed integer to the stream.</summary>
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

        /// <summary>Reads a 64-bit unsigned integer from the stream.</summary>
        public ulong ReadULong()
        {
            readToBuffer(8);
            return
                (((ulong) _buffer[7] << 24) | ((ulong) _buffer[6] << 16) | ((ulong) _buffer[5] << 8) | ((ulong) _buffer[4])) << 32
                | ((ulong) _buffer[3] << 24) | ((ulong) _buffer[2] << 16) | ((ulong) _buffer[1] << 8) | (ulong) _buffer[0];
        }

        /// <summary>Writes a 64-bit unsigned integer to the stream.</summary>
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

        /// <summary>Reads a single-precision floating-point number from the stream.</summary>
        public float ReadFloat()
        {
            return reader.ReadSingle();
        }

        /// <summary>Writes a single-precision floating-point number to the stream.</summary>
        public void WriteFloat(float value)
        {
            writer.Write(value);
        }

        /// <summary>Reads a double-precision floating-point number from the stream.</summary>
        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        /// <summary>Writes a double-precision floating-point number to the stream.</summary>
        public void WriteDouble(double value)
        {
            writer.Write(value);
        }

        /// <summary>Reads a decimal number from the stream.</summary>
        public decimal ReadDecimal()
        {
            return reader.ReadDecimal();
        }

        /// <summary>Writes a decimal number to the stream.</summary>
        public void WriteDecimal(decimal value)
        {
            writer.Write(value);
        }

        /// <summary>Reads a 32-bit signed integer using optim encoding (see <see cref="StreamExtensions.ReadInt32Optim"/>) from the stream.</summary>
        public int ReadVarInt()
        {
            return _stream.ReadInt32Optim();
        }

        /// <summary>Writes a 32-bit signed integer using optim encoding (see <see cref="StreamExtensions.WriteInt32Optim"/>) to the stream.</summary>
        public void WriteVarInt(int value)
        {
            _stream.WriteInt32Optim(value);
        }

        /// <summary>Reads a 32-bit unsigned integer using optim encoding (see <see cref="StreamExtensions.ReadUInt32Optim"/>) from the stream.</summary>
        public uint ReadVarUInt()
        {
            return _stream.ReadUInt32Optim();
        }

        /// <summary>Writes a 32-bit unsigned integer using optim encoding (see <see cref="StreamExtensions.WriteUInt32Optim"/>) to the stream.</summary>
        public void WriteVarUInt(uint value)
        {
            _stream.WriteUInt32Optim(value);
        }

        /// <summary>Reads a 64-bit signed integer using optim encoding (see <see cref="StreamExtensions.ReadInt64Optim"/>) from the stream.</summary>
        public long ReadVarLong()
        {
            return _stream.ReadInt64Optim();
        }

        /// <summary>Writes a 64-bit signed integer using optim encoding (see <see cref="StreamExtensions.WriteInt64Optim"/>) to the stream.</summary>
        public void WriteVarLong(long value)
        {
            _stream.WriteInt64Optim(value);
        }

        /// <summary>Reads a 64-bit unsigned integer using optim encoding (see <see cref="StreamExtensions.ReadUInt64Optim"/>) from the stream.</summary>
        public ulong ReadVarULong()
        {
            return _stream.ReadUInt64Optim();
        }

        /// <summary>Writes a 64-bit unsigned integer using optim encoding (see <see cref="StreamExtensions.WriteUInt64Optim"/>) to the stream.</summary>
        public void WriteVarULong(ulong value)
        {
            _stream.WriteUInt64Optim(value);
        }


        /// <summary>Reads a character from the stream.</summary>
        public char ReadChar()
        {
            return reader.ReadChar();
        }

        /// <summary>Writes a character to the stream.</summary>
        public void WriteChar(char value)
        {
            writer.Write(value);
        }

        /// <summary>Reads a string from the stream.</summary>
        public string ReadString()
        {
            return reader.ReadString();
        }

        /// <summary>Writes a string to the stream.</summary>
        public void WriteString(string value)
        {
            writer.Write(value);
        }

        /// <summary>Reads a DateTime from the stream.</summary>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadLong());
        }

        /// <summary>Writes a DateTime to the stream.</summary>
        public void WriteDateTime(DateTime value)
        {
            WriteLong(value.ToBinary());
        }

        /// <summary>Reads a TimeSpan from the stream.</summary>
        public TimeSpan ReadTimeSpan()
        {
            return TimeSpan.FromTicks(ReadLong());
        }

        /// <summary>Writes a TimeSpan to the stream.</summary>
        public void WriteTimeSpan(TimeSpan value)
        {
            WriteLong(value.Ticks);
        }

        /// <summary>Writes the data contained in the specified <see cref="MemoryStream"/> to the stream as a length-prefixed block of bytes.</summary>
        public void WriteMemoryStream(MemoryStream stream)
        {
            WriteVarUInt((uint) stream.Length);
            stream.WriteTo(_stream);
        }

        /// <summary>Reads a length-prefixed block of bytes from the stream (for example, one produced by <see cref="WriteMemoryStream"/>).</summary>
        public MemoryStream ReadMemoryStream()
        {
            var length = (int) ReadVarUInt();
            byte[] buf = new byte[length];
            int read = _stream.FillBuffer(buf, 0, length);
            if (read < length)
                throw new EndOfStreamException("Unexpected end of stream while reading a block of bytes.");
            return new MemoryStream(buf);
        }
    }
}
