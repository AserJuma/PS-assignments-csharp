using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
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
            return palette; // Is there a better way?
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
        private static Bitmap Convert8To24(Bitmap bitmap8)
        {
            if (bitmap8.PixelFormat != PixelFormat.Format8bppIndexed) throw new Exception("Error: Bitmap not 8bit");
            int height = bitmap8.Height;
            int width = bitmap8.Width;
            Bitmap bitmap24 = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            //bitmap8.Palette = DefineGrayPalette(bitmap8);
            BitmapData b1 = bitmap8.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            BitmapData b2 = bitmap24.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int stride = b1.Stride;
            int stride24 = b2.Stride;
            int offset = stride - bitmap8.Width;
            int offset24 = stride24 - bitmap24.Width * 3;

            unsafe
            {
                byte* ptr = (byte*)b1.Scan0.ToPointer();
                byte* ptr24 = (byte*)b2.Scan0.ToPointer();

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        ptr24[0] = *ptr;
                        ptr24[1] = ptr24[0];
                        ptr24[2] = ptr24[0];
                        ptr24 += 3;
                        ptr++;
                    }
                    ptr += offset;
                    ptr24 += offset24;
                }
            }

            bitmap8.UnlockBits(b1);
            bitmap24.UnlockBits(b2);
            Console.WriteLine($"New Bitmap has {bitmap24.PixelFormat} Pixel format from 8bit");
            return bitmap24;
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
            int stride = bmpData.Stride;
            int pstride = pBmpData.Stride;
            unsafe
            {
                byte* ptr1 = (byte*)pBmpData.Scan0.ToPointer();
                
                for (int i = 0; i < pHeight; i++, ptr1+=pOffset)
                {
                    for (int j = 0; j < pWidth; j++, ptr1++)
                        *ptr1 = color;
                }
                
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)pBmpData.Scan0.ToPointer() + pstride * thickness;

                for (int i = 0; i < oHeight; i++, ptr += oOffset)
                {
                    for (int j = 0; j < oWidth; j++, ptr++)
                    {
                        *(nPtr + i  * pstride + thickness/2 + j + thickness/2) = *ptr;
                    }
                }
            }

            bitmap.UnlockBits(bmpData);
            paddedBmp.UnlockBits(pBmpData);
            return paddedBmp;
        }
        private static Bitmap AddPadding1(Bitmap bitmap, int thickness, byte color) // BROKEN 
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
            int stride = pBmpData.Stride;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)pBmpData.Scan0.ToPointer();

                for (int i = 0; i < pHeight; i++)
                {
                    for (int j = 0; j < pWidth; j++)
                    {
                        //if (i < thickness || j < thickness || j >= pWidth - thickness || i >= pHeight - thickness)
                        *nPtr = color;
                        /*else
                        {
                            *nPtr = *ptr;
                            ptr++;
                        }*/

                        nPtr++;
                    }

                    //ptr += oOffset;
                    nPtr += pOffset;
                }
                byte* fPtr = (byte*)pBmpData.Scan0.ToPointer() + stride + 1;
                for (int i = 1; i < oHeight - 1; i++)
                {
                    for (int j = 1; j < oWidth - 1; j++)
                    {
                        //if (i < thickness || j < thickness || j >= pWidth - thickness || i >= pHeight - thickness)
                        *fPtr = *ptr;
                        /*else
                        {
                            *nPtr = *ptr;
                            
                        }*/
                        ptr++;
                        fPtr++;
                    }
                    ptr += oOffset;
                    fPtr += pOffset;
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
                ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            
            int x = width; int y = height;
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
        private static Bitmap Dilate(Bitmap bitmap /*, int size*/)
        {
            byte[,] sElement = new byte[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sElement[i, j] = (byte)(i == 1 || j == 1 ? 1 : 0);
                }
            }

            int oWidth = bitmap.Width;
            int oHeight = bitmap.Height;
            Bitmap dilatedBmp = new Bitmap(oWidth, oHeight, PixelFormat.Format8bppIndexed);
            dilatedBmp.Palette = DefineGrayPalette(dilatedBmp);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData dilatedBmpData = dilatedBmp.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = bmpData.Stride - oWidth;
            int nOffset = dilatedBmpData.Stride - oWidth;


            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)dilatedBmpData.Scan0.ToPointer();

                for (int i = 0; i < oHeight; i++)
                {
                    for (int j = 0; j < oWidth; j++)
                    {
                        *nPtr = 255;
                        for (int x = 0; x < 3; x++)
                        {
                            for (int y = 0; y < 3; y++)
                            {
                                if (j < 2 || j >= oWidth - 2 || i < 2 || i >= oHeight - 2) 
                                    *nPtr = 255;
                                else if (*(ptr + y + stride * x) == 0 && sElement[x, y] == 1)
                                {
                                    *nPtr = 0;
                                    break;
                                }
                            }

                            if (*nPtr == 0) break;
                        }

                        ptr++;
                        nPtr++;
                    }

                    ptr += offset;
                    nPtr += nOffset;
                }
            }

            bitmap.UnlockBits(bmpData);
            dilatedBmp.UnlockBits(dilatedBmpData);
            return dilatedBmp;
        }

        private static Bitmap Erode(Bitmap bitmap /*, int size*/)
        {
            byte[,] sElement = new byte[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sElement[i, j] = (byte)(i == 1 || j == 1 ? 1 : 0);
                }
            }

            int oWidth = bitmap.Width;
            int oHeight = bitmap.Height;
            Bitmap erodedBmp = new Bitmap(oWidth, oHeight, PixelFormat.Format8bppIndexed);
            erodedBmp.Palette = DefineGrayPalette(erodedBmp);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            BitmapData erodedBmpData = erodedBmp.LockBits(new Rectangle(0, 0, oWidth, oHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = bmpData.Stride - oWidth;
            int nOffset = erodedBmpData.Stride - oWidth;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                byte* nPtr = (byte*)erodedBmpData.Scan0.ToPointer();

                for (int i = 0; i < oHeight; i++)
                {
                    for (int j = 0; j < oWidth; j++)
                    {
                        *nPtr = 0;
                        for (int x = 0; x < 3; x++)
                        {
                            for (int y = 0; y < 3; y++)
                            {
                                if (*(ptr + y + stride * x) > 128 && sElement[x, y] == 0)
                                {
                                    *nPtr = 255;
                                    break;
                                }
                            }

                            if (*nPtr == 255) break;
                        }

                        ptr++;
                        nPtr++;
                    }

                    ptr += offset;
                    nPtr += nOffset;
                }
            }

            bitmap.UnlockBits(bmpData);
            erodedBmp.UnlockBits(erodedBmpData);
            return erodedBmp;
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
        private static Bitmap Sobel(Bitmap bitmap) //Sobel
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            Bitmap outputBitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            outputBitmap.Palette = DefineGrayPalette(outputBitmap);
            BitmapData outputData = outputBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            int stride = bmpData.Stride;
            int[,] Gx = new int[3, 3]
            {
                { -1,  0,  1 },
                { -2,  0,  2 },
                { -1,  0,  1 }
            };

            int[,] Gy = new int[3, 3]
            {
                {  1,  2,  1 },
                {  0,  0,  0 },
                { -1, -2, -1 }
            };
            
            unsafe
            {
                IntPtr ptr = bmpData.Scan0;
                IntPtr outputPtr = outputData.Scan0;
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int gx = 0; int gy = 0;
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                byte* pixelPtr = (byte*)ptr + ((y + j) * stride) + (x + i);
                                int pixelValue = *pixelPtr;
                                gx += Gx[i + 1, j + 1] * pixelValue;
                                gy += Gy[i + 1, j + 1] * pixelValue;
                            }
                        }
                        int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                        magnitude = Math.Min(255, magnitude);
                        byte* outputPixelPtr = (byte*)outputPtr + (y * stride) + x;
                        *outputPixelPtr = (byte)(magnitude > 0 ? magnitude : 0);
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            outputBitmap.UnlockBits(outputData);
            return outputBitmap;
        }
        private static void Corners_Harris(Bitmap bitmap) 
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
            bitmap.Save("harris_corners.bmp", ImageFormat.Bmp);
        }
        private static void Corners_Harris2(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                                                 PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            
            List<double> R_values = new List<double>();
            
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int Ix = -*(ptr + (y - 1) * stride + (x - 1)) - 2 * (*(ptr + y * stride + (x - 1))) - *(ptr + (y + 1) * stride + (x - 1))
                                 + *(ptr + (y - 1) * stride + (x + 1)) + 2 * (*(ptr + y * stride + (x + 1))) + *(ptr + (y + 1) * stride + (x + 1));

                        int Iy = -*(ptr + (y - 1) * stride + (x - 1)) - 2 * (*(ptr + (y - 1) * stride + x)) - *(ptr + (y - 1) * stride + (x + 1))
                                 + *(ptr + (y + 1) * stride + (x - 1)) + 2 * (*(ptr + (y + 1) * stride + x)) + *(ptr + (y + 1) * stride + (x + 1));
                        
                        int IxIx = Ix * Ix;
                        int IyIy = Iy * Iy;
                        int IxIy = Ix * Iy;

                       
                        double detM = IxIx * IyIy - IxIy * IxIy;
                        double traceM = IxIx + IyIy;
                        double R = detM - 0.04 * (traceM * traceM);
                        
                        R_values.Add(R); 

                        
                       
                        double mean_R = R_values.Average();
                        double std_R = CalculateStandardDeviation(R_values);
                        double k = 1.5;
                        double threshold = mean_R + k * std_R;
                        
                        if (Math.Abs(R) > threshold)
                        {
                            *(ptr + y * stride + x) = 100; 
                        }
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            bitmap.Save("harris_corners2.bmp", ImageFormat.Bmp);
        }
        private static void Corners_Harris3(Bitmap bitmap, int num)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                                                 PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            List<double> R_values = new List<double>();
            int count = 0;
            double[,] R_valuesM = new double[width, height];
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (*(ptr + y * stride + x) == 0)
                        {
                            int Ix = -*(ptr + (y - 1) * stride + (x - 1)) - 2 * (*(ptr + y * stride + (x - 1))) - *(ptr + (y + 1) * stride + (x - 1))
                                     + *(ptr + (y - 1) * stride + (x + 1)) + 2 * (*(ptr + y * stride + (x + 1))) + *(ptr + (y + 1) * stride + (x + 1));
                            
                            int Iy = -*(ptr + (y - 1) * stride + (x - 1)) - 2 * (*(ptr + (y - 1) * stride + x)) - *(ptr + (y - 1) * stride + (x + 1))
                                     + *(ptr + (y + 1) * stride + (x - 1)) + 2 * (*(ptr + (y + 1) * stride + x)) + *(ptr + (y + 1) * stride + (x + 1));
                            
                            int IxIx = Ix * Ix;
                            int IyIy = Iy * Iy;
                            int IxIy = Ix * Iy;
                           
                            double detM = IxIx * IyIy - IxIy * IxIy;
                            double traceM = IxIx + IyIy;
                            double R = detM - 0.04 * (traceM * traceM);
                            R_values.Add(R);
                            
                            double mean_R = R_values.Average();
                            double std_R = CalculateStandardDeviation(R_values);
                            double k = 1.5; // Gives different results, increments of 1.5
                            double threshold = mean_R + k * std_R;
                            if (Math.Abs(R) > threshold)
                            {
                                *(ptr + y * stride + x) = 100;
                                count++;
                            }
                        }
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            
            //Non-maximal suppression
            /*for (int i = 1; i < height - 1; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    double currVal = R_values[j, i];
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if(dx != 0 && dy != 0)
                            {
                                if (R_values[j + dx, i + dy] > currVal)
                                {
                                    R_values[j, i] = R_values[j + dx, i + dy];
                                }
                                else
                                {
                                    R_values[j + dx, i + dy] = 0;
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Console.Write(R_values[j,i]);
                }
                Console.WriteLine();
                if (R_values[j, i] > 0)
                    count++;
            }*/
            /*string shapeName = "unknown";
            if (count == 4 || count == 8) // Based on observed corners, TODO: FIX -- 2 cases break this
                shapeName = "rectangle";
            else if(count == 3 || count == 6 || count == 7)
                shapeName = "triangle";
            else if (count > 100  && count < 250)
                shapeName = "circle";
            bitmap.Save($"{shapeName}-shape-{num}.bmp", ImageFormat.Bmp);*/
            //Console.WriteLine($"{num}: {count}");
            bitmap.Save($"{num}: {count}.bmp", ImageFormat.Bmp);
        }
        private static double CalculateStandardDeviation(List<double> values)
        {
            double mean = values.Average();
            double sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
            double standardDeviation = Math.Sqrt(sumOfSquares / values.Count);
            return standardDeviation;
        }
        private static Point[] Corners_Moravec(Bitmap bitmap) // Not done 
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

        private static Bitmap GaussianBlur(Bitmap bitmap) // Not done
        {
            int [,] GBKernel =
            {
                {1, 4, 1}, 
                {4, -20, 4}, 
                {1, 4, 1}
            };
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer(); 
                for (int y = 0; y < height; y++, ptr += offset)
                {
                    for (int x = 0; x < width; x++, ptr++)
                    {
                        if (x < 1 || x >= width - 1 || y < 1 || y >= height - 1) 
                            *ptr = 0;
                        else
                        {
                            //int sum = 0;
                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    //*ptr = (byte)(GBKernel[i, j] * (*(ptr + (i * stride) + j)));
                                }
                            }

                            /*if (sum > 255)
                                sum = 255;
                            else if (sum < 0)
                                sum = 0;
                            *ptr = (byte)sum;*/
                        }
                    }
                }
            }
            return bitmap;
        }
        
        public static Bitmap Laplace(Bitmap bitmap) 
        {
            int [,] laplaceK =
            {
                {1, 2, 1}, 
                {2, -20, 2}, 
                {1, 2, 1}
            };

            int width = bitmap.Width; int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (x < 1 || x >= width - 1 || y < 1 || y >= height - 1) 
                            *ptr = 0;
                        else
                        {
                            int sum = 0;
                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    sum += laplaceK[i, j] * (*(ptr + (i * stride) + j));
                                }
                            }

                            if (sum > 255)
                                sum = 255;
                            else if (sum < 0)
                                sum = 0;
                            *ptr = (byte)sum;
                        }

                        ptr++;
                    }
                    ptr += offset;
                }
            }

            
            bitmap.UnlockBits(bmpData);
            return bitmap;
        }
        
        private static Bitmap Invert(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                for (int i = 0; i < height; i++, ptr += offset)
                {
                    for (int j = 0; j < width; j++, ptr++)
                        *ptr = (byte)(*ptr == 0 ? 255 : 0);
                }
            }
            bitmap.UnlockBits(bmpData);
            return bitmap;
        }

        private static void B_FAST(Bitmap bitmap, int num) // Based on Features from accelerated segment test
        {
            // If 1 & 9 are Black. NOT A CORNER
            // Else if 1 & 5 & 13 are BLACK OR WHITE || 9 & 5 & 13 are BLACK OR WHITE. IS A CORNER
            // More checks exist: 3, 7, 11, 15
            // 2, 4 / 6, 8 / 10, 12 / 14, 16
            
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bmpData.Stride;
            int offset = stride - width;
            //bool[,] isC = new bool[width, height];
            int count = 0;
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer() + stride * 3 + 3;
                for (int i = 3; i < height - 3; i++, ptr += offset)
                {
                    for (int j = 3; j < width - 3; j++, ptr++)
                    {
                        if (*ptr == 0)
                        {
                                byte* north = ptr - stride * 3; byte* east = ptr + 3; byte* south = ptr + stride * 3; byte* west = ptr - 3;
                                byte* NorthEast = ptr - stride * 2 + 2; byte* SouthEast = ptr + stride * 2 + 2; byte* SouthWest = ptr + stride * 2 - 2; byte* NorthWest = ptr - stride * 2 - 2;
                            
                            if (!(*north == 0 && *south == 0) && !(*east == 0 && *west == 0)
                                                              && !(*NorthEast == 0 && *SouthWest == 0) && !(*NorthWest == 0 && *SouthEast == 0)
                                )
                            {
                                if (*north == 0 && *north == *east ||  *south == 0 && *south == *east || *north == 0 && *north == *west
                                    || *south == 0 && *south == *west || *NorthWest == 0 && *NorthWest == *SouthWest || *NorthEast == 0 && *NorthEast == *SouthEast
                                    || *NorthWest == 0 && *NorthWest == *NorthEast || *SouthWest == 0 && *SouthWest == *SouthEast)
                                {
                                    count++;
                                    //*ptr = 128;
                                    //isC[j, i] = true;
                                    //Console.WriteLine($"[{j}, {i}] N: {*north}, S: {*south}, E: {*east}, W: {*west}");
                                }
                            }
                        }
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            //Console.WriteLine(count);
            //bitmap.Save("test1.bmp", ImageFormat.Bmp);
            string shapeName = "unknown";
            if (count == 3 || count == 4)
                shapeName = height == width ? "square" : "rectangle";
            else if(count == 1 || count == 2)
                shapeName = "triangle";
            else if (count == 0)
                shapeName = "circle";
            /*if(count == 8 || count > 4)
                FAST(RemoveOuterShape(bitmap), num);
            else*/
                bitmap.Save($"{shapeName}-{num}.bmp", ImageFormat.Bmp);
        }
        private static void Isolate(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            bool[,] visit = new bool[width, height];
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bitmapData.Stride;
            int offset = stride - width;
            int shapeCount = 0;
            unsafe
            {
                
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();
                byte* ptrN = ptr;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (*ptr == 0 && !visit[x, y])
                        {
                            visit[x, y] = true;
                            List<Point> shape = new List<Point>();
                            Stack<Point> neighbors = new Stack<Point>();
                            neighbors.Push(new Point(x, y));
                            while (neighbors.Count != 0)
                            {
                                Point point = neighbors.Pop();
                                int currX = point.X; int currY = point.Y;
                                shape.Add(point);
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        int neighborX = currX + dx;
                                        int neighborY = currY + dy;
                                        if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                                        {
                                            if (*(ptrN + (neighborY * stride) + neighborX) == 0 &&
                                                !visit[neighborX, neighborY])
                                            {
                                                visit[neighborX, neighborY] = true;
                                                neighbors.Push(new Point(neighborX, neighborY));
                                            }
                                        }
                                    }
                                }
                            }
                            shapeCount++;
                            ProcessShape3(/*bitmap,*/ shape, shapeCount);
                        }
                        ptr++;
                    }

                    ptr += offset;
                }
            }

            bitmap.UnlockBits(bitmapData);
        }
        
        private static void ProcessShape3(/*Bitmap bitmap,*/ List<Point> shape, int shapeNumber)
        {
            int minX = int.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
             HashSet<Point> uniquePoints = new HashSet<Point>();
             /*Point minXy = new Point();
             Point maxXy = new Point();
             Point xMinY = new Point();
             Point xMaxY = new Point();*/
            foreach (Point p in shape)
            {
                int x = p.X, y = p.Y;
                if (x > maxX) {maxX = x;}// maxXy = p; }
                if (x < minX) {minX = x;}// minXy = p; } 
                if (y > maxY) {maxY = y;}// xMaxY = p; }
                if (y < minY) {minY = y;}// xMinY = p; }
            }
            //Console.WriteLine($"min x: [{minXy.X}, {minXy.Y}]"); Console.WriteLine($"max x: [{maxXy.X}, {maxXy.Y}]");
            //Console.WriteLine($"min y: [{xMinY.X}, {xMinY.Y}]");Console.WriteLine($"max y: [{xMaxY.X}, {xMaxY.Y}]");
            
            //uniquePoints.Add(maxXy); uniquePoints.Add(minXy); uniquePoints.Add(xMaxY); uniquePoints.Add(xMinY);
            int width = maxX - minX + 1, height = maxY - minY + 1;
            Bitmap bitmap1 = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData bitmapData = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            int stride = bitmapData.Stride;
            int offset = stride - width;
            int topCount = 0;
            int botCount = 0;
            int leftCount = 0;
            int rightCount = 0;
            bool sidesEqual = false;
            bool avgSideEqual = true;
            string shapeName = "unknown";
            unsafe
            {
                byte* ptr1 = (byte*)bitmapData.Scan0.ToPointer();
                byte* topPtr = (byte*)bitmapData.Scan0.ToPointer();
                byte* bottomPtr = (byte*)bitmapData.Scan0.ToPointer() + stride * (height - 1);
                byte* leftPtr = (byte*)bitmapData.Scan0.ToPointer(); // Overlap
                byte* rightPtr = (byte*)bitmapData.Scan0.ToPointer() + (width - 1);
                for (int i = 0; i < height; i++, ptr1+=offset)
                {
                    for (int j = 0; j < width; j++, ptr1++)
                        *ptr1 = 255;
                }
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();
                foreach (Point p in shape)
                {
                    int pX = p.X; int pY = p.Y;
                    *(ptr + pX - minX + (pY - minY) * stride) = 0;
                    
                }
                for (int i = 0; i < width; i++, topPtr++, bottomPtr++)
                {
                    if (*topPtr == 0)
                        topCount++;
                    if (*bottomPtr == 0)
                        botCount++;
                }
                for (int i = 0; i < height; i++, leftPtr += stride, rightPtr += stride)
                {
                    if (*leftPtr == 0)
                        leftCount++;
                    if (*rightPtr == 0)
                        rightCount++;
                }
            }
            
            bitmap1.UnlockBits(bitmapData);
            //int uniquePointsCount = uniquePoints.Count;
             //int error = 5;
            int[] sideLengths = new int[4];
            sideLengths[0] = topCount; sideLengths[1] = rightCount; sideLengths[2] = botCount; sideLengths[3] = leftCount;
            int avgSide = (botCount + topCount + rightCount + leftCount) / 4;
            
            if (botCount == topCount && botCount == rightCount && topCount == leftCount) sidesEqual = true;
            for (int i = 0; i < sideLengths.Length; i++)
                if (avgSide - 5 > sideLengths[i] || avgSide + 5 < sideLengths[i]) avgSideEqual = false;
            int largestDiff = sideLengths.Max() - sideLengths.Min();
            if (width == height)
            {
                if (sidesEqual || avgSideEqual)
                {
                    if (topCount == width || avgSide == width || topCount == 1)
                        shapeName = "square";
                    else shapeName = "circle";
                }
                else
                {
                    if (largestDiff > width / 2) shapeName = "triangle";
                    else shapeName = "square";
                }
            }
            else
            {
                if (topCount == width && leftCount == height && rightCount == leftCount)
                    shapeName = "rectangle";
                else if (largestDiff >= width - 3) shapeName = "triangle";
                else shapeName = "rotated triangle";
            }
            
            
            //Console.WriteLine("num:" + shapeNumber);
            //Console.WriteLine(uniquePointsCount);
            //Console.WriteLine("---------------");
             /*
            Console.WriteLine($"Width: {width}");
            Console.WriteLine($"Top Count: {topPCount}");
            Console.WriteLine($"Bot Count: {botPCount}");
            Console.WriteLine($"Height: {height}");
            Console.WriteLine($"Left Count: {lefPCount}");
            Console.WriteLine($"Right Count: {rigPCount}");
            */
            //Console.WriteLine(shapeName + " " + "num:" + shapeNumber);
            //Console.WriteLine("---------------");
           
            
            bitmap1.Save($"{shapeName}-{shapeNumber}.bmp", ImageFormat.Bmp);
            
        }
        
        private static void FindShapes(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            bool[,] visit = new bool[width, height];
            Bitmap cBitmap = Convert8To24(bitmap);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int stride = bitmapData.Stride;
            int offset = stride - width;
            int shapeCount = 0;
            List<List<Point>> shapes = new List<List<Point>>();
            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();
                byte* ptrN = ptr;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (*ptr == 0 && !visit[x, y])
                        {
                            visit[x, y] = true;
                            List<Point> shape = new List<Point>();
                            Stack<Point> neighbors = new Stack<Point>();
                            neighbors.Push(new Point(x, y));
                            while (neighbors.Count != 0)
                            {
                                Point point = neighbors.Pop();
                                int currX = point.X; int currY = point.Y;
                                shape.Add(point);
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        int neighborX = currX + dx;
                                        int neighborY = currY + dy;
                                        if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                                        {
                                            if (*(ptrN + (neighborY * stride) + neighborX) == 0 &&
                                                !visit[neighborX, neighborY])
                                            {
                                                visit[neighborX, neighborY] = true;
                                                neighbors.Push(new Point(neighborX, neighborY));
                                            }
                                        }
                                    }
                                }
                            }
                            //shapeCount++;
                            //processShape3(shape, shapeCount);
                            shapes.Add(shape);
                        }
                        ptr++;
                    }

                    ptr += offset;
                }
            }
            bitmap.UnlockBits(bitmapData);
            
            foreach (List<Point> shape in shapes)
            {
                int minX = int.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                foreach (Point p in shape)
                {
                    int x = p.X, y = p.Y;
                    if (x > maxX) {maxX = x;}
                    if (x < minX) {minX = x;}
                    if (y > maxY) {maxY = y;}
                    if (y < minY) {minY = y;}
                }
                int widthB = maxX - minX + 1, heightB = maxY - minY + 1;
                BitmapData bmpData = bitmap.LockBits(new Rectangle(minX, minY, widthB, heightB), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                
                int stride1 = bmpData.Stride;
                int offset1 = stride1 - widthB;
                int topCount = 0;
                int botCount = 0;
                int leftCount = 0;
                int rightCount = 0;
                bool sidesEqual = false;
                bool avgSideEqual = true;
                string shapeName = "unknown";
                unsafe
                {
                    byte* topPtr = (byte*)bmpData.Scan0.ToPointer();
                    byte* bottomPtr = (byte*)bmpData.Scan0.ToPointer() + stride1 * (heightB - 1);
                    byte* leftPtr = (byte*)bmpData.Scan0.ToPointer(); // Overlap
                    byte* rightPtr = (byte*)bmpData.Scan0.ToPointer() + (widthB - 1);
                    for (int i = 0; i < widthB; i++, topPtr++, bottomPtr++)
                    {
                        if (*topPtr == 0)
                            topCount++;
                        if (*bottomPtr == 0)
                            botCount++;
                    }
                    for (int i = 0; i < heightB; i++, leftPtr += stride1, rightPtr += stride1)
                    {
                        if (*leftPtr == 0)
                            leftCount++;
                        if (*rightPtr == 0)
                            rightCount++;
                    }
                    int[] sideLengths = new int[4];
                    sideLengths[0] = topCount; sideLengths[1] = rightCount; sideLengths[2] = botCount; sideLengths[3] = leftCount;
                    int avgSide = (botCount + topCount + rightCount + leftCount) / 4;
                    if (botCount == topCount && botCount == rightCount && topCount == leftCount) sidesEqual = true;
                    for (int i = 0; i < sideLengths.Length; i++)
                        if (avgSide - 5 > sideLengths[i] || avgSide + 5 < sideLengths[i]) avgSideEqual = false;
                    int largestDiff = sideLengths.Max() - sideLengths.Min();
                    if (widthB == heightB)
                    {
                        if (sidesEqual || avgSideEqual)
                        {
                            if (topCount == widthB || avgSide == widthB || topCount == 1)
                                shapeName = "square";
                            else shapeName = "circle";
                        }
                        else
                        {
                            if (largestDiff > widthB / 2) shapeName = "triangle";
                            else shapeName = "square";
                        }
                    }
                    else
                    {
                        if (topCount == widthB && leftCount == heightB && rightCount == leftCount)
                            shapeName = "rectangle";
                        //else if (largestDiff >= widthB - 3) shapeName = "triangle";
                        else shapeName = "triangle";
                    }
                }
                bitmap.UnlockBits(bmpData);
                
                BitmapData bmpData24 = cBitmap.LockBits(new Rectangle(minX-1, minY-1, widthB+10, heightB+10), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride2 = bmpData24.Stride;
                unsafe
                {
                    byte* topPtr2 = (byte*)bmpData24.Scan0.ToPointer();
                    byte* bottomPtr2 = (byte*)bmpData24.Scan0.ToPointer() + stride2 * (heightB - 1);
                    byte* leftPtr2 = (byte*)bmpData24.Scan0.ToPointer(); // Overlap
                    byte* rightPtr2 = (byte*)bmpData24.Scan0.ToPointer() + (widthB - 1) * 3;
                    for (int i = 0; i < widthB; i++, topPtr2+=3, bottomPtr2+=3)
                    {
                        topPtr2[0] = 0;topPtr2[1] = 0;topPtr2[2] = 255;
                        bottomPtr2[0] = 0;bottomPtr2[1] = 0;bottomPtr2[2] = 255;
                    }
                    for (int i = 0; i < heightB; i++, leftPtr2 += stride2, rightPtr2 += stride2)
                    {
                        leftPtr2[0] = 0;leftPtr2[1] = 0;leftPtr2[2] = 255;
                        rightPtr2[0] = 0;rightPtr2[1] = 0;rightPtr2[2] = 255;
                    }
                }
                
                Font font = new Font("Arial", 10);
                SolidBrush brush = new SolidBrush(Color.Red);
                using (Graphics g = Graphics.FromImage(cBitmap))
                {
                    g.DrawString(shapeName, font, brush, minX-8, minY-16);
                }
                
                cBitmap.UnlockBits(bmpData24);
            }
            cBitmap.Save("result.bmp", ImageFormat.Bmp);
        }
        
        
        public static void Main()
        {
            Bitmap[] bitmapArray = Load_Bitmaps(Load_FilesNames());
            
            FindShapes(MeanBinarize(bitmapArray[0]));
            
            DisposeOfBitmaps(bitmapArray);
        }
    }
}