using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace study1
{
    public class Assignments
    {
        private static String[] Load_FilesNames()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Bitmap files (*.bmp)|*.bmp|Jpeg files (*.jpg)|*.jpg|Tiff files (*.tiff)|*.tiff|PNG files (*.png)|*.png|All valid files (*.bmp/*.tiff/*.jpg/*.png)|*.bmp;*.tiff;*.jpg;*.png";
            openFileDialog.FilterIndex = 5;
            openFileDialog.RestoreDirectory = false;
            return DialogResult.OK != openFileDialog.ShowDialog() ? null : openFileDialog.FileNames;
        }
        
        private static Bitmap[] Load_Verify_Bitmaps(string[] bitmapFilenames)
        {
            if (bitmapFilenames == null) throw new NullReferenceException("No image(s) was chosen!");
            Bitmap[] bitmaps = new Bitmap[bitmapFilenames.Length];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                bitmaps[i] = new Bitmap(bitmapFilenames[i]);
                //if (bitmaps[i].Width == 0 || bitmaps[i].Height == 0) throw new Exception($"Bitmap w/ path: {bitmapFilenames[i]} is broken");
            }
            return bitmaps;
        }
        
        
        private static void Main()
        {
            Bitmap[] bitmapArray = Load_Verify_Bitmaps( Load_FilesNames());
            //Ops.Binarize(bitmapArray[0], 100)                     .Save("s_b.bmp"     , ImageFormat.Bmp);
            //Ops.MeanBinarize(bitmapArray[0])                      .Save("m_b.bmp"     , ImageFormat.Bmp);
            //Ops.Concatenate(bitmapArray[0], bitmapArray[1], true) .Save("conc.bmp"    , ImageFormat.Bmp);
            //Ops.Convert24To8(bitmapArray[0])                      .Save("24to8.bmp"   , ImageFormat.Bmp);
            //Ops.Convert1To8(bitmapArray[0])                       .Save("1to8.bmp"    , ImageFormat.Bmp);
            //Ops.AddPadding(bitmapArray[0]).Save("padded.bmp", ImageFormat.Bmp);
        }
    }
}
// TODO: Check input validation