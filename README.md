FastBitmap - The Fast C# Bitmap Layer
=====================================
Based on work found on Visual C# Kicks: http://www.vcskicks.com/fast-image-processing.php

[![Build status](https://ci.appveyor.com/api/projects/status/fwt610ekt3knglp3?svg=true)](https://ci.appveyor.com/project/LuizZak/fastbitmap)
[![codecov](https://codecov.io/gh/LuizZak/FastBitmap/branch/master/graph/badge.svg)](https://codecov.io/gh/LuizZak/FastBitmap)

FastBitmap is a bitmap wrapper class that intends to provide fast bitmap read/write operations on top of a safe layer of abstraction.

It provides operations for setting/getting pixel colors, copying regions accross bitmaps, clearing whole bitmaps, and copying whole bitmaps.

Editing pixels of a bitmap is just as easy as:

```C#
    Bitmap bitmap = new Bitmap(64, 64);
    
    using(var fastBitmap = bitmap.FastLock())
    {
        // Do your changes here...
        fastBitmap.Clear(Color.White);
        fastBitmap.SetPixel(1, 1, Color.Red);
    }
```

Or alternatively, albeit longer:

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

This project contains the FastBitmap class and acompanying unit test suite.  
Note that to compile this class you must have the /unsafe compiler flag turned on in your project settings.

**Note: This code currently only works with 32bpp bitmaps**

Installation
---

FastBitmap is available as a [Nuget package](https://www.nuget.org/packages/FastBitmapLib):

```
PM > Install-Package FastBitmapLib
```

Speed
------

The following profiling tests showcase comparisions with the native System.Drawing.Bitmap equivalent method calls

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
1024 x 1024 Bitmap SetPixel: 2888ms  
1024 x 1024 FastBitmap CopyPixels: 5ms  

Results: FastBitmap **577,60x** faster

**Bitmap clearing profiling**  
1024 x 1024 Bitmap     SetPixel: 1795ms  
1024 x 1024 FastBitmap Clear:    5ms  

Results: FastBitmap **359,00x** faster  
