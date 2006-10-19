using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace RT.Util
{
    public class BitmapUtil
    {
        public static void DrawImageAlpha(Graphics g, Image Image, Rectangle DestRect, float Opacity)
        {
            ColorMatrix ColorMatrix = new ColorMatrix(new float[][] {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, Opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            ImageAttributes ImageAttributes = new ImageAttributes();
            ImageAttributes.SetColorMatrix(ColorMatrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);
            g.DrawImage(Image, DestRect, 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, ImageAttributes);
        }
    }
}
