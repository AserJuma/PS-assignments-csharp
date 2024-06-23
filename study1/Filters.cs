using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace study1
{
    public static class BitmapOperations
    {
        public static bool Convert2GrayScaleFast(Bitmap bmp)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();
                int stopAddress = (int)p + bmData.Stride * bmData.Height;
                while ((int)p != stopAddress)
                {
                    p[0] = (byte)(.299 * p[2] + .587 * p[1] + .114 * p[0]);
                    p[1] = p[0];
                    p[2] = p[0];
                    p += 3;
                }
            }
            bmp.UnlockBits(bmData);
            return true;
        }

        public static bool ConcatenateHorizontally(Bitmap bmp, Bitmap bmp2)
        {
            return true;
        }

        public static bool ConcatenateVertically(Bitmap bmp, Bitmap bmp2)
        {
            return true;
        }
    }
}
