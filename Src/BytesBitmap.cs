using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RT.Util
{
    public class BytesBitmap : IDisposable
    {
        private SharedPinnedByteArray FBytes;
        private Bitmap FBitmap;
        private int FStride;
        private int FPixelFormatSize;

        /// <summary>
        /// Gets an array that contains the bitmap bit buffer.
        /// </summary>
        public byte[] Bits
        {
            get { return FBytes.Bits; }
        }

        /// <summary>
        /// Gets the underlying Bitmap that this BytesBitmap wraps.
        /// </summary>
        public Bitmap Bitmap
        {
            get { return FBitmap; }
            set { FBitmap = value; }
        }

        /// <summary>
        /// Gets the stride (the number of bytes to go one pixel down) of the bitmap.
        /// </summary>
        public int Stride
        {
            get { return FStride; }
        }

        /// <summary>
        /// Gets the number of bits needed to store a pixel.
        /// </summary>
        public int PixelFormatSize
        {
            get { return FPixelFormatSize; }
        }

        /// <summary>
        /// Gets a safe pointer to the buffer containing the bitmap bits.
        /// </summary>
        public IntPtr BitPtr
        {
            get { return FBytes.BitPtr; }
        }

        /// <summary>
        /// Creates a new, blank BytesBitmap with the specified width, height, and pixel format.
        /// </summary>
        public BytesBitmap(int width, int height, PixelFormat format)
        {
            FPixelFormatSize = Image.GetPixelFormatSize(format);
            FStride = width * FPixelFormatSize / 8;
            int padding = FStride % 4;
            FStride += (padding == 0) ? 0 : 4 - padding;
            FBytes = new SharedPinnedByteArray(FStride * height);
            FBitmap = new Bitmap(width, height, FStride, format, FBytes.BitPtr);
        }

        #region Dispose stuff

        private bool FDisposed;

        public bool Disposed
        {
            get { return FDisposed; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (FDisposed)
                return;

            FBitmap.Dispose();
            FBytes.ReleaseReference();
            FDisposed = true;

            if (disposing)
            {
                FBitmap=null;
            }
        }

        ~BytesBitmap()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// This class represents a byte array which is pinned to avoid relocation
    /// by the GC and implements reference counting.
    /// </summary>
    internal class SharedPinnedByteArray
    {
        internal byte[] Bits;
        internal GCHandle Handle;
        internal IntPtr BitPtr;

        int RefCount;

        public SharedPinnedByteArray(int length)
        {
            Bits = new byte[length];
            Handle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            BitPtr = Marshal.UnsafeAddrOfPinnedArrayElement(Bits, 0);
            RefCount++;
        }

        internal void AddReference()
        {
            RefCount++;
        }

        internal void ReleaseReference()
        {
            RefCount--;
            if (RefCount<=0)
                Destroy();
        }

        bool FDestroyed;
        private void Destroy()
        {
            if (!FDestroyed)
            {
                Handle.Free();
                Bits=null;
                FDestroyed=true;
            }
        }

        ~SharedPinnedByteArray()
        {
            Destroy();
        }
    }
}
