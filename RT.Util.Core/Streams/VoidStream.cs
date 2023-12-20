namespace RT.Util.Streams;

/// <summary>
///     A stream that discards everything that goes in; all reads result in 0's. Can be used as the underlying stream for
///     things like <see cref="CRC32Stream"/>. Pretends to be a zero-length stream that can swallow writes and length changes.</summary>
public sealed class VoidStream : Stream
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
    public override bool CanRead { get { return true; } }
    public override bool CanSeek { get { return true; } }
    public override bool CanWrite { get { return true; } }
    public override void Flush() { }
    public override long Length { get { return 0; } }
    public override long Position { get { return 0; } set { } }
    public override void SetLength(long value) { }
    public override long Seek(long offset, SeekOrigin origin) { return 0; }
    public override int Read(byte[] buffer, int offset, int count) { return 0; }
    public override void Write(byte[] buffer, int offset, int count) { }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
}
