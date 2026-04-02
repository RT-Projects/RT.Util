namespace RT.Util.Streams;

/// <summary>
///     Passes through every operation to the underlying stream. When disposed, leaves the underlying stream alone instead of
///     closing it. Intended for use with certain streams which do not have the "do not close the underlying stream" option
///     built-in, such as the CryptoStream for example.</summary>
public sealed class DoNotCloseStream(Stream underlyingStream) : Stream
{
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanWrite { get { return underlyingStream.CanWrite; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanRead { get { return underlyingStream.CanRead; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override bool CanSeek { get { return underlyingStream.CanSeek; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void Flush() { underlyingStream.Flush(); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Length { get { return underlyingStream.Length; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void SetLength(long value) { underlyingStream.SetLength(value); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Position { get { return underlyingStream.Position; } set { underlyingStream.Position = value; } }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override long Seek(long offset, SeekOrigin origin) { return underlyingStream.Seek(offset, origin); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override int Read(byte[] buffer, int offset, int count) { return underlyingStream.Read(buffer, offset, count); }
    /// <summary>Passes through this operation to the underlying stream.</summary>
    public override void Write(byte[] buffer, int offset, int count) { underlyingStream.Write(buffer, offset, count); }
}
