/*
    The MIT License (MIT)
    
    Copyright (c) 2014 Luiz Fernando Silva
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FastBitmap
{
    /// <summary>
    /// Encapsulates a Bitmap for fast bitmap pixel operations using 32bpp images
    /// </summary>
    public unsafe class FastBitmap : IDisposable
    {
        /// <summary>
        /// Specifies the number of bytes available per pixel of the bitmap object being manipulated
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// The Bitmap object encapsulated on this FastBitmap
        /// </summary>
        private readonly Bitmap _bitmap;

        /// <summary>
        /// The BitmapData resulted from the lock operation
        /// </summary>
        private BitmapData _bitmapData;

        /// <summary>
        /// The stride of the bitmap
        /// </summary>
        private int _strideWidth;

        /// <summary>
        /// The first pixel of the bitmap
        /// </summary>
        private int *_scan0;

        /// <summary>
        /// Whether the current bitmap is locked
        /// </summary>
        private bool _locked;

        /// <summary>
        /// The width of this FastBitmap
        /// </summary>
        private readonly int _width;

        /// <summary>
        /// The height of this FastBitmap
        /// </summary>
        private readonly int _height;

        /// <summary>
        /// Gets the width of this FastBitmap object
        /// </summary>
        public int Width { get { return _width; } }

        /// <summary>
        /// Gets the height of this FastBitmap object
        /// </summary>
        public int Height { get { return _height; } }

        /// <summary>
        /// Gets the pointer to the first pixel of the bitmap
        /// </summary>
        public IntPtr Scan0 { get { return _bitmapData.Scan0; } }

        /// <summary>
        /// Gets the stride width of the bitmap
        /// </summary>
        public int Stride { get { return _strideWidth; } }

        /// <summary>
        /// Gets a boolean value that states whether this FastBitmap is currently locked in memory
        /// </summary>
        public bool Locked { get { return _locked; } }

        /// <summary>
        /// Gets an array of 32-bit color pixel values that represent this FastBitmap
        /// </summary>
        /// <exception cref="Exception">The locking operation required to extract the values off from the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap is already locked outside this fast bitmap</exception>
        public int[] DataArray
        {
            get
            {
                bool unlockAfter = false;
                if (!_locked)
                {
                    Lock();
                    unlockAfter = true;
                }

                // Declare an array to hold the bytes of the bitmap
                int bytes = Math.Abs(_bitmapData.Stride) * _bitmap.Height;
                int[] argbValues = new int[bytes / BytesPerPixel];

                // Copy the RGB values into the array
                Marshal.Copy(_bitmapData.Scan0, argbValues, 0, bytes / BytesPerPixel);

                if (unlockAfter)
                {
                    Unlock();
                }

                return argbValues;
            }
        }

        /// <summary>
        /// Creates a new instance of the FastBitmap class with a specified Bitmap.
        /// The bitmap provided must have a 32bpp depth
        /// </summary>
        /// <param name="bitmap">The Bitmap object to encapsulate on this FastBitmap object</param>
        /// <exception cref="ArgumentException">The bitmap provided does not have a 32bpp pixel format</exception>
        public FastBitmap(Bitmap bitmap)
        {
            if (Image.GetPixelFormatSize(bitmap.PixelFormat) != 32)
            {
                throw new ArgumentException("The provided bitmap must have a 32bpp depth", "bitmap");
            }

            _bitmap = bitmap;

            _width = bitmap.Width;
            _height = bitmap.Height;
        }

        /// <summary>
        /// Disposes of this fast bitmap object and releases any pending resources.
        /// The underlying bitmap is not disposes, and is unlocked, if currently locked
        /// </summary>
        public void Dispose()
        {
            if (_locked)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Locks the bitmap to start the bitmap operations. If the bitmap is already locked,
        /// an exception is thrown
        /// </summary>
        /// <returns>A fast bitmap locked struct that will unlock the underlying bitmap after disposal</returns>
        /// <exception cref="InvalidOperationException">The bitmap is already locked</exception>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap is already locked outside this fast bitmap</exception>
        public FastBitmapLocker Lock()
        {
            if (_locked)
            {
                throw new InvalidOperationException("Unlock must be called before a Lock operation");
            }

            return Lock(ImageLockMode.ReadWrite);
        }

        /// <summary>
        /// Locks the bitmap to start the bitmap operations
        /// </summary>
        /// <param name="lockMode">The lock mode to use on the bitmap</param>
        /// <returns>A fast bitmap locked struct that will unlock the underlying bitmap after disposal</returns>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap is already locked outside this fast bitmap</exception>
        private FastBitmapLocker Lock(ImageLockMode lockMode)
        {
            Rectangle rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);

            return Lock(lockMode, rect);
        }

        /// <summary>
        /// Locks the bitmap to start the bitmap operations
        /// </summary>
        /// <param name="lockMode">The lock mode to use on the bitmap</param>
        /// <param name="rect">The rectangle to lock</param>
        /// <returns>A fast bitmap locked struct that will unlock the underlying bitmap after disposal</returns>
        /// <exception cref="System.ArgumentException">The provided region is invalid</exception>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap region is already locked</exception>
        private FastBitmapLocker Lock(ImageLockMode lockMode, Rectangle rect)
        {
            // Lock the bitmap's bits
            _bitmapData = _bitmap.LockBits(rect, lockMode, _bitmap.PixelFormat);

            _scan0 = (int*)_bitmapData.Scan0;
            _strideWidth = _bitmapData.Stride / BytesPerPixel;

            _locked = true;

            return new FastBitmapLocker(this);
        }

        /// <summary>
        /// Unlocks the bitmap and applies the changes made to it. If the bitmap was not locked
        /// beforehand, an exception is thrown
        /// </summary>
        /// <exception cref="InvalidOperationException">The bitmap is already unlocked</exception>
        /// <exception cref="System.Exception">The unlocking operation in the underlying bitmap failed</exception>
        public void Unlock()
        {
            if (!_locked)
            {
                throw new InvalidOperationException("Lock must be called before an Unlock operation");
            }

            _bitmap.UnlockBits(_bitmapData);

            _locked = false;
        }

        /// <summary>
        /// Sets the pixel color at the given coordinates. If the bitmap was not locked beforehands,
        /// an exception is thrown
        /// </summary>
        /// <param name="x">The X coordinate of the pixel to set</param>
        /// <param name="y">The Y coordinate of the pixel to set</param>
        /// <param name="color">The new color of the pixel to set</param>
        /// <exception cref="InvalidOperationException">The fast bitmap is not locked</exception>
        /// <exception cref="ArgumentException">The provided coordinates are out of bounds of the bitmap</exception>
        public void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, color.ToArgb());
        }

        /// <summary>
        /// Sets the pixel color at the given coordinates. If the bitmap was not locked beforehands,
        /// an exception is thrown
        /// </summary>
        /// <param name="x">The X coordinate of the pixel to set</param>
        /// <param name="y">The Y coordinate of the pixel to set</param>
        /// <param name="color">The new color of the pixel to set</param>
        /// <exception cref="InvalidOperationException">The fast bitmap is not locked</exception>
        /// <exception cref="ArgumentException">The provided coordinates are out of bounds of the bitmap</exception>
        public void SetPixel(int x, int y, int color)
        {
            SetPixel(x, y, (uint)color);
        }

        /// <summary>
        /// Sets the pixel color at the given coordinates. If the bitmap was not locked beforehands,
        /// an exception is thrown
        /// </summary>
        /// <param name="x">The X coordinate of the pixel to set</param>
        /// <param name="y">The Y coordinate of the pixel to set</param>
        /// <param name="color">The new color of the pixel to set</param>
        /// <exception cref="InvalidOperationException">The fast bitmap is not locked</exception>
        /// <exception cref="ArgumentException">The provided coordinates are out of bounds of the bitmap</exception>
        public void SetPixel(int x, int y, uint color)
        {
            if (!_locked)
            {
                throw new InvalidOperationException("The FastBitmap must be locked before any pixel operations are made");
            }

            if (x < 0 || x >= _width)
            {
                throw new ArgumentException("The X component must be >= 0 and < width");
            }
            if (y < 0 || y >= _height)
            {
                throw new ArgumentException("The Y component must be >= 0 and < height");
            }

            *(uint*)(_scan0 + x + y * _strideWidth) = color;
        }

        /// <summary>
        /// Gets the pixel color at the given coordinates. If the bitmap was not locked beforehands,
        /// an exception is thrown
        /// </summary>
        /// <param name="x">The X coordinate of the pixel to get</param>
        /// <param name="y">The Y coordinate of the pixel to get</param>
        /// <exception cref="InvalidOperationException">The fast bitmap is not locked</exception>
        /// <exception cref="ArgumentException">The provided coordinates are out of bounds of the bitmap</exception>
        public Color GetPixel(int x, int y)
        {
            return Color.FromArgb(GetPixelInt(x, y));
        }

        /// <summary>
        /// Gets the pixel color at the given coordinates as an integer value. If the bitmap
        /// was not locked beforehands, an exception is thrown
        /// </summary>
        /// <param name="x">The X coordinate of the pixel to get</param>
        /// <param name="y">The Y coordinate of the pixel to get</param>
        /// <exception cref="InvalidOperationException">The fast bitmap is not locked</exception>
        /// <exception cref="ArgumentException">The provided coordinates are out of bounds of the bitmap</exception>
        public int GetPixelInt(int x, int y)
        {
            if (!_locked)
            {
                throw new InvalidOperationException("The FastBitmap must be locked before any pixel operations are made");
            }

            if (x < 0 || x >= _width)
            {
                throw new ArgumentException("The X component must be >= 0 and < width");
            }
            if (y < 0 || y >= _height)
            {
                throw new ArgumentException("The Y component must be >= 0 and < height");
            }

            return *(_scan0 + x + y * _strideWidth);
        }

        /// <summary>
        /// Clears the bitmap with the given color
        /// </summary>
        /// <param name="color">The color to clear the bitmap with</param>
        public void Clear(Color color)
        {
            Clear(color.ToArgb());
        }

        /// <summary>
        /// Clears the bitmap with the given color
        /// </summary>
        /// <param name="color">The color to clear the bitmap with</param>
        public void Clear(int color)
        {
            bool unlockAfter = false;
            if(!_locked)
            {
                Lock();
                unlockAfter = true;
            }

            // Clear all the pixels
            int count = _width * _height;
            int* curScan = _scan0;

            // Defines the ammount of assignments that the main while() loop is performing per loop.
            // The value specified here must match the number of assignment statements inside that loop
            const int assignsPerLoop = 8;

            int rem = count % assignsPerLoop;
            count /= assignsPerLoop;

            while (count-- > 0)
            {
                *(curScan++) = color;
                *(curScan++) = color;
                *(curScan++) = color;
                *(curScan++) = color;

                *(curScan++) = color;
                *(curScan++) = color;
                *(curScan++) = color;
                *(curScan++) = color;
            }
            while (rem-- > 0)
            {
                *(curScan++) = color;
            }

            if (unlockAfter)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Copies a region of the source bitmap into this fast bitmap
        /// </summary>
        /// <param name="source">The source image to copy</param>
        /// <param name="srcRect">The region on the source bitmap that will be copied over</param>
        /// <param name="destRect">The region on this fast bitmap that will be changed</param>
        /// <exception cref="ArgumentException">The provided source bitmap is the same bitmap locked in this FastBitmap</exception>
        public void CopyRegion(Bitmap source, Rectangle srcRect, Rectangle destRect)
        {
            // Throw exception when trying to copy same bitmap over
            if (source == _bitmap)
            {
                throw new ArgumentException("Copying regions across the same bitmap is not supported", "source");
            }

            Rectangle srcBitmapRect = new Rectangle(0, 0, source.Width, source.Height);
            Rectangle destBitmapRect = new Rectangle(0, 0, _width, _height);

            // Check if the rectangle configuration doesn't generate invalid states or does not affect the target image
            if (srcRect.Width <= 0 || srcRect.Height <= 0 || destRect.Width <= 0 || destRect.Height <= 0 ||
                !srcBitmapRect.IntersectsWith(srcRect) || !destRect.IntersectsWith(destBitmapRect))
                return;

            // Find the areas of the first and second bitmaps that are going to be affected
            srcBitmapRect = Rectangle.Intersect(srcRect, srcBitmapRect);

            // Clip the source rectangle on top of the destination rectangle in a way that clips out the regions of the original bitmap
            // that will not be drawn on the destination bitmap for being out of bounds
            srcBitmapRect = Rectangle.Intersect(srcBitmapRect, new Rectangle(srcRect.X, srcRect.Y, destRect.Width, destRect.Height));

            destBitmapRect = Rectangle.Intersect(destRect, destBitmapRect);

            // Clipt the source bitmap region yet again here
            srcBitmapRect = Rectangle.Intersect(srcBitmapRect, new Rectangle(-destRect.X + srcRect.X, -destRect.Y + srcRect.Y, _width, _height));

            // Calculate the rectangle containing the maximum possible area that is supposed to be affected by the copy region operation
            int copyWidth = Math.Min(srcBitmapRect.Width, destBitmapRect.Width);
            int copyHeight = Math.Min(srcBitmapRect.Height, destBitmapRect.Height);

            if (copyWidth == 0 || copyHeight == 0)
                return;

            int srcStartX = srcBitmapRect.Left;
            int srcStartY = srcBitmapRect.Top;

            int destStartX = destBitmapRect.Left;
            int destStartY = destBitmapRect.Top;

            FastBitmap fastSource = new FastBitmap(source);

            using (fastSource.Lock())
            {
                ulong strideWidth = (ulong)copyWidth * BytesPerPixel;

                for (int y = 0; y < copyHeight; y++)
                {
                    int destX = destStartX;
                    int destY = destStartY + y;

                    int srcX = srcStartX;
                    int srcY = srcStartY + y;

                    long offsetSrc = (srcX + srcY * fastSource._strideWidth);
                    long offsetDest = (destX + destY * _strideWidth);

                    memcpy(_scan0 + offsetDest, fastSource._scan0 + offsetSrc, strideWidth);
                }
            }
        }

        /// <summary>
        /// Performs a copy operation of the pixels from the Source bitmap to the Target bitmap.
        /// If the dimensions or pixel depths of both images don't match, the copy is not performed
        /// </summary>
        /// <param name="source">The bitmap to copy the pixels from</param>
        /// <param name="target">The bitmap to copy the pixels to</param>
        /// <returns>Whether the copy proceedure was successful</returns>
        public static bool CopyPixels(Bitmap source, Bitmap target)
        {
            if (source.Width != target.Width || source.Height != target.Height || source.PixelFormat != target.PixelFormat)
                return false;

            FastBitmap fastSource = new FastBitmap(source);
            FastBitmap fastTarget = new FastBitmap(target);

            fastSource.Lock(ImageLockMode.ReadOnly);
            fastTarget.Lock();

            memcpy(fastTarget.Scan0, fastSource.Scan0, (ulong)(fastSource.Height * fastSource._strideWidth * BytesPerPixel));

            fastSource.Unlock();
            fastTarget.Unlock();

            return true;
        }

        /// <summary>
        /// Clears the given bitmap with the given color
        /// </summary>
        /// <param name="bitmap">The bitmap to clear</param>
        /// <param name="color">The color to clear the bitmap with</param>
        public static void ClearBitmap(Bitmap bitmap, Color color)
        {
            ClearBitmap(bitmap, color.ToArgb());
        }

        /// <summary>
        /// Clears the given bitmap with the given color
        /// </summary>
        /// <param name="bitmap">The bitmap to clear</param>
        /// <param name="color">The color to clear the bitmap with</param>
        public static void ClearBitmap(Bitmap bitmap, int color)
        {
            FastBitmap fb = new FastBitmap(bitmap);
            fb.Lock();
            fb.Clear(color);
            fb.Unlock();
        }

        /// <summary>
        /// Copies a region of the source bitmap to a target bitmap
        /// </summary>
        /// <param name="source">The source image to copy</param>
        /// <param name="target">The target image to be altered</param>
        /// <param name="srcRect">The region on the source bitmap that will be copied over</param>
        /// <param name="destRect">The region on the target bitmap that will be changed</param>
        /// <exception cref="ArgumentException">The provided source and target bitmaps are the same bitmap</exception>
        public static void CopyRegion(Bitmap source, Bitmap target, Rectangle srcRect, Rectangle destRect)
        {
            FastBitmap fastTarget = new FastBitmap(target);

            using (fastTarget.Lock())
            {
                fastTarget.CopyRegion(source, srcRect, destRect);
            }
        }

        // .NET wrapper to native call of 'memcpy'. Requires Microsoft Visual C++ Runtime installed
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, ulong count);

        // .NET wrapper to native call of 'memcpy'. Requires Microsoft Visual C++ Runtime installed
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(void* dest, void* src, ulong count);

        /// <summary>
        /// Represents a disposable structure that is returned during Lock() calls, and unlocks the bitmap on Dispose calls
        /// </summary>
        public struct FastBitmapLocker : IDisposable
        {
            /// <summary>
            /// The fast bitmap instance attached to this locker
            /// </summary>
            private readonly FastBitmap _fastBitmap;

            /// <summary>
            /// Gets the fast bitmap instance attached to this locker
            /// </summary>
            public FastBitmap FastBitmap
            {
                get { return _fastBitmap; }
            }

            /// <summary>
            /// Initializes a new instance of the FastBitmapLocker struct with an initial fast bitmap object.
            /// The fast bitmap object passed will be unlocked after calling Dispose() on this struct
            /// </summary>
            /// <param name="fastBitmap">A fast bitmap to attach to this locker which will be released after a call to Dispose</param>
            public FastBitmapLocker(FastBitmap fastBitmap)
            {
                _fastBitmap = fastBitmap;
            }

            /// <summary>
            /// Disposes of this FastBitmapLocker, essentially unlocking the underlying fast bitmap
            /// </summary>
            public void Dispose()
            {
                if(_fastBitmap._locked)
                    _fastBitmap.Unlock();
            }
        }
    }

    /// <summary>
    /// Static class that contains fast bitmap extension methdos for the Bitmap class
    /// </summary>
    public static class FastBitmapExtensions
    {
        /// <summary>
        /// Locks this bitmap into memory and returns a FastBitmap that can be used to manipulate its pixels
        /// </summary>
        /// <param name="bitmap">The bitmap to lock</param>
        /// <returns>A locked FastBitmap</returns>
        public static FastBitmap FastLock(this Bitmap bitmap)
        {
            FastBitmap fast = new FastBitmap(bitmap);
            fast.Lock();

            return fast;
        }
    }
}
