using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local

namespace study1
{
    public static class Assignments
    {
        private static string[] Load_FilesNames()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter =
                "Bitmap files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|Tiff files (*.tiff)|*.tiff|PNG files (*.png)|*.png|All valid files (*.bmp/*.tiff/*.jpg/*.png)|*.bmp;*.tiff;*.jpg;*.png";
            openFileDialog.FilterIndex = 5;
            openFileDialog.RestoreDirectory = false;
            return DialogResult.OK != openFileDialog.ShowDialog() ? null : openFileDialog.FileNames;
        }

        private static Bitmap[] Load_Bitmaps(string[] bitmapFilenames)
        {
            if (bitmapFilenames == null) throw new NullReferenceException("No image(s) chosen!");
            Bitmap[] bitmaps = new Bitmap[bitmapFilenames.Length];
            for (int i = 0; i < bitmaps.Length; i++)
                bitmaps[i] = new Bitmap(bitmapFilenames[i]);
            return bitmaps;
        }

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
        private static byte GetOtsuThreshold(Bitmap bmp)
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
            Console.WriteLine("Otsu: " + FindMax(vet, 256));
            return (byte)FindMax(vet, 256); // Finds maximum variance value
        }

        private static Bitmap ChangePixelFormat(Bitmap bmp)
        {
            PixelFormat pFormat = bmp.PixelFormat;
            //Bitmap convertedBmp = new Bitmap(bmp.Width, bmp.Height);
            //Bitmap convertedBmp = (Bitmap)bmp.Clone();
            switch (pFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    return Convert1To8(bmp);
                case PixelFormat.Format24bppRgb:
                    return Convert24To8(bmp);
                default:
                    throw new Exception("Default case entered in ChangePixelFormat(), not 1bpp or 24 bpp");
            }
        }

        private static Bitmap Binarize(Bitmap bmp, int thresholdValue)
        {
            Bitmap cBmp = (Bitmap)bmp.Clone();
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                cBmp = ChangePixelFormat(bmp);

            BitmapData bmpData = cBmp.LockBits(new Rectangle(0, 0, cBmp.Width, cBmp.Height), ImageLockMode.ReadWrite,
                cBmp.PixelFormat);

            int height = cBmp.Height;
            int width = cBmp.Width;
            int stride = bmpData.Stride;
            int offset = stride - cBmp.Width;
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

            cBmp.UnlockBits(bmpData);
            return cBmp;
        }

        public static Bitmap MeanBinarize(Bitmap bitmap)
        {
            byte th = GetOtsuThreshold(bitmap);
            return Binarize(bitmap, th);
        }

        private static ColorPalette DefineGrayPalette(Bitmap bmp)
        {
            ColorPalette palette = bmp.Palette;
            for (int i = 0; i < 256; i++)
                palette.Entries[i] = Color.FromArgb(i, i, i);
            return palette; //TODO: Is there a better way?
        }

        private static Bitmap Concatenate(Bitmap bmp, Bitmap bmp2, bool direction)
        {
            Bitmap cbmp1 = (Bitmap)bmp.Clone(); //new Bitmap(bmp.Width, bmp.Height);
            Bitmap cbmp2 = (Bitmap)bmp2.Clone(); //new Bitmap(bmp2.Width, bmp2.Height);
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                cbmp1 = ChangePixelFormat(bmp);
            if (bmp2.PixelFormat != PixelFormat.Format8bppIndexed)
                cbmp2 = ChangePixelFormat(bmp2);

            BitmapData bmData = cbmp1.LockBits(new Rectangle(0, 0, cbmp1.Width, cbmp1.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bmData2 = cbmp2.LockBits(new Rectangle(0, 0, cbmp2.Width, cbmp2.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            int combWidth = direction ? cbmp1.Width + cbmp2.Width : Math.Max(cbmp1.Width, cbmp2.Width);
            int combHeight = direction ? Math.Max(cbmp1.Height, cbmp2.Height) : cbmp1.Height + cbmp2.Height;

            Bitmap concBmp = new Bitmap(combWidth, combHeight, PixelFormat.Format8bppIndexed);
            concBmp.Palette = DefineGrayPalette(concBmp);
            BitmapData concData = concBmp.LockBits(new Rectangle(0, 0, concBmp.Width, concBmp.Height),
                ImageLockMode.WriteOnly, concBmp.PixelFormat);

            int width = cbmp1.Width;
            int width2 = cbmp2.Width;
            int height = cbmp1.Height;
            int height2 = cbmp2.Height;
            int stride1 = bmData.Stride;
            int stride2 = bmData2.Stride;
            int strideC = concData.Stride;
            int offset1 = stride1 - width;
            int offset2 = stride2 - width2;
            int offsetC = strideC - combWidth;

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
                        else if (direction ? (x >= width && y < height2) : (x < width2 && y >= height))
                        {
                            *pC = *p2;
                            p2 += 1;
                        }

                        pC += 1;
                    }

                    p += offset1;
                    p2 += offset2;
                    pC += offsetC;
                }
            }

            cbmp1.UnlockBits(bmData);
            cbmp2.UnlockBits(bmData2);
            concBmp.UnlockBits(concData);
            return concBmp;
        }

        private static Bitmap Convert24To8(Bitmap bitmap24)
        {
            if (bitmap24.PixelFormat != PixelFormat.Format24bppRgb) throw new Exception("Error: Bitmap not 24bit");

            Bitmap bitmap8 = new Bitmap(bitmap24.Width, bitmap24.Height, PixelFormat.Format8bppIndexed);
            bitmap8.Palette = DefineGrayPalette(bitmap8);
            BitmapData b1 = bitmap24.LockBits(new Rectangle(0, 0, bitmap24.Width, bitmap24.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            BitmapData b2 = bitmap8.LockBits(new Rectangle(0, 0, bitmap8.Width, bitmap8.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            int stride = b1.Stride;
            int stride8 = b2.Stride;
            int offset = stride - bitmap24.Width * 3;
            int offset8 = stride8 - bitmap8.Width;

            int height = bitmap24.Height;
            int width = bitmap24.Width;

            unsafe
            {
                byte* ptr = (byte*)b1.Scan0.ToPointer();
                byte* ptr8 = (byte*)b2.Scan0.ToPointer();

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        *ptr8 = (byte)(.299 * ptr[0] + .587 * ptr[1] + .114 * ptr[2]); // Weighted average formula
                        ptr += 3;
                        ptr8++;
                    }

                    ptr += offset;
                    ptr8 += offset8;
                }
            }

            bitmap24.UnlockBits(b1);
            bitmap8.UnlockBits(b2);
            Console.WriteLine($"New Bitmap has {bitmap8.PixelFormat} Pixel format from 24bit");
            return bitmap8;
        }

        private static Bitmap Convert1To8(Bitmap bitmap1)
        {
            if (bitmap1.PixelFormat != PixelFormat.Format1bppIndexed) throw new Exception("Error: Bitmap not 1bit");
            Bitmap bitmap8 = new Bitmap(bitmap1.Width, bitmap1.Height, PixelFormat.Format8bppIndexed);
            bitmap8.Palette = DefineGrayPalette(bitmap8);
            BitmapData bmp1data = bitmap1.LockBits(new Rectangle(0, 0, bitmap1.Width, bitmap1.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
            BitmapData bmp8data = bitmap8.LockBits(new Rectangle(0, 0, bitmap8.Width, bitmap8.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            int height = bitmap1.Height;
            int width = bitmap1.Width;
            int stride1 = bmp1data.Stride;
            int stride8 = bmp8data.Stride;
            //int offset1 = stride1 - width;
            int offset8 = stride8 - width;

            unsafe
            {
                byte* ptr1 = (byte*)bmp1data.Scan0.ToPointer();
                byte* ptr8 = (byte*)bmp8data.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte b = (byte)((*(ptr1 + (x / 8)) >> (7 - (x % 8))) & 0x01);
                        *ptr8 = (byte)(b > 0 ? 255 : 0);
                        ptr8++;
                    }

                    ptr1 += stride1;
                    ptr8 += offset8;
                }
            }

            bitmap1.UnlockBits(bmp1data);
            bitmap8.UnlockBits(bmp8data);
            Console.WriteLine($"New Bitmap has {bitmap8.PixelFormat} Pixel format from 1bit");
            return bitmap8;
        }

        private static Bitmap AddPadding(Bitmap bitmap, int thickness, byte color)
        {
            /*Bitmap bitmap = (Bitmap)o_bitmap.Clone(); //(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);) // New empty bitmap
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                bitmap = ChangePixelFormat(o_bitmap);*/
            int oWidth = bitmap.Width;
            int oHeight = bitmap.Height;
            int pWidth = oWidth + thickness * 2;
            int pHeight = oHeight + thickness * 2;
            Bitmap paddedBmp = new Bitmap(pWidth, pHeight, PixelFormat.Format8bppIndexed);
            paddedBmp.Palette = DefineGrayPalette(paddedBmp);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData pBmpData = paddedBmp.LockBits(new Rectangle(0, 0, pWidth, pHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int oOffset = bmpData.Stride - oWidth;
            int pOffset = pBmpData.Stride - pWidth;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)pBmpData.Scan0.ToPointer();

                for (int i = 0; i < pHeight; i++)
                {
                    for (int j = 0; j < pWidth; j++)
                    {
                        if (i < thickness || j < thickness || j >= pWidth - thickness || i >= pHeight - thickness)
                            *nPtr = color;
                        else
                        {
                            *nPtr = *ptr;
                            ptr++;
                        }

                        nPtr++;
                    }

                    ptr += oOffset;
                    nPtr += pOffset;
                }
            }

            bitmap.UnlockBits(bmpData);
            paddedBmp.UnlockBits(pBmpData);
            return paddedBmp;
        }

        private static Bitmap RemoveWhiteBoundaryATTEMPT1(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            int counter = 0;
            /*byte[,] sElement = new byte[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sElement[i, j] = (byte)(i == 1 || j == 1 ? 1 : 0);
                }
            }*/

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (i == j && *ptr == 255)
                            counter++;
                        ptr++;
                    }

                    ptr += offset;
                }
            }

            int x = counter / 2;
            int newW = width - counter;
            int newH = height - counter;
            bitmap.UnlockBits(bmpData);
            return bitmap.Clone(new Rectangle(x, x, newW, newH), bitmap.PixelFormat);
        }

        private static Bitmap WhiteBoundaryRemovalATTEMPT2(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            Bitmap bmp2 = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            bmp2.Palette = DefineGrayPalette(bmp2);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            BitmapData bmpData2 = bmp2.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            unsafe
            {
                byte* topL = (byte*)bmpData.Scan0.ToPointer();
                byte* topR = topL + width - 1;
                byte* botL = topL + stride * (height - 1);
                byte* botR = topL + stride * (height - 1) + width - 1;
                
                byte* topL2 = (byte*)bmpData2.Scan0.ToPointer();
                byte* topR2 = topL2 + width - 1;
                byte* botL2 = topL2 + stride * (height - 1);
                byte* botR2 = topL2 + stride * (height - 1) + width - 1;
                if (width == height)
                {
                    for (int j = 0; j < height / 2; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            if (*topL != 255) *topL2 = (byte)(*topL == 0 ? 255 : *topL);
                            if (*topR != 255) *topR2 = (byte)(*topR == 0 ? 255 : *topR);
                            if (*botL != 255) *botL2 = (byte)(*botL == 0 ? 255 : *botL);
                            if (*botR != 255) *botR2 = (byte)(*botR == 0 ? 255 : *botR);

                            topL++; //OK
                            topR += stride;
                            botL -= stride;
                            botR--; //OK

                            topL2++; //OK
                            topR2 += stride;
                            botL2 -= stride;
                            botR2--; //OK
                        }

                        topL += offset; //OK
                        topR--; topR -= stride * (height); //Ok-ish
                        botL++; botL += (stride) * (height); //Ok-ish
                        botR -= offset; //OK
                        
                        topL2 += offset; //OK
                        topR2--; topR2 -= stride * (height); //Ok-ish
                        botL2++; botL2 += (stride) * (height); //Ok-ish
                        botR2 -= offset; //Ok
                        
                    }
                }
                else
                {
                    Console.WriteLine("Different w & h");
                    for (int i = 0; i < height / 2; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            if (*topR != 255) *topR2 = (byte)(*topR == 0 ? 255 : *topR);
                            if (*botL != 255) *botL2 = (byte)(*botL == 0 ? 255 : *botL);
                            topR += stride;
                            botL -= stride;
                            topR2 += stride;
                            botL2 -= stride;
                        }
                        topR--; topR -= stride * (height);
                        botL++; botL += (stride) * (height);
                        topR2--; topR2 -= stride * (height);
                        botL2++; botL2 += (stride) * (height);
                    }

                    for (int i = 0; i < width / 2; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            if (*topL != 255) *topL2 = (byte)(*topL == 0 ? 255 : *topL);
                            if (*botR != 255) *botR2 = (byte)(*botR == 0 ? 255 : *botR);
                            topL++;
                            topL2++;
                            botR--;
                            botR2--;
                        }
                        topL += offset;
                        botR -= offset;
                        topL2 += offset;
                        botR2 -= offset;
                    }
                }
                
            }
            bitmap.UnlockBits(bmpData);
            bmp2.UnlockBits(bmpData2);
            return bmp2;
            /*Console.WriteLine(counter1);
            Console.WriteLine(counter2);
            Console.WriteLine(counter3);
            Console.WriteLine(counter4);*/
        }

        private static Bitmap WBR(Bitmap bitmap)
        {
            int width = bitmap.Width; int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            int left = width; int top = height;
            int right = 0; int bottom = 0; 
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr != 255)
                        {
                            if (j < left) left = j;
                            if (i < top) top = i;
                            if (j >= right) right = j + 1;
                            if (i >= bottom) bottom = i + 1;
                        }
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            if (left < right && top < bottom)
                return bitmap.Clone(new Rectangle(left, top, right - left, bottom - top), bitmap.PixelFormat);
            return null;
        }
        public static void Main()
        {
            Bitmap[] bitmapArray = Load_Bitmaps(Load_FilesNames());
            //Binarize(bitmapArray[0], 200)                     .Save("s_b.bmp"     , ImageFormat.Bmp);
            //MeanBinarize(bitmapArray[0])                      .Save("m_b.bmp"     , ImageFormat.Bmp);
            //Concatenate(bitmapArray[0], bitmapArray[1], true) .Save("conc.bmp"    , ImageFormat.Bmp);
            //Convert24To8(bitmapArray[0])                      .Save("24to8.bmp"   , ImageFormat.Bmp);
            //Convert1To8(bitmapArray[0])                       .Save("1to8.bmp"    , ImageFormat.Bmp);
            //AddPadding(bitmapArray[0], 15, 255)               .Save("padded.bmp"  , ImageFormat.Bmp);
            //Dilate(AddPadding(MeanBinarize(Convert24To8(bitmapArray[0])), 1, 0) /*, 3*/).Save("dilated.bmp", ImageFormat.Bmp);
            //Erode(AddPadding(MeanBinarize(Convert24To8(bitmapArray[0])), 1, 0) /*, 3*/).Save("eroded.bmp", ImageFormat.Bmp);
            //Console.WriteLine(bitmapArray[0].PixelFormat);
            //WhiteBoundaryRemovalATTEMPT2(bitmapArray[0]).Save("WBR.bmp", ImageFormat.Bmp);
            WBR(bitmapArray[0]).Save("test123.bmp", ImageFormat.Bmp);
        }
    }
}