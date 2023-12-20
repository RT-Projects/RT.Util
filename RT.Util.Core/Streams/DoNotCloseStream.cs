namespace RT.Util.Streams;

/// <summary>
///     Passes through every operation to the underlying stream. When disposed, leaves the underlying stream alone instead of
///     closing it. Intended for use with certain streams which do not have the "do not close the underlying stream" option
///     built-in, such as the CryptoStream for example.</summary>
public sealed class DoNotCloseStream : Stream
{
    private Stream _stream;

    /// <summary>Constructor.</summary>
    public DoNotCloseStream(Stream underlyingStream)
    {
        _stream = underlyingStream;
    }

    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanWrite { get { return _stream.CanWrite; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanRead { get { return _stream.CanRead; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanSeek { get { return _stream.CanSeek; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void Flush() { _stream.Flush(); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Length { get { return _stream.Length; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void SetLength(long value) { _stream.SetLength(value); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Position { get { return _stream.Position; } set { _stream.Position = value; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Seek(long offset, SeekOrigin origin) { return _stream.Seek(offset, origin); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override int Read(byte[] buffer, int offset, int count) { return _stream.Read(buffer, offset, count); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void Write(byte[] buffer, int offset, int count) { _stream.Write(buffer, offset, count); }
}
