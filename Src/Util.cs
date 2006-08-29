/// Utils.cs  -  utility functions and classes

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace RT.Util
{
    /// <summary>
    /// This class records the time when it was created and a time-out interval. It provides
    /// a method to check whether the time-out interval has elapsed and another one to check
    /// how much time is left until the interval elapses.
    /// </summary>
    public class TimeOut
    {
        /// Hidden from public use
        private TimeSpan Interval;
        private DateTime StartTime;
        private TimeOut() { }

        /// <summary>
        /// Constructs an instance of the time-out class starting immediately and timing out
        /// after the Interval has elapsed.
        /// </summary>
        public TimeOut(TimeSpan Interval)
        {
            this.Interval = Interval;
            this.StartTime = DateTime.Now;
        }

        /// <summary>
        /// Returns whether the time-out has occurred.
        /// </summary>
        public bool TimedOut
        {
            get { return TimeSpan.Compare(DateTime.Now - StartTime, Interval) >= 0; }
        }

        /// <summary>
        /// Returns how much time is left until the time-out.
        /// </summary>
        public TimeSpan TimeLeft
        {
            get
            {
                TimeSpan ts = (StartTime + Interval) - DateTime.Now;
                return ts < TimeSpan.Zero ? TimeSpan.Zero : ts;
            }
        }
    }

    /// <summary>
    /// SortedDictionaryDT is a special kind of SortedDictionary which is intended to keep
    /// track of events ordered by time. The only difference from the SortedDictionary is
    /// that an attempt to add an event with the same time as an existing entry does not
    /// fail - instead the next available time is used.
    /// </summary>
    /// <typeparam name="TV"></typeparam>
    public class SortedDictionaryDT<TV> : SortedDictionary<DateTime, TV>
    {
        /// <summary>
        /// Adds an element at the specified time. If an element with that time already
        /// exists, the next available time will be used, effectively adding Value just
        /// after the other items with the same time.
        /// </summary>
        public new void Add(DateTime Key, TV Value)
        {
            while (ContainsKey(Key))
                Key.AddTicks(1);
            base.Add(Key, Value);
        }
    }

    /// <summary>
    /// This class can be used to keep track of non-fatal problems which are not reported
    /// to the user directly but can be obtained by the developer.
    /// </summary>
    public static class Fault
    {
        /// <summary>
        /// Represents an entry in the set of recorded faults.
        /// </summary>
        public class FaultEntry
        {
            public DateTime Timestamp;
            public string Message;
            public string Filename;
            public string Method;
            public int LineNumber;
            public Thread Thread;

            public override string ToString()
            {
                return ToString("{time}: {file}[{line}] - fault in {func}. {msg}{thread}");
            }

            public string ToString(string fmt)
            {
                string s = fmt;
                s = s.Replace("{time}", Timestamp.ToShortTimeString());
                s = s.Replace("{file}", Filename);
                s = s.Replace("{line}", LineNumber.ToString());
                s = s.Replace("{func}", Method);
                s = s.Replace("{msg}", Message);
                if (Thread != null)
                    s = s.Replace("{thread}", "(thread:" + Thread.Name + ")");
                return s;
            }
        }

        /// <summary>
        /// The list of all fault entries
        /// 
        /// Multi-threading: if you access the Entries list directly, you should either
        /// ensure that no other thread calls AddMT, or lock the Entries list for the
        /// duration of the processing.
        /// </summary>
        public static List<FaultEntry> Entries = new List<FaultEntry>();

        /// <summary>
        /// Adds a message to the Fault list. The fault entry will contain the specified
        /// message as well as a timestamp and full information about the function
        /// calling Add, including file & line number.
        /// 
        /// Multi-threading: use AddMT instead.
        /// </summary>
        public static void Add(string Message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);
            
            // Create the new fault entry
            FaultEntry FE = new FaultEntry();
            FE.Timestamp = DateTime.Now;
            FE.Message = Message;
            FE.Filename = sf.GetFileName();
            FE.Method = sf.GetMethod().Name;
            FE.LineNumber = sf.GetFileLineNumber();
            FE.Thread = null;

            // Add to the list
            Entries.Add(FE);
        }

        /// <summary>
        /// Adds a message to the Fault list. This method can be safely called from
        /// multiple threads. This method will also store a reference to the thread
        /// which invoked it. See also information about Add.
        /// </summary>
        public static void AddMT(string Message)
        {
            // Skip the stack frame for the current function
            StackFrame sf = new StackFrame(1, true);

            // Create the new fault entry
            FaultEntry FE = new FaultEntry();
            FE.Timestamp = DateTime.Now;
            FE.Message = Message;
            FE.Filename = sf.GetFileName();
            FE.Method = sf.GetMethod().Name;
            FE.LineNumber = sf.GetFileLineNumber();
            FE.Thread = Thread.CurrentThread;

            // Add to the list
            lock (Entries)
                Entries.Add(FE);
        }
    }

    /// <summary>
    /// A pair of two values of specified types.
    /// </summary>
    public struct Pair<T1, T2>
    {
        public T1 E1;
        public T2 E2;

        public Pair(T1 Element1, T2 Element2)
        {
            E1 = Element1;
            E2 = Element2;
        }
    }

    /// <summary>
    /// A class which can enumerate all pairs of items in an IList.
    /// 
    /// Usage example: foreach (Pair<T,T> p in new EnumPairs(TheList)) {...}
    /// </summary>
    /// <typeparam name="T">The type of an item in the IList</typeparam>
    public class EnumPairs<T>
    {
        private IList<T> A;

        private EnumPairs() {}

        public EnumPairs(IList<T> List)
        {
            A = List;
        }

        public IEnumerator<Pair<T, T>> GetEnumerator()
        {
            for (int i=0; i<A.Count-1; i++)
                for (int j=i+1; j<A.Count; j++)
                    yield return new Pair<T, T>(A[i], A[j]);
        }
    }

    /// <summary>
    /// Calculates CRC32 checksum of all values that are read/written via this stream.
    /// Seeking is ignored. All the bytes seeked over will be ignored.
    /// </summary>
    public class CRC32Stream : Stream
    {
        private static uint[] poly = new uint[]
        {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172, 0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924, 0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0, 0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236, 0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94, 0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
        };

        private uint crc;

        private Stream stream = null;

        public virtual Stream BaseStream { get { return stream; } }

        private CRC32Stream() { }

        public CRC32Stream(Stream stream)
        {
            this.stream = stream;
            crc = 0xFFFFFFFF;
        }

        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get
            {
                return stream.Position;
            }
            set
            {
                stream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int numread = stream.Read(buffer, offset, count);

            for (int i=offset; i<offset+count; i++)
                crc = poly[(crc ^ (buffer[i])) & 0xFF] ^ (crc >> 8);

            return numread;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);

            for (int i=offset; i<offset+count; i++)
                crc = poly[(crc ^ (buffer[i])) & 0xFF] ^ (crc >> 8);
        }

        public uint CRC
        {
            get
            {
                return crc ^ 0xFFFFFFFF;
            }
        }
    }

    /// <summary>
    /// Helper class for GDI operations
    /// </summary>
    public static class GDI
    {
        /// <summary>
        /// Caches previously used Pens of predefined width and the specified color.
        /// </summary>
        private static Dictionary<Color, Pen> PenCache = new Dictionary<Color, Pen>();

        /// <summary>
        /// Caches previously used Solid Brushes of the specified color.
        /// </summary>
        private static Dictionary<Color, Brush> BrushCache = new Dictionary<Color, Brush>();

        /// <summary>
        /// Returns a pen of the specified color. The pen will be retrieved from the cache
        /// in case it exists; otherwise it will be created and cached.
        /// </summary>
        /// <param name="clr">Color of the pen to be retrieved</param>
        public static Pen GetPen(Color clr)
        {
            if (PenCache.ContainsKey(clr))
                return PenCache[clr];

            Pen p = new Pen(clr, 3);
            PenCache[clr] = p;
            return p;
        }

        /// <summary>
        /// Returns a brush of the specified color. The brush will be retrieved from the cache
        /// in case it exists; otherwise it will be created and cached.
        /// </summary>
        /// <param name="clr">Color of the brush to be retrieved</param>
        public static Brush GetBrush(Color clr)
        {
            if (BrushCache.ContainsKey(clr))
                return BrushCache[clr];

            SolidBrush b = new SolidBrush(clr);
            BrushCache[clr] = b;
            return b;
        }
    }

    /// <summary>
    /// WinAPI function wrappers
    /// </summary>
    public static class WinAPI
    {
        static WinAPI()
        {
            QueryPerformanceFrequency(out PerformanceFreq);
        }

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        public static readonly long PerformanceFreq;
    }

    /// <summary>
    /// A double-precision rectangle class which supports intersect tests.
    /// </summary>
    public struct RectangleD
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public RectangleD(double X, double Y, double Width, double Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public double Left { get { return X; } }
        public double Top { get { return Y; } }
        public double Right { get { return X + Width; } }
        public double Bottom { get { return Y + Height; } }

        public bool IntersectsWith(RectangleD r)
        {
            return ContainsPoint(r.Left,  r.Top) || ContainsPoint(r.Left,  r.Bottom)
                || ContainsPoint(r.Right, r.Top) || ContainsPoint(r.Right, r.Bottom)
                || (r.Left >= Left && r.Right <= Right && r.Top <= Top && r.Bottom >= Bottom)
                || (r.Left <= Left && r.Right >= Right && r.Top >= Top && r.Bottom <= Bottom);
        }

        public bool ContainsPoint(double X, double Y)
        {
            return (X >= Left) && (X <= Right) && (Y >= Top) && (Y <= Bottom);
        }
    }
}
