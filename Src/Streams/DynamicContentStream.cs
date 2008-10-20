using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    /// <summary>
    /// Provides a read-only stream that can "read from" an <c>IEnumerable&lt;string&gt;</c>.
    /// In particular, an intended application is to "read from" a method that uses <c>yield return</c> to return strings as execution proceeds.
    /// This enables generation of, for example, HTML for dynamic web pages.
    /// </summary>
    public class DynamicContentStream : Stream
    {
        private IEnumerator<string> Enumerator = null;
        private byte[] LastUnprocessedBytes = null;
        private int LastUnprocessedBytesIndex = 0;

        /// <summary>
        /// Instantiates a buffered <see cref="DynamicContentStream"/>.
        /// </summary>
        /// <param name="Enumerable">The object that provides the content for this stream to read from.</param>
        public DynamicContentStream(IEnumerable<string> Enumerable)
        {
            this.Enumerator = Enumerable.GetEnumerator();
            this.Buffered = true;
        }

        /// <summary>
        /// Instantiates a <see cref="DynamicContentStream"/> and lets you configure whether it's buffered or not.
        /// </summary>
        /// <param name="Enumerable">The object that provides the content for this stream to read from.</param>
        /// <param name="Buffered">Provides an initial value for the <see cref="Buffered"/> property.</param>
        public DynamicContentStream(IEnumerable<string> Enumerable, bool Buffered)
        {
            this.Enumerator = Enumerable.GetEnumerator();
            this.Buffered = Buffered;
        }

        /// <summary>
        /// Instantiates a buffered <see cref="DynamicContentStream"/>.
        /// </summary>
        /// <param name="Enumerator">The object that provides the content for this stream to read from.</param>
        public DynamicContentStream(IEnumerator<string> Enumerator)
        {
            this.Enumerator = Enumerator;
        }

        /// <summary>
        /// Instantiates a <see cref="DynamicContentStream"/> and lets you configure whether it's buffered or not.
        /// </summary>
        /// <param name="Enumerator">The object that provides the content for this stream to read from.</param>
        /// <param name="Buffered">Provides an initial value for the <see cref="Buffered"/> property.</param>
        public DynamicContentStream(IEnumerator<string> Enumerator, bool Buffered)
        {
            this.Enumerator = Enumerator;
            this.Buffered = Buffered;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush() { }

        /// <summary>
        /// If true, each call to <see cref="Read"/> will move the enumerator forward as far as necessary to fill the buffer.
        /// If false, each call to <see cref="Read"/> returns only the text produced by a single MoveNext() of the enumerator.
        /// </summary>
        public bool Buffered { get; set; }

        /// <summary>
        /// Reads some text into the specified buffer. The behaviour depends on the <see cref="Buffered"/> property.
        /// The bytes returned respresent the text returned by the underlying enumerator, UTF-8-encoded.
        /// Although DynamicContentStream makes no effort to keep multi-byte characters within a single invocation
        /// of Read(), all output will be valid UTF-8 when concatenated.
        /// </summary>
        /// <param name="buffer">The buffer to copy the data into.</param>
        /// <param name="offset">The offset at which to start copying into buffer.</param>
        /// <param name="count">The maximum number of bytes to copy. The stream may return less than this.</param>
        /// <returns>The number of bytes actually copied into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (LastUnprocessedBytes != null && LastUnprocessedBytes.Length > 0)
            {
                if (LastUnprocessedBytes.Length - LastUnprocessedBytesIndex > count)
                {
                    Array.Copy(LastUnprocessedBytes, LastUnprocessedBytesIndex, buffer, offset, count);
                    LastUnprocessedBytesIndex += count;
                    return count;
                }
                else
                {
                    int HowMany = LastUnprocessedBytes.Length - LastUnprocessedBytesIndex;
                    Array.Copy(LastUnprocessedBytes, LastUnprocessedBytesIndex, buffer, offset, HowMany);
                    LastUnprocessedBytes = null;
                    LastUnprocessedBytesIndex = 0;
                    return HowMany;
                }
            }

            if (Buffered)
            {
                StringBuilder b = new StringBuilder();
                long BytesSoFar = 0;
                while (BytesSoFar < count)
                {
                    if (!Enumerator.MoveNext())
                        break;
                    b.Append(Enumerator.Current);
                    BytesSoFar += Enumerator.Current.UTF8Length();
                }
                if (b.Length == 0)
                    return 0;

                byte[] BigBuffer = b.ToString().ToUTF8();
                if (BigBuffer.Length > count)
                {
                    Array.Copy(BigBuffer, 0, buffer, offset, count);
                    LastUnprocessedBytes = BigBuffer;
                    LastUnprocessedBytesIndex = count;
                    return count;
                }
                else
                {
                    Array.Copy(BigBuffer, 0, buffer, offset, BigBuffer.Length);
                    LastUnprocessedBytes = null;
                    return BigBuffer.Length;
                }
            }
            else
            {
                do
                {
                    if (!Enumerator.MoveNext())
                        return 0;
                } while (Enumerator.Current.Length == 0);
                byte[] Encoded = Enumerator.Current.ToUTF8();
                if (Encoded.Length > count)
                {
                    Array.Copy(Encoded, 0, buffer, 0, count);
                    LastUnprocessedBytes = Encoded;
                    LastUnprocessedBytesIndex = count;
                    return count;
                }
                else
                {
                    Array.Copy(Encoded, 0, buffer, 0, Encoded.Length);
                    return Encoded.Length;
                }
            }
        }

        // Things you can't do
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
