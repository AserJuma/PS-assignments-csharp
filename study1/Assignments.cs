using System;
using System.Drawing;
using System.Windows.Forms;

namespace study1
{
    public class Assignments
    {
        private static String[] Load_Files()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Bitmap files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|Tiff files (*.tiff)|*.tiff|PNG files (*.png)|*.png|All valid files (*.bmp/*.tiff/*.jpg/*.png)|*.bmp;*.tiff;*.jpg;*.png";
            openFileDialog.FilterIndex = 5;
            openFileDialog.RestoreDirectory = false;
            return DialogResult.OK != openFileDialog.ShowDialog() ? null : openFileDialog.FileNames;
        }
        
        private static Bitmap[] Load_Verify(string[] bitmapFilenames)
        {
            if (bitmapFilenames == null) throw new NullReferenceException("No image(s) was chosen!");
            Bitmap[] bitmaps = new Bitmap[bitmapFilenames.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                bitmaps[i] = new Bitmap(bitmapFilenames[i]);
                if (bitmaps[i].Width == 0 || bitmaps[i].Height == 0) throw new Exception($"Bitmap w/ path: {bitmapFilenames[i]} is broken");
            }
            return bitmaps;
        }
        
        
        private static void Main()
        {
            //Bitmap[] verifiedBitmaps = Load_Verify( Load_Files());
            //BitmapOperations.Binarize((Bitmap)verifiedBitmaps[0].Clone(), 100).Save("/home/aser/RiderProjects/study1/study1/sb_bmp.bmp", ImageFormat.Bmp);
            //BitmapOperations.GetOptimalThreshold((Bitmap)verifiedBitmaps[0].Clone()).Save("/home/aser/RiderProjects/study1/study1/mb_bmp.bmp", ImageFormat.Bmp);
            //BitmapOperations.ConcatenateHorizontally(verifiedBitmaps[0], verifiedBitmaps[1]).Save("/home/aser/RiderProjects/study1/study1/Hconc_bmp.bmp", ImageFormat.Bmp);
            //BitmapOperations.Convert24To8(verifiedBitmaps[0]).Save("/home/aser/RiderProjects/study1/study1/24to8.bmp",ImageFormat.Bmp);
            //BitmapOperations.Convert1To8(verifiedBitmaps[0]).Save("/home/aser/RiderProjects/study1/study1/1to8.bmp",ImageFormat.Bmp);
            // TODO: FINISH THIS:
            //BitmapOperations.ConcatenateVertically(verifiedBitmaps[0], verifiedBitmaps[1]).Save("/home/aser/RiderProjects/study1/study1/Vconc_bmp.bmp", ImageFormat.Bmp);
        }
    }
}

// TODO: Check input validations