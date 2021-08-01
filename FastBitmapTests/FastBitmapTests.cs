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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using FastBitmapLib;
using JetBrains.Annotations;

namespace FastBitmapTests
{
    /// <summary>
    /// Contains tests for the FastBitmap class and related components
    /// </summary>
    [TestClass]
    public class FastBitmapTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            BitmapSnapshot.RecordMode = false;
        }

        [TestMethod]
        public void TestStride()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();

            Assert.AreEqual(fastBitmap.Stride, 64);
            Assert.AreEqual(fastBitmap.StrideInBytes, 64 * 4);

            fastBitmap.Unlock();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
            "Providing a bitmap with a bit-depth different than 32bpp to a FastBitmap must return an ArgumentException")]
        public void TestFastBitmapCreation()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();
            fastBitmap.Unlock();

            // Try creating a FastBitmap with different 32bpp depths
            try
            {
                // ReSharper disable once ObjectCreationAsStatement
                new FastBitmap(new Bitmap(1, 1, PixelFormat.Format32bppArgb));
                // ReSharper disable once ObjectCreationAsStatement
                new FastBitmap(new Bitmap(1, 1, PixelFormat.Format32bppPArgb));
                // ReSharper disable once ObjectCreationAsStatement
                new FastBitmap(new Bitmap(1, 1, PixelFormat.Format32bppRgb));
            }
            catch (ArgumentException)
            {
                Assert.Fail("The FastBitmap should accept any type of 32bpp pixel format bitmap");
            }

            // Try creating a FastBitmap with a bitmap of a bit depth different from 32bpp
            var invalidBitmap = new Bitmap(64, 64, PixelFormat.Format4bppIndexed);

            // ReSharper disable once ObjectCreationAsStatement
            new FastBitmap(invalidBitmap);
        }

        /// <summary>
        /// Tests sequential instances of FastBitmaps on the same Bitmap.
        /// As long as all the operations pending on a fast bitmap are finished, the original bitmap can be used in as many future fast bitmaps as needed.
        /// </summary>
        [TestMethod]
        public void TestSequentialFastBitmapLocking()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            Assert.IsFalse(fastBitmap.Locked, "Immediately after creation, the FastBitmap.Locked property must be false");

            fastBitmap.Lock();

            Assert.IsTrue(fastBitmap.Locked, "After a successful call to .Lock(), the .Locked property must be true");

            fastBitmap.Unlock();

            Assert.IsFalse(fastBitmap.Locked, "After a successful call to .Lock(), the .Locked property must be false");

            fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();
            fastBitmap.Unlock();
        }

        /// <summary>
        /// Tests a failing scenario for fast bitmap creations where a sequential fast bitmap is created and locked while another fast bitmap is operating on the same bitmap
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "Trying to Lock() a bitmap while it is Locked() in another FastBitmap must raise an exception")]
        public void TestFailedSequentialFastBitmapLocking()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();

            fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();
        }

        /// <summary>
        /// Tests the behavior of the .Clear() instance and class methods by clearing a bitmap and checking the result pixel-by-pixel
        /// </summary>
        [TestMethod]
        public void TestClearBitmap()
        {
            var bitmap = GenerateRainbowBitmap(63, 63); // Non-divisible by 8 bitmap, used to test loop unrolling
            FastBitmap.ClearBitmap(bitmap, Color.Red);

            // Loop through the image checking the pixels now
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.Red.ToArgb())
                    {
                        Assert.Fail(
                            "Immediately after a call to FastBitmap.Clear(), all of the bitmap's pixels must be of the provided color");
                    }
                }
            }

            // Test an arbitrary color now
            FastBitmap.ClearBitmap(bitmap, Color.FromArgb(25, 12, 0, 42));

            // Loop through the image checking the pixels now
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.FromArgb(25, 12, 0, 42).ToArgb())
                    {
                        Assert.Fail(
                            "Immediately after a call to FastBitmap.Clear(), all of the bitmap's pixels must be of the provided color");
                    }
                }
            }

            // Test instance call
            var fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Clear(Color.FromArgb(25, 12, 0, 42));

            Assert.IsFalse(fastBitmap.Locked, "After a successfull call to .Clear() on a fast bitmap previously unlocked, the .Locked property must be false");

            // Loop through the image checking the pixels now
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.FromArgb(25, 12, 0, 42).ToArgb())
                    {
                        Assert.Fail(
                            "Immediately after a call to FastBitmap.Clear(), all of the bitmap's pixels must be of the provided color");
                    }
                }
            }
        }

        [TestMethod]
        public void TestClearBitmapMemSetOptimization()
        {
            // If a provided color has the same byte values for each component
            // (e.g. 0xFFFFFFFF, 0xABABABAB, 0x66666666, etc.) the code takes a
            // fast path that simply mem-sets each row of the target image

            {
                var bitmap = new Bitmap(63, 63); // Non-divisible by 8 bitmap, used to test loop unrolling

                FillBitmapRegion(bitmap, new Rectangle(0, 0, 63, 63), Color.Red);
                
                using (var fastBitmap = bitmap.FastLock())
                {
                    fastBitmap.Clear(Color.White);
                }

                // Verify expected pixels
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.White.ToArgb(), $"{{{x},{y}}}");
                    }
                }
            }

            {
                // Now try with a black transparent colors

                var bitmap = new Bitmap(63, 63);

                FillBitmapRegion(bitmap, new Rectangle(0, 0, 64, 64), Color.Red);
                
                using (var fastBitmap = bitmap.FastLock())
                {
                    fastBitmap.Clear(Color.FromArgb(0));
                }

                // Verify expected pixels
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), 0, $"{{{x},{y}}}");
                    }
                }
            }
        }

        /// <summary>
        /// Tests the behavior of the GetPixel(x, y) method by comparing the results from it to the results of the native Bitmap.GetPixel()
        /// </summary>
        [TestMethod]
        public void TestGetPixel()
        {
            var original = GenerateRainbowBitmap(12, 12);
            var copy = original.Clone(new Rectangle(0, 0, 12, 12), original.PixelFormat);

            var fastOriginal = new FastBitmap(original);
            fastOriginal.Lock();

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Assert.AreEqual(fastOriginal.GetPixel(x, y).ToArgb(), copy.GetPixel(x, y).ToArgb(),
                        "Calls to FastBitmap.GetPixel() must return the same value as returned by Bitmap.GetPixel()");
                }
            }

            fastOriginal.Unlock();
        }
        
        /// <summary>
        /// Tests the behavior of the GetPixelInt(x, y) method by comparing the results from it to the results of the native Bitmap.GetPixel()
        /// </summary>
        [TestMethod]
        public void TestGetPixelInt()
        {
            var original = GenerateRainbowBitmap(12, 12);
            var copy = original.Clone(new Rectangle(0, 0, 12, 12), original.PixelFormat);

            var fastOriginal = new FastBitmap(original);
            fastOriginal.Lock();

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Assert.AreEqual(fastOriginal.GetPixelInt(x, y), copy.GetPixel(x, y).ToArgb(),
                        "Calls to FastBitmap.GetPixelInt() must return the same value as returned by Bitmap.GetPixel()");
                }
            }

            fastOriginal.Unlock();
        }

        /// <summary>
        /// Tests the behavior of the GetPixelUInt(x, y) method by comparing the results from it to the results of the native Bitmap.GetPixel()
        /// </summary>
        [TestMethod]
        public void TestGetPixelUInt()
        {
            var original = GenerateRainbowBitmap(12, 12);
            var copy = original.Clone(new Rectangle(0, 0, 12, 12), original.PixelFormat);

            var fastOriginal = new FastBitmap(original);
            fastOriginal.Lock();

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Assert.AreEqual(fastOriginal.GetPixelUInt(x, y), (uint)copy.GetPixel(x, y).ToArgb(),
                        "Calls to FastBitmap.GetPixelUInt() must return the same value as returned by Bitmap.GetPixel()");
                }
            }

            fastOriginal.Unlock();
        }

        /// <summary>
        /// Tests the behavior of the GetPixelUInt(index) method by comparing the results from it to the results of the native Bitmap.GetPixel()
        /// </summary>
        [TestMethod]
        public void TestGetPixelUIntIndex()
        {
            var original = GenerateRainbowBitmap(12, 12);
            var copy = original.Clone(new Rectangle(0, 0, 12, 12), original.PixelFormat);

            var fastOriginal = new FastBitmap(original);
            fastOriginal.Lock();

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Assert.AreEqual(fastOriginal.GetPixelUInt(x + y * fastOriginal.Height), (uint)copy.GetPixel(x, y).ToArgb(),
                        "Calls to FastBitmap.GetPixelUInt() must return the same value as returned by Bitmap.GetPixel()");
                }
            }

            fastOriginal.Unlock();
        }

        /// <summary>
        /// Tests the behavior of the SetPixel() method by randomly filling two bitmaps via native SetPixel and the implemented SetPixel, then comparing the output similarity
        /// </summary>
        [TestMethod]
        public void TestSetPixel()
        {
            var bitmap1 = new Bitmap(12, 12);
            var bitmap2 = new Bitmap(12, 12);

            var fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            var r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    int intColor = r.Next(0xFFFFFF);
                    var color = Color.FromArgb(intColor);

                    fastBitmap1.SetPixel(x, y, color);
                    bitmap2.SetPixel(x, y, color);
                }
            }

            fastBitmap1.Unlock();

            AssertBitmapEquals(bitmap1, bitmap2,
                "Calls to FastBitmap.SetPixel() must be equivalent to calls to Bitmap.SetPixel()");
        }

        /// <summary>
        /// Tests the behavior of the SetPixel() integer overload method by randomly filling two bitmaps via native SetPixel and the implemented SetPixel, then comparing the output similarity
        /// </summary>
        [TestMethod]
        public void TestSetPixelInt()
        {
            var bitmap1 = new Bitmap(12, 12);
            var bitmap2 = new Bitmap(12, 12);

            var fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            var r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    int intColor = r.Next(0xFFFFFF);
                    var color = Color.FromArgb(intColor);

                    fastBitmap1.SetPixel(x, y, intColor);
                    bitmap2.SetPixel(x, y, color);
                }
            }

            fastBitmap1.Unlock();

            AssertBitmapEquals(bitmap1, bitmap2,
                "Calls to FastBitmap.SetPixel() with an integer overload must be equivalent to calls to Bitmap.SetPixel() with a Color with the same ARGB value as the interger");
        }

        /// <summary>
        /// Tests the behavior of the SetPixel() unsigned integer overload method by randomly filling two bitmaps via native SetPixel and the implemented SetPixel, then comparing the output similarity
        /// </summary>
        [TestMethod]
        public void TestSetPixelUInt()
        {
            var bitmap1 = new Bitmap(12, 12);
            var bitmap2 = new Bitmap(12, 12);

            var fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            var r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    uint uintColor = (uint)r.Next(0xFFFFFF);
                    var color = Color.FromArgb((int)uintColor);

                    fastBitmap1.SetPixel(x, y, uintColor);
                    bitmap2.SetPixel(x, y, color);
                }
            }

            fastBitmap1.Unlock();

            AssertBitmapEquals(bitmap1, bitmap2,
                "Calls to FastBitmap.SetPixel() with an integer overload must be equivalent to calls to Bitmap.SetPixel() with a Color with the same ARGB value as the interger");
        }

        /// <summary>
        /// Tests the behavior of the SetPixel() indexed unsigned integer overload method by randomly filling two bitmaps via native SetPixel and the implemented SetPixel, then comparing the output similarity
        /// </summary>
        [TestMethod]
        public void TestSetPixelUIntIndex()
        {
            var bitmap1 = new Bitmap(12, 12);
            var bitmap2 = new Bitmap(12, 12);

            var fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            var r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    uint uintColor = (uint)r.Next(0xFFFFFF);
                    var color = Color.FromArgb((int)uintColor);

                    fastBitmap1.SetPixel(x + y * fastBitmap1.Height, uintColor);
                    bitmap2.SetPixel(x, y, color);
                }
            }

            fastBitmap1.Unlock();

            AssertBitmapEquals(bitmap1, bitmap2,
                "Calls to FastBitmap.SetPixel() with an integer overload must be equivalent to calls to Bitmap.SetPixel() with a Color with the same ARGB value as the interger");
        }

        /// <summary>
        /// Tests a call to FastBitmap.CopyPixels() with valid provided bitmaps
        /// </summary>
        [TestMethod]
        public void TestValidCopyPixels()
        {
            var bitmap1 = GenerateRainbowBitmap(64, 64);
            var bitmap2 = new Bitmap(64, 64);

            FastBitmap.CopyPixels(bitmap1, bitmap2);

            AssertBitmapEquals(bitmap1, bitmap2,
                "After a successful call to CopyPixels(), both bitmaps must be equal down to the pixel level");
        }

        /// <summary>
        /// Tests a call to FastBitmap.CopyPixels() with bitmaps of different sizes and different bitdepths
        /// </summary>
        [TestMethod]
        public void TestInvalidCopyPixels()
        {
            var bitmap1 = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            var bitmap2 = new Bitmap(64, 64, PixelFormat.Format1bppIndexed);

            if (FastBitmap.CopyPixels(bitmap1, bitmap2))
            {
                Assert.Fail("Trying to copy two bitmaps of different bitdepths should not be allowed");
            }

            bitmap1 = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
            bitmap2 = new Bitmap(66, 64, PixelFormat.Format32bppArgb);

            if (FastBitmap.CopyPixels(bitmap1, bitmap2))
            {
                Assert.Fail("Trying to copy two bitmaps of different sizes should not be allowed");
            }
        }

        #region CopyRegion Tests

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities
        /// </summary>
        [TestMethod]
        public void TestSimpleCopyRegion()
        {
            var canvasBitmap = new Bitmap(64, 64);
            var copyBitmap = GenerateRainbowBitmap(32, 32);

            var sourceRectangle = new Rectangle(0, 0, 32, 32);
            var targetRectangle = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);
            
            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source and target rectangles are moved around, and the source rectangle clips outside the bounds of the copy bitmap
        /// </summary>
        [TestMethod]
        public void TestComplexCopyRegion()
        {
            var canvasBitmap = new Bitmap(64, 64);
            var copyBitmap = GenerateRainbowBitmap(32, 32);

            var sourceRectangle = new Rectangle(5, 5, 32, 32);
            var targetRectangle = new Rectangle(9, 9, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);
            
            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The copy region clips outside the target and source bitmap areas
        /// </summary>
        [TestMethod]
        public void TestClippingCopyRegion()
        {
            var canvasBitmap = new Bitmap(64, 64);
            var copyBitmap = GenerateRainbowBitmap(32, 32);

            var sourceRectangle = new Rectangle(-5, 5, 32, 32);
            var targetRectangle = new Rectangle(40, 9, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);
            
            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source region provided is out of the bounds of the copy image
        /// </summary>
        [TestMethod]
        public void TestOutOfBoundsCopyRegion()
        {
            var canvasBitmap = new Bitmap(64, 64);
            var copyBitmap = GenerateRainbowBitmap(32, 32);

            var sourceRectangle = new Rectangle(32, 0, 32, 32);
            var targetRectangle = new Rectangle(0, 0, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);
            
            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source region provided is invalid, and no modifications are to be made
        /// </summary>
        [TestMethod]
        public void TestInvalidCopyRegion()
        {
            var canvasBitmap = new Bitmap(64, 64);
            var copyBitmap = GenerateRainbowBitmap(32, 32);

            var sourceRectangle = new Rectangle(0, 0, -1, 32);
            var targetRectangle = new Rectangle(0, 0, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);
            
            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        /// <summary>
        /// Tests sequential region copying across multiple bitmaps by copying regions between 4 bitmaps
        /// </summary>
        [TestMethod]
        public void TestSequentialCopyRegion()
        {
            var bitmap1 = new Bitmap(64, 64);
            var bitmap2 = new Bitmap(64, 64);
            var bitmap3 = new Bitmap(64, 64);
            var bitmap4 = new Bitmap(64, 64);

            var region = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(bitmap1, bitmap2, region, region);
            FastBitmap.CopyRegion(bitmap3, bitmap4, region, region);
            FastBitmap.CopyRegion(bitmap1, bitmap3, region, region);
            FastBitmap.CopyRegion(bitmap4, bitmap2, region, region);
        }

        /// <summary>
        /// Tests a copy region operation that is slices through the destination
        /// </summary>
        [TestMethod]
        public void TestSlicedDestinationCopyRegion()
        {
            // Have a copy operation that goes:
            //
            //       -src---
            // -dest-|-----|------
            // |     |xxxxx|     |
            // |     |xxxxx|     |
            // ------|-----|------
            //       -------
            // 

            var canvasBitmap = new Bitmap(128, 32);
            var copyBitmap = GenerateRainbowBitmap(32, 64);

            var sourceRectangle = new Rectangle(0, 0, 32, 64);
            var targetRectangle = new Rectangle(48, -16, 32, 64);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            BitmapSnapshot.Snapshot(canvasBitmap, TestContext);
        }

        #endregion

        /// <summary>
        /// Tests the FastBitmapLocker struct returned by lock calls
        /// </summary>
        [TestMethod]
        public void TestFastBitmapLocker()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            // Immediate lock and dispose
            fastBitmap.Lock().Dispose();
            Assert.IsFalse(fastBitmap.Locked, "After disposing of the FastBitmapLocker object, the underlying fast bitmap must be unlocked");

            using (var locker = fastBitmap.Lock())
            {
                fastBitmap.SetPixel(0, 0, 0);

                Assert.AreEqual(fastBitmap, locker.FastBitmap, "The fast bitmap referenced in the fast bitmap locker must be the one that had the original Lock() call");
            }

            Assert.IsFalse(fastBitmap.Locked, "After disposing of the FastBitmapLocker object, the underlying fast bitmap must be unlocked");

            // Test the conditional unlocking of the fast bitmap locker by unlocking the fast bitmap before exiting the 'using' block
            using (fastBitmap.Lock())
            {
                fastBitmap.SetPixel(0, 0, 0);
                fastBitmap.Unlock();
            }
        }

        [TestMethod]
        public void TestLockExtensionMethod()
        {
            var bitmap = new Bitmap(64, 64);

            using (var fast = bitmap.FastLock())
            {
                fast.SetPixel(0, 0, Color.Red);
            }

            // Test unlocking by trying to modify the bitmap
            bitmap.SetPixel(0, 0, Color.Blue);
        }

        [TestMethod]
        public void TestDataArray()
        {
            // TODO: Devise a way to test the returned array in a more consistent way, because currently this test only deals with ARGB pixel values because Bitmap.GetPixel().ToArgb() only returns 0xAARRGGBB format values
            var bitmap = GenerateRainbowBitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            Assert.IsFalse(fastBitmap.Locked, "After accessing the .Data property on a fast bitmap previously unlocked, the .Locked property must be false");

            var pixels = fastBitmap.DataArray;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), pixels[y * bitmap.Width + x], "");
                }
            }
        }

        [TestMethod]
        public void TestGetDataAsArray()
        {
            // TODO: Devise a way to test the returned array in a more consistent way, because currently this test only deals with ARGB pixel values because Bitmap.GetPixel().ToArgb() only returns 0xAARRGGBB format values
            var bitmap = GenerateRainbowBitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            Assert.IsFalse(fastBitmap.Locked, "After accessing the .Data property on a fast bitmap previously unlocked, the .Locked property must be false");

            var pixels = fastBitmap.GetDataAsArray();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), pixels[y * bitmap.Width + x], "");
                }
            }
        }

        [TestMethod]
        public void TestCopyFromArray()
        {
            var bitmap = new Bitmap(4, 4);
            int[] colors =
            {
                0xFFFFFF, 0xFFFFEF, 0xABABAB, 0xABCDEF,
                0x111111, 0x123456, 0x654321, 0x000000,
                0xFFFFFF, 0xFFFFEF, 0xABABAB, 0xABCDEF,
                0x111111, 0x123456, 0x654321, 0x000000
            };

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.CopyFromArray(colors);
            }

            // Test now the resulting bitmap
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int index = y * bitmap.Width + x;

                    Assert.AreEqual(colors[index], bitmap.GetPixel(x, y).ToArgb(),
                        "After a call to CopyFromArray, the values provided on the on the array must match the values in the bitmap pixels");
                }
            }
        }

        [TestMethod]
        public void TestCopyFromArrayIgnoreZeroes()
        {
            var bitmap = new Bitmap(4, 4);

            FillBitmapRegion(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), Color.Red);

            int[] colors =
            {
                0xFFFFFF, 0xFFFFEF, 0xABABAB, 0xABCDEF,
                0x111111, 0x123456, 0x654321, 0x000000,
                0x000000, 0xFFFFEF, 0x000000, 0xABCDEF,
                0x000000, 0x000000, 0x654321, 0x000000
            };

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.CopyFromArray(colors, true);
            }

            // Test now the resulting bitmap
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int index = y * bitmap.Width + x;
                    int arrayColor = colors[index];
                    int bitmapColor = bitmap.GetPixel(x, y).ToArgb();

                    if (arrayColor != 0)
                    {
                        Assert.AreEqual(arrayColor, bitmapColor,
                            "After a call to CopyFromArray(_, true), the non-zeroes values provided on the on the array must match the values in the bitmap pixels");
                    }
                    else
                    {
                        Assert.AreEqual(Color.Red.ToArgb(), bitmapColor,
                            "After a call to CopyFromArray(_, true), the 0 values on the original array must not be copied over");
                    }
                }
            }
        }

        [TestMethod]
        public void TestClearRegionSmall()
        {
            var bitmap = new Bitmap(16, 16);

            FillBitmapRegion(bitmap, new Rectangle(0, 0, 16, 16), Color.Red);

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.ClearRegion(new Rectangle(1, 1, 4, 4), Color.White);
            }

            // Verify expected pixels
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (x >= 1 && x <= 4 && y >= 1 && y <= 4)
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.White.ToArgb(), $"{{{x},{y}}}");
                    else
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.Red.ToArgb(), $"{{{x},{y}}}");
                }
            }
        }

        [TestMethod]
        public void TestClearRegionEntireBitmap()
        {
            var bitmap = new Bitmap(16, 16);

            FillBitmapRegion(bitmap, new Rectangle(0, 0, 16, 16), Color.Red);

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.ClearRegion(new Rectangle(0, 0, 16, 16), Color.White);
            }

            // Verify expected pixels
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.White.ToArgb(), $"{{{x},{y}}}");
                }
            }
        }

        [TestMethod]
        public void TestClearRegionRowBlockCopyOptimization()
        {
            var bitmap = new Bitmap(63, 63); // Non-dibisible by 8 bitmap, used to test loop unrolling

            FillBitmapRegion(bitmap, new Rectangle(0, 0, 63, 63), Color.Red);

            var region = new Rectangle(4, 4, 16, 16);

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.ClearRegion(region, Color.Blue);
            }

            // Verify expected pixels
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (x >= region.Left && x < region.Right && y >= region.Top && y < region.Bottom)
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.Blue.ToArgb(), $"{{{x},{y}}}");
                    else
                        Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.Red.ToArgb(), $"{{{x},{y}}}");
                }
            }
        }

        [TestMethod]
        public void TestClearRegionMemSetOptimization()
        {
            // If a provided color has the same byte values for each component
            // (e.g. 0xFFFFFFFF, 0xABABABAB, 0x66666666, etc.) the code takes a
            // fast path that simply mem-sets each row of the target image

            {
                var bitmap = new Bitmap(63, 63); // Non-dibisible by 8 bitmap, used to test loop unrolling

                FillBitmapRegion(bitmap, new Rectangle(0, 0, 63, 63), Color.Red);

                var region = new Rectangle(4, 4, 16, 16);

                using (var fastBitmap = bitmap.FastLock())
                {
                    fastBitmap.ClearRegion(region, Color.White);
                }

                // Verify expected pixels
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        if (x >= region.Left && x < region.Right && y >= region.Top && y < region.Bottom)
                            Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.White.ToArgb(), $"{{{x},{y}}}");
                        else
                            Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.Red.ToArgb(), $"{{{x},{y}}}");
                    }
                }
            }

            {
                // Now try with a black transparent colors

                var bitmap = new Bitmap(63, 63);

                FillBitmapRegion(bitmap, new Rectangle(0, 0, 63, 63), Color.Red);

                var region = new Rectangle(4, 4, 16, 16);

                using (var fastBitmap = bitmap.FastLock())
                {
                    fastBitmap.ClearRegion(region, Color.FromArgb(0));
                }

                // Verify expected pixels
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        if (x >= region.Left && x < region.Right && y >= region.Top && y < region.Bottom)
                            Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), 0, $"{{{x},{y}}}");
                        else
                            Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), Color.Red.ToArgb(), $"{{{x},{y}}}");
                    }
                }
            }
        }

        [TestMethod]
        public void TestLockFormatArgbToPArgb()
        {
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock(FastBitmapLockFormat.Format32bppPArgb);

            fastBitmap.Unlock();
        }

        [TestMethod]
        public void TestLockFormatArgbToRgb()
        {
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock(FastBitmapLockFormat.Format32bppRgb);

            fastBitmap.Unlock();
        }

        [TestMethod]
        public void TestLockFormatArgbToArgb()
        {
            var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock(FastBitmapLockFormat.Format32bppArgb);

            fastBitmap.Unlock();
        }

        #region Exception Tests

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to unlock a FastBitmap that is not locked, an exception must be thrown")]
        public void TestUnlockWhileUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Unlock();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to lock a FastBitmap that is already locked, an exception must be thrown")]
        public void TestLockWhileLockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();
            fastBitmap.Lock();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via GetPixel(x, y) while it is unlocked, an exception must be thrown"
            )]
        public void TestGetPixelUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.GetPixel(0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via GetPixelInt(x, y) while it is unlocked, an exception must be thrown"
        )]
        public void TestGetPixelIntUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.GetPixelInt(0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via GetPixelUInt(x, y) while it is unlocked, an exception must be thrown"
        )]
        public void TestGetPixelUIntUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.GetPixelUInt(0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via GetPixelUInt(index) while it is unlocked, an exception must be thrown"
        )]
        public void TestGetPixelUIntIndexUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.GetPixelUInt(0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via SetPixel(x, y) while it is unlocked, an exception must be thrown"
            )]
        public void TestSetPixelUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.SetPixel(0, 0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "When trying to read or write to the FastBitmap via SetPixel(index) while it is unlocked, an exception must be thrown"
        )]
        public void TestSetPixelIndexUnlockedException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.SetPixel(0, 0);
        }

        [TestMethod]
        public void TestGetPixelBoundsException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.GetPixel(-1, -1);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixel(fastBitmap.Width, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixel(0, fastBitmap.Height);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            fastBitmap.GetPixel(fastBitmap.Width - 1, fastBitmap.Height - 1);
        }

        [TestMethod]
        public void TestGetPixelIntBoundsException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.GetPixelInt(-1, -1);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixelInt(fastBitmap.Width, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixelInt(0, fastBitmap.Height);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            fastBitmap.GetPixelInt(fastBitmap.Width - 1, fastBitmap.Height - 1);
        }

        [TestMethod]
        public void TestGetPixelUIntBoundsException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.GetPixelUInt(-1, -1);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelUInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixelUInt(fastBitmap.Width, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelUInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixelUInt(0, fastBitmap.Height);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelUInt(x, y), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            fastBitmap.GetPixelUInt(fastBitmap.Width - 1, fastBitmap.Height - 1);
        }

        [TestMethod]
        public void TestGetPixelUIntIndexBoundsException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.GetPixelUInt(-1);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelUInt(index), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.GetPixelUInt(fastBitmap.Height * fastBitmap.Stride);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixelUInt(index), an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }
            
            fastBitmap.GetPixelUInt(fastBitmap.Height * fastBitmap.Stride - 1);
        }

        [TestMethod]
        public void TestSetPixelBoundsException()
        {
            var bitmap = new Bitmap(64, 64);
            var fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.SetPixel(-1, -1, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via SetPixel, an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.SetPixel(fastBitmap.Width, 0, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via SetPixel, an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.SetPixel(0, fastBitmap.Height, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via SetPixel, an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                fastBitmap.SetPixel(fastBitmap.Height * fastBitmap.Stride, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via SetPixel, an exception must be thrown");
            }
            catch (ArgumentOutOfRangeException) { }

            fastBitmap.SetPixel(fastBitmap.Width - 1, fastBitmap.Height - 1, 0);
            fastBitmap.SetPixel(fastBitmap.Height * fastBitmap.Stride - 1, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "An ArgumentException exception must be thrown when trying to copy regions across the same bitmap")]
        public void TestSameBitmapCopyRegionException()
        {
            var bitmap = new Bitmap(64, 64);

            var sourceRectangle = new Rectangle(0, 0, 64, 64);
            var targetRectangle = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(bitmap, bitmap, sourceRectangle, targetRectangle);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
            "an ArgumentException exception must be raised when calling CopyFromArray() with an array of colors that does not match the pixel count of the bitmap")]
        public void TestCopyFromArrayMismatchedLengthException()
        {
            var bitmap = new Bitmap(4, 4);

            FillBitmapRegion(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), Color.Red);

            int[] colors =
            {
                0xFFFFFF, 0xFFFFEF, 0xABABAB, 0xABCDEF,
                0x111111, 0x123456, 0x654321, 0x000000,
                0x000000, 0xFFFFEF, 0x000000, 0xABCDEF,
                0x000000, 0x000000, 0x654321, 0x000000,
                0x000000, 0x000000, 0x654321, 0x000000
            };

            using (var fastBitmap = bitmap.FastLock())
            {
                fastBitmap.CopyFromArray(colors, true);
            }
        }

        #endregion

        /// <summary>
        /// Generates a frame image with a given set of parameters.
        /// The seed is used to randomize the frame, and any call with the same width, height and seed will generate the same image
        /// </summary>
        /// <param name="width">The width of the image to generate</param>
        /// <param name="height">The height of the image to generate</param>
        /// <param name="seed">The seed for the image, used to seed the random number generator that will generate the image contents</param>
        /// <returns>An image with the passed parameters</returns>
        public static Bitmap GenerateRainbowBitmap(int width, int height, int seed = 0)
        {
            uint RainbowRgb(float hue)
            {
                //        |_    _|__ 1
                //   Red: | \__/ |__ 0
                //        |______|
                //        | __   |__ 1
                // Green: |/  \__|__ 0
                //        |______|
                //        |   __ |__ 1
                //  Blue: |__/  \|__ 0
                //        |_.__._|
                //        0 |  | 1
                //         1/3 |
                //            2/3

                float r;
                float g;
                float b;
                if (hue < 1 / 3.0f)
                {
                    r = 2 - hue * 6;
                    g = hue * 6;
                    b = 0;
                }
                else if (hue < 2 / 3.0f)
                {
                    r = 0; 
                    g = 4 - hue * 6; 
                    b = hue * 6 - 2; 
                }
                else
                {
                    r = hue * 6 - 4; 
                    g = 0;
                    b = (1 - hue) * 6;
                }

                if (r > 1) r = 1;
                if (g > 1) g = 1;
                if (b > 1) b = 1;

                uint rInt = (uint) (r * 255);
                uint gInt = (uint) (g * 255);
                uint bInt = (uint) (b * 255);

                return ((uint)0xFF << 24) | ((rInt & 0xFF) << 16) | ((gInt & 0xFF) << 8) | (bInt & 0xFF);
            }

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint pixelColor = RainbowRgb((float)x / width);

                    fastBitmap.SetPixel(x, y, pixelColor);
                }
            }
            fastBitmap.Unlock();
            return bitmap;
        }

        /// <summary>
        /// Fills a rectangle region of bitmap with a specified color
        /// </summary>
        /// <param name="bitmap">The bitmap to operate on</param>
        /// <param name="region">The region to fill on the bitmap</param>
        /// <param name="color">The color to fill the bitmap with</param>
        public static void FillBitmapRegion([NotNull] Bitmap bitmap, Rectangle region, Color color)
        {
            for (int y = Math.Max(0, region.Top); y < Math.Min(bitmap.Height, region.Bottom); y++)
            {
                for (int x = Math.Max(0, region.Left); x < Math.Min(bitmap.Width, region.Right); x++)
                {
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Helper method that tests the equality of two bitmaps and fails with a provided assert message when they are not pixel-by-pixel equal
        /// </summary>
        /// <param name="bitmap1">The first bitmap object to compare</param>
        /// <param name="bitmap2">The second bitmap object to compare</param>
        /// <param name="message">The message to display when the comparision fails</param>
        public static void AssertBitmapEquals([NotNull] Bitmap bitmap1, [NotNull] Bitmap bitmap2, string message = "")
        {
            if (bitmap1.PixelFormat != bitmap2.PixelFormat)
                Assert.Fail(message);

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    Assert.AreEqual(bitmap1.GetPixel(x, y).ToArgb(), bitmap2.GetPixel(x, y).ToArgb(), message);
                }
            }
        }

        public TestContext TestContext { get; set; }
    }
}