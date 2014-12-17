FastBitmap - The Fast C# Bitmap Layer
=====================================

FastBitmap is a bitmap wrapper class that intends to provide fast bitmap read/write operations on top of a safe layer of abstraction.

It provides operations for setting/getting pixel colors, copying regions accross bitmaps, clearing whole bitmaps, and copying whole bitmaps.

Note that to compile this class you must have the /unsafe compiler flag turned on in your project settings.

This project contains the FastBitmap class and acompanying unit test suite.

**Note: This code currently only works with 32bpp bitmaps**

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
```


Speed
------

The following profiling tests 

**SetPixel profiling**  
1024 x 1024 Bitmap         SetPixel: 2054ms  
1024 x 1024 FastBitmap     SetPixel: 398ms  
1024 x 1024 FastBitmap Int SetPixel: 331ms  

Results: FastBitmap **6,21x** faster

**GetPixel profiling**  
1024 x 1024 Bitmap         GetPixel: 1498ms  
1024 x 1024 FastBitmap     GetPixel: 382ms  
1024 x 1024 FastBitmap Int GetPixel: 327ms  

Results: FastBitmap **4,58x** faster  

**Bitmap copying profiling**  
1024 x 1024 Bitmap     SetPixel:    3038ms  
1024 x 1024 FastBitmap CopyPixels:  6ms  

Results: FastBitmap **506,33x** faster  

**Bitmap clearing profiling**  
1024 x 1024 Bitmap     SetPixel: 1795ms  
1024 x 1024 FastBitmap Clear:    5ms  

Results: FastBitmap **359,00x** faster  
