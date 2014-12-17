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

namespace FastBitmap.Utils
{
    /// <summary>
    /// Contains tests for the FastBitmap class and related components
    /// </summary>
    [TestClass]
    public class FastBitmapTests
    {
        [TestMethod]
        [ExpectedException(typeof (ArgumentException),
            "Providing a bitmap with a bitdepth different than 32bpp to a FastBitmap must return an ArgumentException")]
        public void TestFastBitmapCreation()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);
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
            Bitmap invalidBitmap = new Bitmap(64, 64, PixelFormat.Format4bppIndexed);

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
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

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
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);
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
            Bitmap bitmap = GenerateRandomBitmap(63, 63); // Non-dibisible by 8 bitmap, used to test loop unrolling
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

            // Test an arbitratry color now
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
            FastBitmap fastBitmap = new FastBitmap(bitmap);
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

        /// <summary>
        /// Tests the behavior of the GetPixel() method by comparing the results from it to the results of the native Bitmap.GetPixel()
        /// </summary>
        [TestMethod]
        public void TestGetPixel()
        {
            Bitmap original = GenerateRandomBitmap(64, 64);
            Bitmap copy = original.Clone(new Rectangle(0, 0, 64, 64), original.PixelFormat);

            FastBitmap fastOriginal = new FastBitmap(original);
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
        /// Tests the behavior of the SetPixel() method by randomly filling two bitmaps via native SetPixel and the implemented SetPixel, then comparing the output similarity
        /// </summary>
        [TestMethod]
        public void TestSetPixel()
        {
            Bitmap bitmap1 = new Bitmap(64, 64);
            Bitmap bitmap2 = new Bitmap(64, 64);

            FastBitmap fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            Random r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    int intColor = r.Next(0xFFFFFF);
                    Color color = Color.FromArgb(intColor);

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
            Bitmap bitmap1 = new Bitmap(64, 64);
            Bitmap bitmap2 = new Bitmap(64, 64);
            
            FastBitmap fastBitmap1 = new FastBitmap(bitmap1);
            fastBitmap1.Lock();

            Random r = new Random();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    int intColor = r.Next(0xFFFFFF);
                    Color color = Color.FromArgb(intColor);

                    fastBitmap1.SetPixel(x, y, intColor);
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
            Bitmap bitmap1 = GenerateRandomBitmap(64, 64);
            Bitmap bitmap2 = new Bitmap(64, 64);

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
            Bitmap bitmap1 = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            Bitmap bitmap2 = new Bitmap(64, 64, PixelFormat.Format1bppIndexed);

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

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities
        /// </summary>
        [TestMethod]
        public void TestSimpleCopyRegion()
        {
            Bitmap canvasBitmap = new Bitmap(64, 64);
            Bitmap copyBitmap = GenerateRandomBitmap(32, 32);

            Rectangle sourceRectangle = new Rectangle(0, 0, 32, 32);
            Rectangle targetRectangle = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            for (int y = targetRectangle.Y; y < Math.Min(sourceRectangle.Height, targetRectangle.Height); y++)
            {
                for (int x = targetRectangle.X; x < Math.Min(sourceRectangle.Width, targetRectangle.Width); x++)
                {
                    Assert.AreEqual(canvasBitmap.GetPixel(x, y).ToArgb(),
                        copyBitmap.GetPixel(x - targetRectangle.X, y - targetRectangle.Y).ToArgb(),
                        "Pixels of the target region must fully match the pixels from the origin region");
                }
            }
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source and target rectangles are moved around, and the source rectangle clips outside the bounds of the copy bitmap
        /// </summary>
        [TestMethod]
        public void TestComplexCopyRegion()
        {
            Bitmap canvasBitmap = new Bitmap(64, 64);
            Bitmap copyBitmap = GenerateRandomBitmap(32, 32);

            Rectangle sourceRectangle = new Rectangle(5, 5, 32, 32);
            Rectangle targetRectangle = new Rectangle(9, 9, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            for (int y = targetRectangle.Y; y < Math.Min(sourceRectangle.Height, targetRectangle.Height); y++)
            {
                for (int x = targetRectangle.X; x < Math.Min(sourceRectangle.Width, targetRectangle.Width); x++)
                {
                    // Ignore pixels out of range
                    if (x < 0 || y < 0 || x >= canvasBitmap.Width ||
                        x >= copyBitmap.Width + targetRectangle.X - sourceRectangle.X ||
                        y >= canvasBitmap.Height || y >= copyBitmap.Height + targetRectangle.Y - sourceRectangle.Y)
                        continue;

                    Assert.AreEqual(canvasBitmap.GetPixel(x, y).ToArgb(),
                        copyBitmap.GetPixel(x - targetRectangle.X + sourceRectangle.X,
                            y - targetRectangle.Y + sourceRectangle.Y).ToArgb(),
                        "Pixels of the target region must fully match the pixels from the origin region");
                }
            }
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The copy region clips outside the target and source bitmap areas
        /// </summary>
        [TestMethod]
        public void TestClippingCopyRegion()
        {
            Bitmap canvasBitmap = new Bitmap(64, 64);
            Bitmap copyBitmap = GenerateRandomBitmap(32, 32);

            Rectangle sourceRectangle = new Rectangle(-5, 5, 32, 32);
            Rectangle targetRectangle = new Rectangle(40, 9, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            for (int y = targetRectangle.Y; y < Math.Min(sourceRectangle.Height, targetRectangle.Height); y++)
            {
                for (int x = targetRectangle.X; x < Math.Min(sourceRectangle.Width, targetRectangle.Width); x++)
                {
                    // Ignore pixels out of range
                    if (x < 0 || y < 0 || x >= canvasBitmap.Width ||
                        x >= copyBitmap.Width + targetRectangle.X - sourceRectangle.X ||
                        y >= canvasBitmap.Height || y >= copyBitmap.Height + targetRectangle.Y - sourceRectangle.Y)
                        continue;

                    Assert.AreEqual(canvasBitmap.GetPixel(x, y).ToArgb(),
                        copyBitmap.GetPixel(x - targetRectangle.X + sourceRectangle.X,
                            y - targetRectangle.Y + sourceRectangle.Y).ToArgb(),
                        "Pixels of the target region must fully match the pixels from the origin region");
                }
            }
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source region provided is out of the bounds of the copy image
        /// </summary>
        [TestMethod]
        public void TestOutOfBoundsCopyRegion()
        {
            Bitmap canvasBitmap = new Bitmap(64, 64);
            Bitmap copyBitmap = GenerateRandomBitmap(32, 32);

            Rectangle sourceRectangle = new Rectangle(32, 0, 32, 32);
            Rectangle targetRectangle = new Rectangle(0, 0, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            for (int y = targetRectangle.Y; y < Math.Min(sourceRectangle.Height, targetRectangle.Height); y++)
            {
                for (int x = targetRectangle.X; x < Math.Min(sourceRectangle.Width, targetRectangle.Width); x++)
                {
                    // Ignore pixels out of range
                    if (x < 0 || y < 0 || x >= canvasBitmap.Width ||
                        x >= copyBitmap.Width + targetRectangle.X - sourceRectangle.X ||
                        y >= canvasBitmap.Height || y >= copyBitmap.Height + targetRectangle.Y - sourceRectangle.Y)
                        continue;

                    Assert.AreEqual(canvasBitmap.GetPixel(x, y).ToArgb(),
                        copyBitmap.GetPixel(x - targetRectangle.X + sourceRectangle.X,
                            y - targetRectangle.Y + sourceRectangle.Y).ToArgb(),
                        "Pixels of the target region must fully match the pixels from the origin region");
                }
            }
        }

        /// <summary>
        /// Tests the CopyRegion() static and instance methods by creating two bitmaps, copying regions over from one to another, and comparing the expected pixel equalities.
        /// The source region provided is invalid
        /// </summary>
        [TestMethod]
        public void TestInvalidCopyRegion()
        {
            Bitmap canvasBitmap = new Bitmap(64, 64);
            Bitmap copyBitmap = GenerateRandomBitmap(32, 32);

            Rectangle sourceRectangle = new Rectangle(0, 0, -1, 32);
            Rectangle targetRectangle = new Rectangle(0, 0, 23, 48);

            FastBitmap.CopyRegion(copyBitmap, canvasBitmap, sourceRectangle, targetRectangle);

            for (int y = targetRectangle.Y; y < Math.Min(sourceRectangle.Height, targetRectangle.Height); y++)
            {
                for (int x = targetRectangle.X; x < Math.Min(sourceRectangle.Width, targetRectangle.Width); x++)
                {
                    // Ignore pixels out of range
                    if (x < 0 || y < 0 || x >= canvasBitmap.Width ||
                        x >= copyBitmap.Width + targetRectangle.X - sourceRectangle.X ||
                        y >= canvasBitmap.Height || y >= copyBitmap.Height + targetRectangle.Y - sourceRectangle.Y)
                        continue;

                    Assert.AreEqual(canvasBitmap.GetPixel(x, y).ToArgb(),
                        copyBitmap.GetPixel(x - targetRectangle.X + sourceRectangle.X,
                            y - targetRectangle.Y + sourceRectangle.Y).ToArgb(),
                        "Pixels of the target region must fully match the pixels from the origin region");
                }
            }
        }

        /// <summary>
        /// Tests sequential region copying across multiple bitmaps by copying regions between 4 bitmaps
        /// </summary>
        [TestMethod]
        public void TestSequentialCopyRegion()
        {
            Bitmap bitmap1 = new Bitmap(64, 64);
            Bitmap bitmap2 = new Bitmap(64, 64);
            Bitmap bitmap3 = new Bitmap(64, 64);
            Bitmap bitmap4 = new Bitmap(64, 64);

            Rectangle region = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(bitmap1, bitmap2, region, region);
            FastBitmap.CopyRegion(bitmap3, bitmap4, region, region);
            FastBitmap.CopyRegion(bitmap1, bitmap3, region, region);
            FastBitmap.CopyRegion(bitmap4, bitmap2, region, region);
        }

        [TestMethod]
        public void TestDataArray()
        {
            // TODO: Devise a way to test the returned array in a more consistent way, because currently this test only deals with ARGB pixel values because Bitmap.GetPixel().ToArgb() only returns 0xAARRGGBB format values
            Bitmap bitmap = GenerateRandomBitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            Assert.IsFalse(fastBitmap.Locked, "After accessing the .Data property on a fast bitmap previously unlocked, the .Locked property must be false");

            int[] pixels = fastBitmap.DataArray;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Assert.AreEqual(bitmap.GetPixel(x, y).ToArgb(), pixels[y * bitmap.Width + x], "");
                }
            }
        }

        #region Exception Tests

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException),
            "When trying to unlock a FastBitmap that is not locked, an exception must be thrown")]
        public void TestFastBitmapUnlockingException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Unlock();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException),
            "When trying to lock a FastBitmap that is already locked, an exception must be thrown")]
        public void TestFastBitmapLockingException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();
            fastBitmap.Lock();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException),
            "When trying to read or write to the FastBitmap via GetPixel while it is unlocked, an exception must be thrown"
            )]
        public void TestFastBitmapUnlockedGetAccessException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.GetPixel(0, 0);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException),
            "When trying to read or write to the FastBitmap via SetPixel while it is unlocked, an exception must be thrown"
            )]
        public void TestFastBitmapUnlockedSetAccessException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.SetPixel(0, 0, 0);
        }

        [TestMethod]
        public void TestFastBitmapGetPixelBoundsException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.GetPixel(-1, -1);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            } catch (ArgumentException) { }

            try
            {
                fastBitmap.GetPixel(fastBitmap.Width, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            }
            catch (ArgumentException) { }

            try
            {
                fastBitmap.GetPixel(0, fastBitmap.Height);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            }
            catch (ArgumentException) { }

            fastBitmap.GetPixel(fastBitmap.Width - 1, fastBitmap.Height - 1);
        }

        [TestMethod]
        public void TestFastBitmapSetPixelBoundsException()
        {
            Bitmap bitmap = new Bitmap(64, 64);
            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            try
            {
                fastBitmap.SetPixel(-1, -1, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            }
            catch (ArgumentException) { }

            try
            {
                fastBitmap.SetPixel(fastBitmap.Width, 0, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            }
            catch (ArgumentException) { }

            try
            {
                fastBitmap.SetPixel(0, fastBitmap.Height, 0);
                Assert.Fail("When trying to access a coordinate that is out of bounds via GetPixel, an exception must be thrown");
            }
            catch (ArgumentException) { }

            fastBitmap.SetPixel(fastBitmap.Width - 1, fastBitmap.Height - 1, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "An ArgumentException exception must be thrown when trying to copy regions across the same bitmap")]
        public void TestSameBitmapCopyRegionException()
        {
            Bitmap bitmap = new Bitmap(64, 64);

            Rectangle sourceRectangle = new Rectangle(0, 0, 64, 64);
            Rectangle targetRectangle = new Rectangle(0, 0, 64, 64);

            FastBitmap.CopyRegion(bitmap, bitmap, sourceRectangle, targetRectangle);
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
        public static Bitmap GenerateRandomBitmap(int width, int height, int seed = -1)
        {
            if (seed == -1)
            {
                seed = _seedRandom.Next();
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            FastBitmap fastBitmap = new FastBitmap(bitmap);
            fastBitmap.Lock();

            // Plot the image with random pixels now
            Random r = new Random(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint pixelColor = (uint)(r.NextDouble() * 0xFFFFFFFF);
                    fastBitmap.SetPixel(x, y, pixelColor);
                }
            }

            fastBitmap.Unlock();

            return bitmap;
        }

        /// <summary>
        /// Helper method that tests the equality of two bitmaps and fails with a provided assert message when they are not pixel-by-pixel equal
        /// </summary>
        /// <param name="bitmap1">The first bitmap object to compare</param>
        /// <param name="bitmap2">The second bitmap object to compare</param>
        /// <param name="message">The message to display when the comparision fails</param>
        public void AssertBitmapEquals(Bitmap bitmap1, Bitmap bitmap2, string message = "")
        {
            if(bitmap1.PixelFormat != bitmap2.PixelFormat)
                Assert.Fail(message);

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    Assert.AreEqual(bitmap1.GetPixel(x, y).ToArgb(), bitmap2.GetPixel(x, y).ToArgb(), message);
                }
            }
        }
        
        /// <summary>
        /// Profiles the FastBitmap class against the native System.Drawing.Bitmap class
        /// </summary>
        public static void ProfileFastBitmap()
        {
            Console.WriteLine("-- SetPixel profiling");
            ProfileFastSetPixel();

            Console.WriteLine("\n-- GetPixel profiling");
            ProfileFastGetPixel();

            Console.WriteLine("\n-- Bitmap copying profiling");
            ProfileFastCopy();

            Console.WriteLine("\n-- Bitmap clearing profiling");
            ProfileFastClear();
        }

        /// <summary>
        /// Profiles the SetPixel() operation
        /// </summary>
        public static void ProfileFastSetPixel()
        {
            long bitmapMs;
            long fastBitmapMs;

            Bitmap bitmap = new Bitmap(1024, 1024);

            Stopwatch sw = Stopwatch.StartNew();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    bitmap.SetPixel(x, y, Color.Red);
                }
            }

            bitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap.Width + " x " + bitmap.Height + " Bitmap         SetPixel: " + bitmapMs + "ms");

            sw = Stopwatch.StartNew();

            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    fastBitmap.SetPixel(x, y, Color.Red);
                }
            }

            fastBitmap.Unlock();

            Console.WriteLine(fastBitmap.Width + " x " + fastBitmap.Height + " FastBitmap     SetPixel: " + sw.ElapsedMilliseconds + "ms");

            sw = Stopwatch.StartNew();

            fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            // We cache de color to an integer for faster setting
            int colorInt = Color.Red.ToArgb();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    fastBitmap.SetPixel(x, y, colorInt);
                }
            }

            fastBitmap.Unlock();

            fastBitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(fastBitmap.Width + " x " + fastBitmap.Height + " FastBitmap Int SetPixel: " + fastBitmapMs + "ms");
            Console.WriteLine("Results: FastBitmap " + ((float)bitmapMs / fastBitmapMs).ToString("0.00") + "x faster");
        }

        /// <summary>
        /// Profiles the GetPixel() operation
        /// </summary>
        public static void ProfileFastGetPixel()
        {
            long bitmapMs;
            long fastBitmapMs;

            Bitmap bitmap = new Bitmap(1024, 1024);

            Stopwatch sw = Stopwatch.StartNew();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    bitmap.GetPixel(x, y);
                }
            }

            bitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap.Width + " x " + bitmap.Height + " Bitmap         GetPixel: " + bitmapMs + "ms");

            sw = Stopwatch.StartNew();

            FastBitmap fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    fastBitmap.GetPixel(x, y);
                }
            }

            fastBitmap.Unlock();

            Console.WriteLine(fastBitmap.Width + " x " + fastBitmap.Height + " FastBitmap     GetPixel: " + sw.ElapsedMilliseconds + "ms");

            sw = Stopwatch.StartNew();

            fastBitmap = new FastBitmap(bitmap);

            fastBitmap.Lock();

            // We cache de color to an integer for faster setting
            int colorInt = Color.Red.ToArgb();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    fastBitmap.GetPixelInt(x, y);
                }
            }

            fastBitmap.Unlock();

            fastBitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(fastBitmap.Width + " x " + fastBitmap.Height + " FastBitmap Int GetPixel: " + fastBitmapMs + "ms");
            Console.WriteLine("Results: FastBitmap " + ((float)bitmapMs / fastBitmapMs).ToString("0.00") + "x faster");
        }

        /// <summary>
        /// Profiles a whole copy of pixels utilizing Bitmap's SetPixel and FastBitmap's CopyPixels
        /// </summary>
        public static void ProfileFastCopy()
        {
            long bitmapMs;
            long fastBitmapMs;

            Bitmap bitmap1 = new Bitmap(1024, 1024);
            Bitmap bitmap2 = new Bitmap(1024, 1024);

            Stopwatch sw = Stopwatch.StartNew();

            for (int y = 0; y < bitmap1.Height; y++)
            {
                for (int x = 0; x < bitmap1.Width; x++)
                {
                    bitmap1.SetPixel(x, y, bitmap2.GetPixel(x, y));
                }
            }

            bitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap1.Width + " x " + bitmap1.Height + " Bitmap     SetPixel:    " + bitmapMs + "ms");

            sw = Stopwatch.StartNew();

            FastBitmap.CopyPixels(bitmap1, bitmap2);

            fastBitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap1.Width + " x " + bitmap1.Height + " FastBitmap CopyPixels:  " + fastBitmapMs + "ms");
            Console.WriteLine("Results: FastBitmap " + ((float)bitmapMs / fastBitmapMs).ToString("0.00") + "x faster");
        }

        /// <summary>
        /// Profiles a clearing of all bitmap pixels using the Bitmap's SetPixel and the FastBitmap's Clear
        /// </summary>
        public static void ProfileFastClear()
        {
            long bitmapMs;
            long fastBitmapMs;

            Bitmap bitmap = new Bitmap(1024, 1024);

            Stopwatch sw = Stopwatch.StartNew();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    bitmap.SetPixel(x, y, Color.Red);
                }
            }

            bitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap.Width + " x " + bitmap.Height + " Bitmap     SetPixel: " + bitmapMs + "ms");

            sw = Stopwatch.StartNew();

            FastBitmap.ClearBitmap(bitmap, Color.Red);

            fastBitmapMs = sw.ElapsedMilliseconds;

            Console.WriteLine(bitmap.Width + " x " + bitmap.Height + " FastBitmap Clear:    " + fastBitmapMs + "ms");
            Console.WriteLine("Results: FastBitmap " + ((float)bitmapMs / fastBitmapMs).ToString("0.00") + "x faster");
        }

        /// <summary>
        /// Random number generator used to randomize seeds for image generation when none are provided
        /// </summary>
        private static Random _seedRandom = new Random();
    }
}
