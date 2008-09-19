using System;
using System.IO;

namespace RT.Util.Streams
{
    public class SlowStream : Stream
    {
        public static int ChunkSize = 20;

        private Stream MyStream;
        public SlowStream(Stream MyStream) { this.MyStream = MyStream; }

        public override bool CanRead
        {
            get { return MyStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return MyStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return MyStream.CanWrite; }
        }

        public override void Flush()
        {
            MyStream.Flush();
        }

        public override long Length
        {
            get { return MyStream.Length; }
        }

        public override long Position
        {
            get
            {
                return MyStream.Position;
            }
            set
            {
                MyStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return MyStream.Read(buffer, offset, Math.Min(count, ChunkSize));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return MyStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            MyStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            MyStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            MyStream.Close();
        }
    }
}
