using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RT.Util.Drawing
{
    /// <summary>
    /// Wrapper around a Bitmap that allows access to its raw byte data.
    /// </summary>
    public class BytesBitmap : IDisposable
    {
        private SharedPinnedByteArray _bytes;
        private Bitmap _bitmap;
        private int _stride;
        private int _pixelFormatSize;

        /// <summary>
        /// Gets an array that contains the bitmap bit buffer.
        /// </summary>
        public byte[] Bits
        {
            get { return _bytes._bits; }
        }

        /// <summary>
        /// Gets the underlying Bitmap that this BytesBitmap wraps.
        /// </summary>
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        /// <summary>
        /// Gets the stride (the number of bytes to go one pixel down) of the bitmap.
        /// </summary>
        public int Stride
        {
            get { return _stride; }
        }

        /// <summary>
        /// Gets the number of bits needed to store a pixel.
        /// </summary>
        public int PixelFormatSize
        {
            get { return _pixelFormatSize; }
        }

        /// <summary>
        /// Gets a safe pointer to the buffer containing the bitmap bits.
        /// </summary>
        public IntPtr BitPtr
        {
            get { return _bytes._bitPtr; }
        }

        /// <summary>
        /// Creates a new, blank BytesBitmap with the specified width, height, and pixel format.
        /// </summary>
        public BytesBitmap(int width, int height, PixelFormat format)
        {
            _pixelFormatSize = Image.GetPixelFormatSize(format);
            _stride = width * _pixelFormatSize / 8;
            int padding = _stride % 4;
            _stride += (padding == 0) ? 0 : 4 - padding;
            _bytes = new SharedPinnedByteArray(_stride * height);
            _bitmap = new Bitmap(width, height, _stride, format, _bytes._bitPtr);
        }

        #region Dispose stuff

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        private bool _disposed;

        public bool Disposed
        {
            get { return _disposed; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _bitmap.Dispose();
            _bytes.releaseReference();
            _disposed = true;

            if (disposing)
            {
                _bitmap = null;
            }
        }

        ~BytesBitmap()
        {
            Dispose(false);
        }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        #endregion
    }

    /// <summary>
    /// This class represents a byte array which is pinned to avoid relocation
    /// by the GC and implements reference counting.
    /// </summary>
    internal class SharedPinnedByteArray
    {
        internal byte[] _bits;
        internal GCHandle _handle;
        internal IntPtr _bitPtr;

        private int _refCount;
        private bool _destroyed;

        public SharedPinnedByteArray(int length)
        {
            _bits = new byte[length];
            _handle = GCHandle.Alloc(_bits, GCHandleType.Pinned);
            _bitPtr = Marshal.UnsafeAddrOfPinnedArrayElement(_bits, 0);
            _refCount++;
        }

        internal void addReference()
        {
            _refCount++;
        }

        internal void releaseReference()
        {
            _refCount--;
            if (_refCount <= 0)
                destroy();
        }

        private void destroy()
        {
            if (!_destroyed)
            {
                _handle.Free();
                _bits = null;
                _destroyed = true;
            }
        }

        ~SharedPinnedByteArray()
        {
            destroy();
        }
    }
}
