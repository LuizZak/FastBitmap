FastBitmap - The Fast C# Bitmap Layer
=====================================

FastBitmap is a bitmap wrapper class that intends to provide fast bitmap read/write operations on top of a safe layer of abstraction.

It provides operations for setting/getting pixel colors, copying regions accross bitmaps, clearing whole bitmaps, and copying whole bitmaps.

This project contains the FastBitmap class and acompanying unit test suite.


Editing pixels of a bitmap is just as easy as:

```C#
    Bitmap bitmap = new Bitmap(64, 64);
    FastBitmap fastBitmap = new FastBitmap(bitmap);
    
    // Locking bitmap before doing operations
    fastBitmap.Lock();
    
    // Do your changes here...
    fastBitmap.Clear(Color.White);
    fastBitmap.SetPixel(1, 1, Color.Red);
    
    // Don't forget to unlock!
    fastBitmap.Unlock();
