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

namespace Pixelaria.Utils
{
    /// <summary>
    /// Encapsulates a Bitmap for fast bitmap pixel operations using 32bpp images
    /// </summary>
    public unsafe class FastBitmap
    {
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
        private int _width;

        /// <summary>
        /// The height of this FastBitmap
        /// </summary>
        private int _height;

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
                int[] argbValues = new int[bytes / 4];

                // Copy the RGB values into the array
                Marshal.Copy(_bitmapData.Scan0, argbValues, 0, bytes / 4);

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
        /// Locks the bitmap to start the bitmap operations. If the bitmap is already locked,
        /// an exception is thrown
        /// </summary>
        /// <exception cref="InvalidOperationException">The bitmap is already locked</exception>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap is already locked outside this fast bitmap</exception>
        public void Lock()
        {
            if (_locked)
            {
                throw new InvalidOperationException("Unlock must be called before a Lock operation");
            }

            Lock(ImageLockMode.ReadWrite);
        }

        /// <summary>
        /// Locks the bitmap to start the bitmap operations
        /// </summary>
        /// <param name="lockMode">The lock mode to use on the bitmap</param>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap is already locked outside this fast bitmap</exception>
        private void Lock(ImageLockMode lockMode)
        {
            Rectangle rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);

            Lock(lockMode, rect);
        }

        /// <summary>
        /// Locks the bitmap to start the bitmap operations
        /// </summary>
        /// <param name="lockMode">The lock mode to use on the bitmap</param>
        /// <param name="rect">The rectangle to lock</param>
        /// <exception cref="System.ArgumentException">The provided region is invalid</exception>
        /// <exception cref="System.Exception">The locking operation in the underlying bitmap failed</exception>
        /// <exception cref="InvalidOperationException">The bitmap region is already locked</exception>
        private void Lock(ImageLockMode lockMode, Rectangle rect)
        {
            // Lock the bitmap's bits
            _bitmapData = _bitmap.LockBits(rect, lockMode, _bitmap.PixelFormat);

            _scan0 = (int*)_bitmapData.Scan0;
            _strideWidth = _bitmapData.Stride / 4;

            _locked = true;
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

            int rem = count % 8;

            count /= 8;

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
            // Check if the rectangle configuration doesn't generate invalid states or does not affect the target image
            if (srcRect.Width <= 0 || srcRect.Height <= 0 || destRect.Width <= 0 || destRect.Height <= 0 || destRect.X > _width || destRect.Y > _height)
                return;

            // Throw exception when trying to copy same bitmap over
            if (source == _bitmap)
            {
                throw new ArgumentException("Copying regions across the same bitmap is not supported", "source");
            }

            FastBitmap fastSource = new FastBitmap(source);
            fastSource.Lock();

            int copyWidth = Math.Min(srcRect.Width, destRect.Width);
            int copyHeight = Math.Min(srcRect.Height, destRect.Height);

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int destX = destRect.X + x;
                    int destY = destRect.Y + y;

                    int srcX = x + srcRect.X;
                    int srcY = y + srcRect.Y;

                    if (destX >= 0 && destY >= 0 && destX < _width && destY < _height && srcX >= 0 && srcY >= 0 && srcX < fastSource._width && srcY < fastSource._height)
                    {
                        SetPixel(destX, destY, fastSource.GetPixelInt(srcX, srcY));
                    }
                }
            }

            fastSource.Unlock();
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

            // Simply copy the argb values array
            int *s0s = fastSource._scan0;
            int *s0t = fastTarget._scan0;

            const int bpp = 1; // Bytes per pixel

            int count = fastSource._width * fastSource._height * bpp;
            int rem = count % 8;

            count /= 8;

            while (count-- > 0)
            {
                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);

                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);
                *(s0t++) = *(s0s++);
            }
            while (rem-- > 0)
            {
                *(s0t++) = *(s0s++);
            }

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
            // Check if the rectangle configuration doesn't generate invalid states or does not affect the target image
            if (srcRect.Width <= 0 || srcRect.Height <= 0 || destRect.Width <= 0 || destRect.Height <= 0 || destRect.X > target.Width || destRect.Y > target.Height)
                return;

            // Throw exception when trying to copy same bitmap over
            if (source == target)
            {
                throw new ArgumentException("Copying regions across the same bitmap is not supported", "source");
            }

            FastBitmap fastTarget = new FastBitmap(target);
            fastTarget.Lock();

            fastTarget.CopyRegion(source, srcRect, destRect);

            fastTarget.Unlock();
        }
    }
}
