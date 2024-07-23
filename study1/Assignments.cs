using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
// ReSharper disable MemberCanBePrivate.Global

namespace study1
{
    
    public class Assignments
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
                    float p1 = Px(0, k, hist);
                    float p2 = Px(k + 1, 255, hist);
                    // Continually sums up histogram values in different ranges, covering the span of the image data, in two float values p1, p2
                    float p12 = p1 * p2;
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

        private static Bitmap MeanBinarize(Bitmap bitmap)
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
            Bitmap convBmp = (Bitmap)bmp.Clone(); 
            Bitmap convBmp2 = (Bitmap)bmp2.Clone(); 
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                convBmp = ChangePixelFormat(bmp);
            if (bmp2.PixelFormat != PixelFormat.Format8bppIndexed)
                convBmp2 = ChangePixelFormat(bmp2);
            if (!direction) {convBmp.RotateFlip(RotateFlipType.Rotate90FlipNone);convBmp2.RotateFlip(RotateFlipType.Rotate90FlipNone);}
            
            BitmapData bmData = convBmp.LockBits(new Rectangle(0, 0, convBmp.Width, convBmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bmData2 = convBmp2.LockBits(new Rectangle(0, 0, convBmp2.Width, convBmp2.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            int combWidth = convBmp.Width + convBmp2.Width;
            int combHeight =  Math.Max(convBmp.Height, convBmp2.Height);

            Bitmap concBmp = new Bitmap(combWidth, combHeight, PixelFormat.Format8bppIndexed);
            concBmp.Palette = DefineGrayPalette(concBmp);
            BitmapData concData = concBmp.LockBits(new Rectangle(0, 0, combWidth, combHeight),
                ImageLockMode.WriteOnly, concBmp.PixelFormat);
            int width = convBmp.Width;
            int width2 = convBmp2.Width;
            
            int height = convBmp.Height;
            int height2 = convBmp2.Height;
            
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
                        else if (x >= width && y < height2)
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

            convBmp.UnlockBits(bmData);
            convBmp2.UnlockBits(bmData2);
            concBmp.UnlockBits(concData);
            if (!direction) {concBmp.RotateFlip(RotateFlipType.Rotate270FlipNone);}
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
        
        private static Bitmap ChangePixelFormat(Bitmap bmp)
        {
            PixelFormat pFormat = bmp.PixelFormat;
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
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData bmpData2 = bmp2.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
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
        }

        private static Bitmap WBR(Bitmap bitmap)
        {
            /*
             if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed) return ChangePixelFormat(bitmap);
            */
            int width = bitmap.Width; int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            
            int x = width; int y = height;
            int right = 0; int bottom = 0; // new image coord ( x, y, wDiff, hDiff);
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr != 255)
                        {
                            if (j < x) x = j;
                            if (i < y) y = i;
                            if (j >= right) right = j + 1;
                            if (i >= bottom) bottom = i + 1;
                        }
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            int wDiff = right - x;
            int hDiff = bottom - y;
            if (x < right && y < bottom) // true if there is something to remove
                return bitmap.Clone(new Rectangle(x, y, wDiff, hDiff), bitmap.PixelFormat);
            return bitmap;
        }

        private static Bitmap MedianFilter(Bitmap bitmap, int size)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            
            int widthOffset = width / size;
            List<byte> list = new List<byte>();
                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0.ToPointer();

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < widthOffset; j++)
                        {
                            for (int x = 0; x < size; x++)
                            {
                                for (int y = 0; y < size; y++)
                                    list.Add(*(ptr + y + stride * x));
                            }
                            list.Sort();
                            *ptr = list[size / 2];
                            list.Clear();
                            ptr += size;
                        }
                        ptr += offset;
                    }
                }

            bitmap.UnlockBits(bmpData);
            return bitmap;
        }

        private static Bitmap RescaleOneSide(Bitmap bitmap, int percent, bool dimension)
        {
            int width = bitmap.Width; int height = bitmap.Height;
            float scalar = (float)percent / 100;
            //bool bigger = scalar > 1 ? true : false;
            int nWidth = dimension ? (int)(width * scalar) : width;
            int nHeight = dimension ? height :(int)(height * scalar);
            
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;

            Bitmap scaledBmp = new Bitmap(nWidth, nHeight, PixelFormat.Format8bppIndexed);
            scaledBmp.Palette = DefineGrayPalette(scaledBmp);
            BitmapData nBmpData = scaledBmp.LockBits(new Rectangle(0, 0, nWidth, nHeight), 
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int nStride = nBmpData.Stride;
            int nOffset = nStride - width;
            
            float yRatio = (float)height / nHeight;
            float xRatio = (float)width / nWidth; // One is always 1
            
            unsafe
            {
                byte* oPtr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)nBmpData.Scan0.ToPointer();
                
                for (int i = 0; i < nHeight; i++)
                {
                    for (int j = 0; j < nWidth; j++)
                    {
                        int srcX = (int)(j * xRatio);
                        int srcY = (int)(i * yRatio);
                        
                        int srcIndex = srcY * stride + srcX; 
                        int dstIndex = i * nStride + j;
                        
                        nPtr[dstIndex] = oPtr[srcIndex];
                    }
                }
                
            }
            
            //Console.WriteLine(sWidth);
            //Console.WriteLine(sHeight);
            bitmap.UnlockBits(bmpData);
            scaledBmp.UnlockBits(nBmpData);
            return scaledBmp;
        }
        
        private static Bitmap RescaleBothSides(Bitmap bitmap, int percent)
        {
            int width = bitmap.Width; int height = bitmap.Height;
            float scalar = (float)percent / 100;
            int nWidth = (int)(width * scalar);
            int nHeight = (int)(height * scalar);
            
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), 
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            Bitmap scaledBmp = new Bitmap(nWidth, nHeight, PixelFormat.Format8bppIndexed);
            scaledBmp.Palette = DefineGrayPalette(scaledBmp);
            BitmapData nBmpData = scaledBmp.LockBits(new Rectangle(0, 0, nWidth, nHeight), 
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int nStride = nBmpData.Stride;
            
            float yRatio = (float)height / nHeight;
            float xRatio = (float)width / nWidth;
            
            unsafe
            {
                byte* oPtr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)nBmpData.Scan0.ToPointer();
                
                for (int i = 0; i < nHeight; i++)
                {
                    for (int j = 0; j < nWidth; j++)
                    {
                        int srcX = (int)(j * xRatio);
                        int srcY = (int)(i * yRatio);
                        nPtr[i * nStride + j] = oPtr[srcY * stride + srcX];
                    }
                }
                
            }
            bitmap.UnlockBits(bmpData);
            scaledBmp.UnlockBits(nBmpData);
            return scaledBmp;
        }

        private static void DisposeOfBitmaps(Bitmap[] bitmaps)
        {
            if (bitmaps.Length > 1)
            {
                foreach (Bitmap b in bitmaps)
                    b.Dispose();
            }
            else
                bitmaps[0].Dispose();
        }

        private static void Isolate(Bitmap bitmap)
        {
            /*
             if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed) return ChangePixelFormat(bitmap);
            */
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;

            List<Rectangle> shapesBounds = new List<Rectangle>();
            List<Bitmap> shapes = new List<Bitmap>();
            bool[,] visited = new bool[height, width]; // def to false
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr != 255 && !visited[i, j]) 
                        {
                            int x = j; int right = j;
                            int y = i; int bottom = i;
                            //byte* ptrN = ptr;
                            // Expand the bounding box
                            for (int row = i; row < height; row++)
                            {
                                for (int col = j; col < width; col++)
                                {
                                    if (*ptr != 255 && !visited[row, col])
                                    {
                                        visited[row, col] = true;
                                        right = Math.Max(right, col);
                                        bottom = Math.Max(bottom, row);
                                    }
                                    else if (visited[row, col])
                                    {
                                        break;
                                    }
                                    ptr++;
                                }
                                ptr += offset ;
                            }
                            
                            if (right > x && bottom > y)
                            {
                                shapesBounds.Add(new Rectangle(x, y, right - x + 1, bottom - y + 1));
                            }
                        }
                        ptr++;
                    }
                    ptr += offset; 
                }
                bitmap.UnlockBits(bmpData);
                foreach (Rectangle rect in shapesBounds)
                {
                    Console.WriteLine(rect.Size);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        //Console.WriteLine("Entered");
                        //Bitmap b = bitmap.Clone(rect, PixelFormat.Format8bppIndexed);
                        //shapes.Add(b);
                    }
                }

                /*for (int i = 0; i < shapes.Count; i++)
                {
                    shapes[i].Save($"'iso_shape{i}.bmp", ImageFormat.Bmp);
                }*/
            }
        }

        private static IEnumerable<Point> checkNeighbors0(BitmapData bitmapData, int i, int j, int st)
        {
            List<Point> points = new List<Point>();
            Queue<Point> queue = new Queue<Point>();
            unsafe
            {
                bool Checked = false;
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();
                while (!Checked)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            if (*(ptr + (y + i)+ st * (x + j)) == 0)
                            {
                                //labels[i + y, j + x] = labelC;
                            }
                        }
                    }
                }
            }

            return points;
        }
        
        private static Rectangle findRect(BitmapData bitmapData, int i, int j, int st, bool[,] visited, int width, int height)
        {
            //Rectangle rect;
            bool flag = true;
            int w = width; int h = height;
            int right = 0; int bottom = 0;
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(i, j));
            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();
                while (flag)
                {
                    Point p = queue.Dequeue();
                    int pX = p.X;
                    int pY = p.Y;
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            int coordY = y + pY; int coordX = pX + j;
                            if (coordX >= 0 && coordX < j && coordY >= 0 && coordY < i)
                            {
                                if (*(ptr + (coordY) + st * (coordX)) == 0)
                                {
                                    if (!visited[coordY, coordX])
                                    {
                                        visited[coordY, coordX] = true;
                                        queue.Enqueue(new Point(coordY, coordX));
                                        if (coordX < w) w = coordX;
                                        if (coordX >= right) right = coordX + 1;
                                        if (coordY < h) h = coordY;
                                        if (coordY >= bottom) bottom = coordY + 1;
                                    }
                                    else
                                        flag = false;
                                }
                            }
                        }
                    }
                }
            }
            int wDiff = right - w;
            int hDiff = bottom - h;
            return new Rectangle(w, h, wDiff, hDiff);
        }
        
        private static void Isolate2(Bitmap bitmap)
        {
            /*
             if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed) return ChangePixelFormat(bitmap);
            */
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;

            //int labelCount = 1;
            //int[,] labels = new int[height, width];
            //Array.Clear(labels, 0, labels.Length);
            
            bool[,] visited = new bool[height, width];
            bool flag = true;
            List<Rectangle> shapesBounds = new List<Rectangle>();
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr == 0 && !visited[i, j])
                        {
                            /*while (flag)
                            {
                                int w = width; int h = height;
                                int right = 0; int bottom = 0;
                                for (int y = -1; y <= 1; y++)
                                {
                                    for (int x = -1; x <= 1; x++)
                                    {
                                        int ii = y + i;
                                        int jj = x + j;
                                        if (*(ptr + ii + stride * jj ) == 0)
                                        {
                                            if(!visited[ii, jj])
                                            {
                                                visited[ii, jj] = true;
                                                if (jj < w) w = jj;
                                                if (ii < h) h = ii;
                                                if (jj >= right) right = jj + 1;
                                                if (ii >= bottom) bottom = ii + 1;
                                            }
                                            else
                                            {
                                                flag = false;
                                            }
                                        }
                                    }
                                }
                            }*/
                            //shapesBounds.Add(findRect(bmpData, i, j, stride, visited, width, height));
                            while (flag)
                            {
                                int y = i; int x = j;
                                visited[y, x] = true;
                                for (int sI = -1; sI <= 1; sI++)
                                {
                                    for (int sJ = -1; sJ <= 1; sJ++)
                                    {
                                        int checkX = x + sJ; int checkY = y + sI;
                                        if (*ptr + (checkY * stride) + checkX == 0)
                                        {
                                            if (!visited[checkY, checkX])
                                            {
                                                x = checkX; y = checkY;
                                            }
                                            else
                                                flag = false;
                                        }
                                    }
                                }
                            }
                        }
                        ptr++;
                    }
                    ptr += offset; 
                }
            }
            bitmap.UnlockBits(bmpData);
            int c = 0;
            foreach (Rectangle rect in shapesBounds)
            {
                //Console.WriteLine(rect.Size);
                /*f (rect.Width > 0 && rect.Height > 0)
                {
                    //Console.WriteLine("Entered");
                    //Bitmap b = bitmap.Clone(rect, PixelFormat.Format8bppIndexed);
                    //shapes.Add(b);
                }*/
                c++;
            }
            Console.WriteLine(c);
            //return bitmap;
        }
        private static void Identify(Bitmap bitmap) // Under construction 
        {
            int width = bitmap.Width;
            int height = bitmap.Width;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            int counter = 0;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr == 255)
                            counter++;
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            Console.WriteLine(counter);
            Console.WriteLine(width * height);
            //Console.WriteLine(bitmap.PixelFormat);
        }
        private static void Test1(Bitmap bitmap) 
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            
            int[,] Gx = // Perwitt
            {
                { 1,  4,  1 },
                { 4,  -20,  4 },
                { 1,  4,  1 }
            };

            int[,] Gy =
            {
                {  1,  1,  1 },
                {  0,  0,  0 },
                { -1, -1, -1 }
            };
            
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            Bitmap outputBitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            outputBitmap.Palette = DefineGrayPalette(outputBitmap);
            BitmapData outputData = outputBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int counter = 0;
            int[,] magnitudeMatrix = new int[height, width];
            unsafe
            {
                IntPtr ptr = bmpData.Scan0;
                IntPtr outputPtr = outputData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int gx = 0;
                        int gy = 0;
                        
                        for (int i = -1; i < 2; i++)
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                byte* pixelPtr = (byte*)ptr + ((y + i) * stride) + (x + j);
                                int pixelValue = *pixelPtr;

                                gx += Gx[i+1, j+1] * pixelValue;
                                //gy += Gy[i+1, j+1] * pixelValue;
                            }
                        }
                        
                        int magnitude = (int)Math.Sqrt(gx * gx /*+ gy * gy*/);
                        //Console.WriteLine(magnitude);
                        //magnitude = Math.Min(255, magnitude);
                        
                        if (magnitude > 0)
                        {
                            // Set the pixel in the output bitmap
                            /*byte* outputPixelPtr = (byte*)outputPtr + (y * stride) + x;
                            *outputPixelPtr = (byte)magnitude;*/
                            //Console.WriteLine(magnitude);
                            magnitudeMatrix[y, x] = magnitude;
                            counter++;
                        }
                        else
                        {
                            // Set to black for white pixels
                            /*byte* outputPixelPtr = (byte*)outputPtr + (y * stride) + x;
                            *outputPixelPtr = 0;*/
                        }
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            outputBitmap.UnlockBits(outputData);
            Console.WriteLine(counter);
            outputBitmap.Save("sobel-ed_image.bmp", ImageFormat.Bmp);
        }
        private static void Corners_Harris(Bitmap bitmap) // HARRIS CORNER
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            int[,] ly = new int[3, 3];
            int[,] lx = new int[3, 3];
            double C = 0;
            
           
            int stride2 = bmpData.Stride;
            int offset2 = stride2 - width;
            
            unsafe
            {
                
                byte* ptr2 = (byte*)bmpData.Scan0.ToPointer();
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*(ptr + stride + 1) == 0)
                        {
                            int p1 = 0; int p2 = 0; int p3 = 0; int p4 = 0;
                            for (int y = 0; y < 3; y++)
                            {
                                for (int x = 0; x < 3; x++)
                                {
                                    ly[x, y] = (-1 * *(ptr + 1 + y + (stride*x)) + *(ptr + (stride*(2+x)) + 1 + y));
                                    lx[x, y] = (-1 * *(ptr + (stride * (x+1)) + y) + *(ptr + (stride * (x+1)) + y + 2));
                                }
                            }
                            for (int k = 0; k < 3; k++)
                            {
                                for (int l = 0; l < 3; l++)
                                {
                                    p1 += lx[l, k] * lx[l, k];
                                    p4 += ly[l, k] * ly[l, k];
                                    p2 += lx[l, k] * ly[l, k];
                                    p3 = p2;
                                }
                            }
                            C = (p1 * p4 - p2 * p3) - 0.04 * (p1 + p4) * (p1 * p4); // det(H) - k * trace(H)^2
                            if (C != 0)
                            {
                                *(ptr2 + stride * i + j + 1) = 100;
                            }
                        }
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            bitmap.Save("dotted.bmp", ImageFormat.Bmp);
        }
        private static Point[] Corners_Moravec(Bitmap bitmap) // Moravec CORNER
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            //int[] SSD = new int[8];
            List<Point> corners = new List<Point>();
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int min = int.MaxValue; // largest 32 bit integer 2,147,483,647
                        if (*ptr == 0)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    if (i == 0 && j == 0)
                                        continue;
                                    int SSD = 0;
                                    for (int dy = -1; dy <= 1; dy++)
                                    {
                                        for (int dx = -1; dx <= 1; dx++)
                                        {
                                            //int d = 
                                            //SSD += d * d;
                                        }
                                    }
                                    if (SSD < min)
                                    {
                                        min = SSD;
                                    }
                                }
                            }
                            /*if (min > 0)
                            {
                                corners.Add(new Point(x, y));
                            }*/
                            Console.WriteLine("Entered");
                            Console.WriteLine(min);
                        }  
                        //ptr++;
                    }
                    //ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            return corners.ToArray();
        }

        private static void Isolate3(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            bool[,] visit = new bool[height,width];
            int[,] labelM = new int[height, width];
            int currLabel = 1;
            Stack<Point> neighbors = new Stack<Point>();
            Stack<Point> neighborsOfNeighbors = new Stack<Point>();
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if(!visit[i,j])
                        {
                            if (*ptr == 0)
                            {
                                labelM[i, j] = currLabel; 
                                visit[i, j] = true;
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        if (j + dx >= 0 && j + dx < width && i + dy >= 0 && i + dy < height)
                                        {
                                            if (dx != 0 && dy != 0)
                                            {
                                                byte* neighbor = ptr + (j + dx) + ((i + dy) * stride);
                                                if (!visit[i + dy, j + dx])
                                                {
                                                    if (*neighbor == 0)
                                                    {
                                                        labelM[i + dy, j + dx] = currLabel;
                                                        neighbors.Push(new Point(i + dy, j + dx)); visit[i + dy, j + dx] = true;
                                                    }

                                                    //visit[i + dy, j + dx] = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                while (neighbors.Count != 0)
                                {
                                    Point p = neighbors.Pop();
                                    int pX = p.X;
                                    int pY = p.Y;
                                    for (int dy = -1; dy <= 1; dy++)
                                    {
                                        for (int dx = -1; dx <= 1; dx++)
                                        {
                                            if (pX + dx >= 0 && pX + dx < width && pY + dy >= 0 && pY + dy < height)
                                            {
                                                if (dx != 0 && dy != 0)
                                                {
                                                    byte* neighborN = ptr + (pX + dx) + ((pY + dy) * stride);
                                                    if (!visit[pY + dy, pX + dx])
                                                    {
                                                        if (*neighborN == 0)
                                                        {
                                                            labelM[pY + dy, pX + dx] = currLabel;
                                                            neighborsOfNeighbors.Push(new Point(i + dy, j + dx));
                                                            visit[pY + dy, pX + dx] = true;
                                                        }
                                                        //visit[pY + dy, pX + dx] = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            } //currLabel++;
                            //visit[i, j] = true;
                        }
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Console.Write(labelM[i, j].ToString().PadLeft(4));
                }
                Console.WriteLine(); 
            }
        }

        private static void Isolate4(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            //bool[,] visit = new bool[width,height];
            int[,] label = new int[width, height];
            int currLabel = 1;
            //Queue<Point> neighbors = new Queue<Point>();
            //Queue<Point> neighborsOfNeighbors = new Queue<Point>();
            int[] dx = { +1, 0, -1, 0 };
            int[] dy = { 0, +1, 0, -1 };
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (*ptr == 0)
                        {
                            
                            /*for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if(dx != 0 && dy != 0) // Dont check same pixel again
                                    {
                                        if(!visit[j + dx, i + dy])
                                        {
                                            if (*(ptr + dx + stride * dy) == 0 && mask[dy, dx] == 1)
                                            {
                                                neighbors.Push(new Point(j + dx, i + dy));
                                            }
                                            else if (*(ptr + dx + stride * dy) != 0 && mask[dy, dx] == 1)
                                            {
                                                visit[j + dx, i + dy] = true;
                                            }
                                        }
                                    }
                                }
                            }*/
                        } 
                        ptr++;
                    }
                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bmpData);
            
        }
        public static void Main()
        {
            Bitmap[] bitmapArray = Load_Bitmaps(Load_FilesNames());
            //Binarize(bitmapArray[0], 200)                     .Save("s_b.bmp"     , ImageFormat.Bmp);
            //MeanBinarize(bitmapArray[0])                      .Save("m_b.bmp"     , ImageFormat.Bmp);
            //Concatenate(bitmapArray[0], bitmapArray[1], false) .Save("conc.bmp"    , ImageFormat.Bmp);
            //Convert24To8(bitmapArray[0])                      .Save("24to8.bmp"   , ImageFormat.Bmp);
            //Convert1To8(bitmapArray[0])                       .Save("1to8.bmp"    , ImageFormat.Bmp);
            //AddPadding(bitmapArray[0], 15, 255)               .Save("padded.bmp"  , ImageFormat.Bmp);
            //Dilate(AddPadding(MeanBinarize(Convert24To8(bitmapArray[0])), 1, 0) /*, 3*/).Save("dilated.bmp", ImageFormat.Bmp);
            //Erode(AddPadding(MeanBinarize(Convert24To8(bitmapArray[0])), 1, 0) /*, 3*/).Save("eroded.bmp", ImageFormat.Bmp);
            //WhiteBoundaryRemovalATTEMPT2(bitmapArray[0]).Save("abc.bmp", ImageFormat.Bmp);
            //WBR(bitmapArray[0]).Save("WBR.bmp", ImageFormat.Bmp);
            //RescaleOneSide(bitmapArray[0], 50, false).Save("scaled.bmp", ImageFormat.Bmp);
            //RescaleBothSides(RescaleBothSides(bitmapArray[0], 30), 50).Save("scaledEq1.bmp", ImageFormat.Bmp);
            //RescaleBothSides(bitmapArray[0], 50).Save("scaledEq2.bmp", ImageFormat.Bmp);
            //Isolate(bitmapArray[0]);
            //Console.WriteLine(bitmapArray[0].PixelFormat);
            //MedianFilter(bitmapArray[0], 3).Save("median1-3.bmp", ImageFormat.Bmp);
            //MedianFilter(MedianFilter(MedianFilter(bitmapArray[0], 3), 3), 3).Save("median3-3.bmp", ImageFormat.Bmp);
            //MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(bitmapArray[0])))))))))).Save("median10-5.bmp", ImageFormat.Bmp);
            //MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(MedianFilter(bitmapArray[0])))))))))))))))))))).Save("median20-5.bmp", ImageFormat.Bmp);
            //Isolate(bitmapArray[0]);
            //Isolate2(AddPadding(bitmapArray[0], 1, 255));//.Save("iso-test.bmp", ImageFormat.Bmp);
            //Test1(AddPadding(bitmapArray[0], 1, 255));
            //FindContours(AddPadding(bitmapArray[0], 1, 255));//.Save("iso-test.bmp", ImageFormat.Bmp);)
            
            //Point[] points = Corners_Moravec(bitmapArray[0]);
            //Console.WriteLine(points.Length);
            /*foreach (Point p in points)
            {
                Console.WriteLine(p);
            }*/
            
            /*Bitmap b = bitmapArray[0];
            List<List <Point>> listInList = FindContours(b);
            Bitmap[] b2 = Split(b, listInList);
            
            for (int i = 0; i < b2.Length; i++)
            {
                b2[i].Save($"Shape{i+1}.jpg", ImageFormat.Jpeg);
            }*/
            Corners_Harris(bitmapArray[0]);
            
            //Isolate3(AddPadding(bitmapArray[0], 5, 255));
            
            DisposeOfBitmaps(bitmapArray);
        }
    }
}