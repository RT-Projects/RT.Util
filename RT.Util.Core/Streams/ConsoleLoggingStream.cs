using RT.Util.ExtensionMethods;

#pragma warning disable 1591  // XML comment

namespace RT.Util.Streams;

/// <summary>
///     Implements a stream that passes through all operations to the underlying stream, but also prints the bytes read or
///     written to the console.</summary>
/// <param name="underlyingStream">
///     The underlying stream on which all operations are to be performed.</param>
/// <param name="readPrefix">
///     If not null, all calls to <see cref="Read"/> will print a single line to the console starting with this prefix and
///     showing a hex dump of the bytes read.</param>
/// <param name="writePrefix">
///     If not null, all calls to <see cref="Read"/> will print a single line to the console starting with this prefix and
///     showing a hex dump of the bytes written.</param>
public class ConsoleLoggingStream(Stream underlyingStream, string readPrefix, string writePrefix) : Stream
{
    public override bool CanRead => underlyingStream.CanRead;
    public override bool CanSeek => underlyingStream.CanSeek;
    public override bool CanWrite => underlyingStream.CanWrite;
    public override void Flush() { underlyingStream.Flush(); }
    public override long Length => underlyingStream.Length;

    public override long Position
    {
        get { return underlyingStream.Position; }
        set { underlyingStream.Position = value; }
    }

    public override long Seek(long offset, SeekOrigin origin) => underlyingStream.Seek(offset, origin);
    public override void SetLength(long value) { underlyingStream.SetLength(value); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = underlyingStream.Read(buffer, offset, count);
        if (readPrefix != null)
        {
            var toprint = buffer.Subarray(offset, read);
            Console.WriteLine(readPrefix + toprint.Length + " bytes: " + toprint.ToHex());
        }
        return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        underlyingStream.Write(buffer, offset, count);
        if (writePrefix != null)
        {
            var toprint = buffer.Subarray(offset, count);
            Console.WriteLine(writePrefix + toprint.Length + " bytes: " + toprint.ToHex());
        }
    }
}
