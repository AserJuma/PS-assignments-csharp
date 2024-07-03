using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace study1
{
    public static class BitmapOperations
    {
        // Otsu helper function, Computes image histogram of pixel intensities
        // Initializes an array, iterates through and fills up histogram count values
        private static unsafe void GetHistogram(byte* pt, int width, int height, int stride, int[] histArr)
        {
            histArr.Initialize();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width * 3; j += 3)
                {
                    int index = i * stride + j;
                    histArr[pt[index]]++;
                }
            }
        }

        // Otsu helper function, Compute q values
        // Gets the sum of some histogram values within an intensity range
        private static float Px(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += hist[i];

            return sum;
        }

        // Otsu helper function, Get the mean values in the equation
        // Gets weighted sum of histogram values in an intensity range
        private static float Mx(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += i * hist[i];

            return sum;
        }

        // Otsu helper function, Maximum element
        private static int FindMax(float[] vec, int n) // Returns index of maximum float value in array
        {
            float maxVec = 0;
            int idx = 0;
            int i;
            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }

            return idx;
        }

        // Otsu's threshold
        private static int GetOtsuThreshold(Bitmap bmp)
        {
            float[] vet = new float[256];
            int[] hist = new int[256];
            vet.Initialize();

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            unsafe
            {
                byte* p = (byte*)bmData.Scan0.ToPointer();
                GetHistogram(p, bmp.Width, bmp.Height, bmData.Stride,
                    hist); // Fills up an array with pixel intensity values
                // loop through all possible threshold values and maximize between-class variance
                for (int k = 1; k != 255; k++)
                {
                    var p1 = Px(0, k, hist);
                    var p2 = Px(k + 1, 255, hist);
                    // Continually sums up histogram values in different ranges, covering the span of the image data, in two float values p1, p2
                    var p12 = p1 * p2;
                    if (p12 == 0)
                        p12 = 1;
                    float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                    vet[k] = diff * diff /
                             p12; // Computes and stores variance values for each threshold value using simple variance formula from statistics.
                    //vet[k] = (float)Math.Pow((Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1), 2) / p12; // Another way to compute variance (more overhead/overly complex)
                }
            }

            bmp.UnlockBits(bmData);

            return (byte)FindMax(vet, 256); // Finds maximum variance value
        }
        
        public static Bitmap GetOptimalThreshold(Bitmap bmp)
        {
            int th = GetOtsuThreshold(bmp);
            return Binarize(bmp, th);
        }

        public static Bitmap Binarize(Bitmap bmp, int thresholdValue)
        {
            const int maxVal = 256;
            if (thresholdValue <= 0 || thresholdValue >= maxVal) return null;

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            int size = bmp.Height * bmp.Width;

            int height = bmp.Height;
            int width = bmp.Width;
            int stride = bmpData.Stride;
            int offset = stride - bmp.Width;
            unsafe
            {
                byte* pt = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        *pt = (byte)(*pt < thresholdValue ? 0 : 255);
                        pt += 1;
                    }

                    pt += offset;
                }
            }

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static ColorPalette DefineGrayPalette(Bitmap b)
        {
            ColorPalette palette = b.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }

            return palette;
        }

        public static Bitmap ConcatenateHorizontally(Bitmap bmp, Bitmap bmp2)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            BitmapData bmData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height),
                ImageLockMode.ReadOnly, bmp2.PixelFormat);

            int combWidth = bmp.Width + bmp2.Width;
            int combHeight = Math.Max(bmp.Height, bmp2.Height);
            int minH = Math.Min(bmp.Height, bmp2.Height);
            Bitmap concBmp = new Bitmap(combWidth, combHeight, PixelFormat.Format8bppIndexed);

            concBmp.Palette = DefineGrayPalette(concBmp);

            BitmapData concData = concBmp.LockBits(new Rectangle(0, 0, concBmp.Width, concBmp.Height),
                ImageLockMode.WriteOnly, concBmp.PixelFormat);

            int width = bmp.Width;
            int height = bmp.Height;

            int offset1 = bmData.Stride - bmp.Width;
            int offset2 = bmData2.Stride - bmp2.Width;
            int offsetC = concData.Stride - concBmp.Width;

            unsafe
            {
                byte* p = (byte*)bmData.Scan0.ToPointer();
                byte* p2 = (byte*)bmData2.Scan0.ToPointer();
                byte* pC = (byte*)concData.Scan0.ToPointer();

                for (int y = 0; y < combHeight; y++)
                {
                    for (int x = 0; x < combWidth; x++)
                    {
                        if (x < width && y < height)
                        {
                            *pC = *p;
                            p += 1;
                        }
                        else if (x >= width && y < bmp2.Height)
                        {
                            *pC = *p2;
                            p2 += 1;
                        }
                        else
                        {
                            *pC = 255;
                        }

                        pC += 1;
                    }

                    p += offset1;
                    p2 += offset2;
                    pC += offsetC;
                }
            }

            bmp.UnlockBits(bmData);
            bmp2.UnlockBits(bmData2);
            concBmp.UnlockBits(concData);
            return concBmp;
        }

        public static Bitmap ConcatenateVertically(Bitmap bmp, Bitmap bmp2)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, bmp.PixelFormat);
            BitmapData bmData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height),
                ImageLockMode.ReadOnly, bmp2.PixelFormat);
            int combWidth = Math.Max(bmp.Width, bmp2.Width);
            int combHeight = bmp.Height + bmp2.Height;
            Bitmap concBitmap = new Bitmap(combWidth, combHeight, PixelFormat.Format8bppIndexed);
            concBitmap.Palette = DefineGrayPalette(concBitmap);
            BitmapData concData = concBitmap.LockBits(new Rectangle(0, 0, concBitmap.Width, concBitmap.Height),
                ImageLockMode.WriteOnly, concBitmap.PixelFormat);

            int width = bmp.Width;
            int height = bmp.Height;

            int stride1 = bmData.Stride;
            int stride2 = bmData2.Stride;
            int strideC = concData.Stride;

            int offset1 = stride1 - bmp.Width;
            int offset2 = stride2 - bmp2.Width;
            int offsetC = strideC - concBitmap.Width;

            unsafe
            {
                byte* p = (byte*)bmData.Scan0.ToPointer();
                byte* p2 = (byte*)bmData2.Scan0.ToPointer();
                byte* pC = (byte*)concData.Scan0.ToPointer();

                for (int y = 0; y < combHeight; y++)
                {
                    for (int x = 0; x < combWidth; x++)
                    {
                        if (y < height)
                        {
                            *pC = *p;
                            p += 1;
                        }
                        else if (y >= height)
                        {
                            *pC = *p2;
                            p2 += 1;
                        }
                        pC += 1;
                    }
                    if (y < height) p += offset1;
                    else p2 += offset2;
                    pC += offsetC;
                }
            }

            bmp.UnlockBits(bmData);
            bmp2.UnlockBits(bmData2);
            concBitmap.UnlockBits(concData);
            return concBitmap;
        }
        
        public static Bitmap Convert24To8(Bitmap bitmap)
        {
            Bitmap newB = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format8bppIndexed);
            newB.Palette = DefineGrayPalette(newB);
            BitmapData b1 = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData b2 = newB.LockBits(new Rectangle(0, 0, newB.Width, newB.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = b1.Stride;
            int stride8 = b2.Stride;
            int offset = stride - bitmap.Width * 3;
            int offset8 = stride8 - newB.Width; // 1

            unsafe
            {
                byte* ptr = (byte*)b1.Scan0.ToPointer();
                byte* ptr8 = (byte*)b2.Scan0.ToPointer();

                for (int i = 0; i < bitmap.Height; i++)
                {
                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        *ptr8 = (byte)(.299 * ptr[0] + .587 * ptr[1] + .114 * ptr[2]); // Weighted average formula

                        ptr += 3;
                        ptr8++;
                    }

                    ptr += offset;
                    ptr8 += offset8;
                }
            }

            bitmap.UnlockBits(b1);
            newB.UnlockBits(b2);
            Console.WriteLine($"New Bitmap has {newB.PixelFormat} Pixelformat");
            return newB;
        }

        public static Bitmap Convert1To8(Bitmap bitmap1)
        {
            Bitmap bitmap8 = new Bitmap(bitmap1.Width, bitmap1.Height, PixelFormat.Format8bppIndexed);
            bitmap8.Palette = DefineGrayPalette(bitmap8);
            BitmapData bmp1data = bitmap1.LockBits(new Rectangle(0, 0, bitmap1.Width, bitmap1.Height),
                ImageLockMode.ReadOnly, bitmap1.PixelFormat);
            BitmapData bmp8data = bitmap8.LockBits(new Rectangle(0, 0, bitmap8.Width, bitmap8.Height),
                ImageLockMode.WriteOnly, bitmap8.PixelFormat);

            int height = bitmap1.Height;
            int width = bitmap1.Width;
            int stride1 = bmp1data.Stride;
            int stride8 = bmp8data.Stride;

            unsafe
            {
                byte* ptr1 = (byte*)bmp1data.Scan0.ToPointer();
                byte* ptr8 = (byte*)bmp8data.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index1 = stride1 * y + x / 8;
                        int index8 = stride8 * y + x;

                        byte b = (byte)(ptr1[index1] & (0x80 >> (x % 8)));
                        ptr8[index8] = (byte)(b > 0 ? 255 : 0);
                    }
                }
            }

            bitmap1.UnlockBits(bmp1data);
            bitmap8.UnlockBits(bmp8data);
            Console.WriteLine($"New Bitmap has {bitmap8.PixelFormat} Pixelformat");
            return bitmap8;
        }
    }
}