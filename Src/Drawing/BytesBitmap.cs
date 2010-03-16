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
            get { return _bytes.Bytes; }
        }

        /// <summary>
        /// Gets the underlying Bitmap that this BytesBitmap wraps. USAGE WARNING:
        /// DO NOT use this if the BytesBitmap wrapping it may have gone out of context
        /// and disposed of. This will cause intermittent issues - when the BytesBitmap
        /// gets GC'd. Use <see cref="GetBitmapCopy"/> instead.
        /// </summary>
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        /// <summary>
        /// Use this to create a new Bitmap that is a copy of the image stored in this
        /// BytesBitmap. This can be passed around safely, unlike the wrapped bitmap
        /// returned by <see cref="Bitmap"/>.
        /// </summary>
        public Bitmap GetBitmapCopy()
        {
            Bitmap bmp = new Bitmap(_bitmap);
            Graphics gr = Graphics.FromImage(bmp);
            gr.DrawImageUnscaled(_bitmap, 0, 0);
            return bmp;
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
            get { return _bytes.Address; }
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
            _bitmap = new Bitmap(width, height, _stride, format, _bytes.Address);
        }

        #region Dispose stuff

        private bool _disposed;

        /// <summary>Specifies whether the underlying resources for this <see cref="BytesBitmap"/> have already been disposed.</summary>
        public bool Disposed
        {
            get { return _disposed; }
        }

        /// <summary>Disposes the underlying resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>Disposes the underlying resources.</summary>
        /// <param name="disposing">True if called from <see cref="Dispose()"/>; false if called from the destructor.</param>
        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _bitmap.Dispose();
            _bytes.ReleaseReference();
            _disposed = true;

            if (disposing)
            {
                _bitmap = null;
            }
        }

        /// <summary>Destructor.</summary>
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
        private GCHandle _handle;
        private int _refCount;
        private bool _destroyed;

        /// <summary>
        /// Gets the allocated byte array. This can be modified as desired.
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Gets an unmanaged address of the first (index 0) byte of the byte array.
        /// </summary>
        public IntPtr Address { get; private set; }

        /// <summary>
        /// Returns an unmanaged address of the specified byte in the byte array.
        /// </summary>
        public IntPtr AddressOf(int index)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(Bytes, index);
        }

        /// <summary>
        /// Creates a new pinned array of the specified size, that can be accessed through <see cref="Bytes"/>.
        /// One reference is automatically added; call <see cref="ReleaseReference"/> when finished using this array.
        /// </summary>
        /// <param name="length">The number of bytes that the pinned array should contain</param>
        public SharedPinnedByteArray(int length)
        {
            Bytes = new byte[length];
            _handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Address = Marshal.UnsafeAddrOfPinnedArrayElement(Bytes, 0);
            _refCount++;
        }

        /// <summary>
        /// Adds a reference to this array. One reference is counted when the array is created. It is deleted when
        /// all references are released using <see cref="ReleaseReference"/>.
        /// </summary>
        public void AddReference()
        {
            _refCount++;
        }

        /// <summary>
        /// Releases a reference to this array. When there are none left, the array is unpinned and can get garbage-collected.
        /// </summary>
        public void ReleaseReference()
        {
            _refCount--;
            if (_refCount <= 0)
                destroy();
        }

        /// <summary>Gets the length of the byte array.</summary>
        public int Length { get { return Bytes.Length; } }

        private void destroy()
        {
            if (!_destroyed)
            {
                _handle.Free();
                Bytes = null;
                _destroyed = true;
            }
        }

        ~SharedPinnedByteArray()
        {
            destroy();
        }
    }
}
