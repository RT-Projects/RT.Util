using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using RT.Util;

namespace RT.KitchenSink.Drawing
{
    /// <summary>Contains a method to generate a plasma fractal.</summary>
    public static class PlasmaFractal
    {
        /// <summary>Generates a plasma fractal of the specified size.</summary>
        /// <param name="width">Width of the bitmap to generate.</param>
        /// <param name="height">Height of the bitmap to generate.</param>
        /// <param name="leftTop">Desired color of the top-left pixel.</param>
        /// <param name="rightTop">Desired color of the top-right pixel.</param>
        /// <param name="leftBottom">Desired color of the bottom-left pixel.</param>
        /// <param name="rightBottom">Desired color of the bottom-right pixel.</param>
        /// <param name="varyRLevel">A number between 0 and 8 specifying by how much to vary the Red channel. 0 means maximum variance, 8 means no variance.</param>
        /// <param name="varyGLevel">A number between 0 and 8 specifying by how much to vary the Green channel. 0 means maximum variance, 8 means no variance.</param>
        /// <param name="varyBLevel">A number between 0 and 8 specifying by how much to vary the Blue channel. 0 means maximum variance, 8 means no variance.</param>
        /// <returns>The generated plasma fractal as a Bitmap object.</returns>
        public static Bitmap Create(int width, int height, Color leftTop, Color rightTop, Color leftBottom, Color rightBottom, int varyRLevel, int varyGLevel, int varyBLevel)
        {
            Color?[][] arr = Enumerable.Range(0, width).Select(i => new Color?[height]).ToArray();

            arr[0][0] = leftTop;
            arr[width - 1][0] = rightTop;
            arr[0][height - 1] = leftBottom;
            arr[width - 1][height - 1] = rightBottom;

            drawPlasmaPart(arr, 0, 0, width - 1, height - 1, varyRLevel, varyGLevel, varyBLevel, leftTop, rightTop, leftBottom, rightBottom, width, height);

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            unsafe
            {
                var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int y = 0; y < height; y++)
                {
                    var b = (byte*) data.Scan0 + y * data.Stride;
                    for (int x = 0; x < width; x++)
                    {
                        var col = arr[x][y].Value;
                        b[4 * x] = col.B;
                        b[4 * x + 1] = col.G;
                        b[4 * x + 2] = col.R;
                        b[4 * x + 3] = 255;
                    }
                }
                bmp.UnlockBits(data);
            }
            return bmp;
        }

        private static int confine(int i) { return i <= 0 ? 0 : i >= 255 ? 255 : i; }

        private static Color addRandom(Color one, Color two, int levelR, int levelG, int levelB)
        {
            var varyR = Rnd.Next(-255, 256) >> levelR;
            var varyG = Rnd.Next(-255, 256) >> levelG;
            var varyB = Rnd.Next(-255, 256) >> levelB;

            int r = confine((one.R + two.R) / 2 + varyR);
            int g = confine((one.G + two.G) / 2 + varyG);
            int b = confine((one.B + two.B) / 2 + varyB);
            return Color.FromArgb(r, g, b);
        }

        private static Color averageColor(Color one, Color two, Color three, Color four)
        {
            int r = (one.R + two.R + three.R + four.R) / 4;
            int g = (one.G + two.G + three.G + four.G) / 4;
            int b = (one.B + two.B + three.B + four.B) / 4;
            return Color.FromArgb(r, g, b);
        }

        private static void drawPlasmaPart(Color?[][] canvas, int left, int top, int right, int bottom, int levelR, int levelG, int levelB, Color leftTop, Color rightTop, Color leftBottom, Color rightBottom, int width, int height)
        {
            if ((right - left <= 1) && (bottom - top <= 1))
                return;

            int centre = (left + right) / 2;
            int middle = (top + bottom) / 2;
            Color centreTop, leftMiddle, rightMiddle, centreBottom, centreMiddle;

            canvas[centre][top] = centreTop = canvas[centre][top] ?? addRandom(leftTop, rightTop, levelR, levelG, levelB);
            canvas[centre][bottom] = centreBottom = canvas[centre][bottom] ?? addRandom(leftBottom, rightBottom, levelR, levelG, levelB);
            canvas[left][middle] = leftMiddle = canvas[left][middle] ?? addRandom(leftTop, leftBottom, levelR, levelG, levelB);
            canvas[right][middle] = rightMiddle = canvas[right][middle] ?? addRandom(rightTop, rightBottom, levelR, levelG, levelB);
            canvas[centre][middle] = centreMiddle = canvas[centre][middle] ?? averageColor(centreTop, leftMiddle, rightMiddle, centreBottom);

            drawPlasmaPart(canvas, left, top, centre, middle, levelR + 1, levelG + 1, levelB + 1, leftTop, centreTop, leftMiddle, centreMiddle, width, height);
            drawPlasmaPart(canvas, centre, top, right, middle, levelR + 1, levelG + 1, levelB + 1, centreTop, rightTop, centreMiddle, rightMiddle, width, height);
            drawPlasmaPart(canvas, left, middle, centre, bottom, levelR + 1, levelG + 1, levelB + 1, leftMiddle, centreMiddle, leftBottom, centreBottom, width, height);
            drawPlasmaPart(canvas, centre, middle, right, bottom, levelR + 1, levelG + 1, levelB + 1, centreMiddle, rightMiddle, centreBottom, rightBottom, width, height);
        }
    }
}
