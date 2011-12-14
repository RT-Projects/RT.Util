using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace RT.Util.Drawing
{
    /// <summary>
    /// Contains static methods for various graphics-related operations.
    /// </summary>
    public static class GraphicsUtil
    {
        /// <summary>Blends the specified colors together.</summary>
        /// <param name="color">Color to blend onto the background color.</param>
        /// <param name="backColor">Color to blend the other color onto.</param>
        /// <param name="amount">How much of <paramref name="color"/> to keep, “on top of” <paramref name="backColor"/>.</param>
        /// <returns>The blended colors.</returns>
        public static Color ColorBlend(Color color, Color backColor, double amount)
        {
            byte r = (byte) ((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte) ((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte) ((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Draws the specified <paramref name="image"/> into the destination rectangle <paramref name="destRect"/> of the <paramref name="graphics"/> object using the specified <paramref name="opacity"/>.
        /// </summary>
        /// <param name="graphics">Graphics object to alpha-blend the image onto.</param>
        /// <param name="image">Image to draw.</param>
        /// <param name="destRect">Destination rectangle within the target Graphics canvas.</param>
        /// <param name="opacity">Opacity level to use when drawing the image. 0 means nothing changes.
        /// 1 means the image is drawn normally. 0.5 means a 50% blend between source and destination.</param>
        public static void DrawImageAlpha(this Graphics graphics, Image image, Rectangle destRect, float opacity)
        {
            ColorMatrix matrix = new ColorMatrix(new float[][] {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            ImageAttributes attr = new ImageAttributes();
            attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
        }

#if UNSAFE
        /// <summary>Modifies the current bitmap’s transparency layer by setting it to the data from another bitmap.</summary>
        /// <param name="source">The bitmap to be modified.</param>
        /// <param name="transparencyLayer">The bitmap containing the transparency channel. Must be the same size as the source bitmap.</param>
        /// <param name="channel">Which channel from <paramref name="transparencyLayer"/> to use: 0 for blue, 1 for green, 2 for red, and 3 for the alpha channel.</param>
        /// <param name="invert">If true, the selected channel is inverted.</param>
        /// <returns>A reference to the same bitmap that was modified.</returns>
        public static unsafe Bitmap SetTransparencyChannel(this Bitmap source, Bitmap transparencyLayer, int channel, bool invert = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (transparencyLayer == null)
                throw new ArgumentNullException("transparencyLayer");
            if (source.Width != transparencyLayer.Width || source.Height != transparencyLayer.Height)
                throw new ArgumentException("transparencyLayer must be the same size as opaqueLayer.");
            if (channel < 0 || channel > 3)
                throw new ArgumentException("channel must be 0, 1, 2 or 3.");

            var sBits = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var tBits = transparencyLayer.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < source.Height; y++)
            {
                byte* trn = (byte*) tBits.Scan0 + y * tBits.Stride;
                byte* src = (byte*) sBits.Scan0 + y * sBits.Stride;
                for (int x = 0; x < source.Width; x++)
                    src[4 * x + 3] = Math.Min(src[4 * x + 3], (byte) (invert ? 255 - trn[4 * x + channel] : trn[4 * x + channel]));
            }
            source.UnlockBits(sBits);
            transparencyLayer.UnlockBits(tBits);
            return source;
        }
#endif

        /// <summary>Returns a <see cref="GraphicsPath"/> object that represents a rounded rectangle.</summary>
        /// <param name="rectangle">Position of the rectangle.</param>
        /// <param name="radius">Radius of the rounding of each corner of the rectangle.</param>
        /// <param name="tolerant">If true, the radius is reduced if it is too large to fit into the specified size. Otherwise, an exception is thrown.</param>
        public static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius, bool tolerant = false)
        {
            return RoundedRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, radius, tolerant);
        }

        /// <summary>
        /// Returns a <see cref="GraphicsPath"/> object that represents a rounded rectangle.
        /// </summary>
        /// <param name="x">Left edge of the rounded rectangle.</param>
        /// <param name="y">Top edge of the rounded rectangle.</param>
        /// <param name="width">Width of the rounded rectangle.</param>
        /// <param name="height">Height of the rounded rectangle.</param>
        /// <param name="radius">Radius of the rounding of each corner of the rectangle.</param>
        /// <param name="tolerant">If true, the radius is reduced if it is too large to fit into the specified size. Otherwise, an exception is thrown.</param>
        public static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius, bool tolerant = false)
        {
            if (width <= 0)
                throw new ArgumentException("'width' must be positive.", "width");
            if (height <= 0)
                throw new ArgumentException("'height' must be positive.", "height");
            if (radius <= 0)
                throw new ArgumentException("'radius' must be positive.", "radius");
            if (width < 2 * radius || height < 2 * radius)
            {
                if (!tolerant)
                    throw new ArgumentException("'radius' is too large to fit into the width and/or height.", "radius");
                radius = Math.Min(width / 2, height / 2);
            }

            GraphicsPath g = new GraphicsPath();
            g.AddArc(x, y, 2 * radius, 2 * radius, 180, 90);
            g.AddArc(x + width - 2 * radius, y, 2 * radius, 2 * radius, 270, 90);
            g.AddArc(x + width - 2 * radius, y + height - 2 * radius, 2 * radius, 2 * radius, 360, 90);
            g.AddArc(x, y + height - 2 * radius, 2 * radius, 2 * radius, 450, 90);
            g.CloseFigure();
            return g;
        }

        /// <summary>Determines the largest font size at which the specified text fits into the specified maximum size in the specified font.</summary>
        /// <param name="graphics">Specifies the <see cref="Graphics"/> object to use when measuring the font size.</param>
        /// <param name="maximumSize">Maximum size (in pixels) the text should have.</param>
        /// <param name="fontFamily">The font to measure.</param>
        /// <param name="text">The text whose size mustn't exceed <paramref name="maximumSize"/>.</param>
        /// <param name="style">Font style to apply.</param>
        /// <param name="allowWordWrapping">True if the text is allowed to word-wrap within the specified bounds.</param>
        public static float GetMaximumFontSize(this Graphics graphics, SizeF maximumSize, FontFamily fontFamily, string text, FontStyle style = FontStyle.Regular, bool allowWordWrapping = false)
        {
            return GetMaximumFontSize(graphics, fontFamily, text, style, allowWordWrapping, maximumSize.Width, maximumSize.Height);
        }

        /// <summary>Determines the largest font size at which the specified text fits into the specified maximum size in the specified font.</summary>
        /// <param name="graphics">Specifies the <see cref="Graphics"/> object to use when measuring the font size.</param>
        /// <param name="fontFamily">The font to measure.</param>
        /// <param name="text">The text whose size mustn't exceed <paramref name="maxWidth"/> and <paramref name="maxHeight"/>.</param>
        /// <param name="style">Font style to apply.</param>
        /// <param name="allowWordWrapping">True if the text is allowed to word-wrap within the specified bounds.</param>
        /// <param name="maxWidth">Maximum width the text may have, or null if only the maximum height should apply. If <paramref name="allowWordWrapping"/> is true, this cannot be null.</param>
        /// <param name="maxHeight">Maximum width the text may have, or null if only the maximum width should apply. If <paramref name="maxWidth"/> is null, this cannot be null.</param>
        public static float GetMaximumFontSize(this Graphics graphics, FontFamily fontFamily, string text, FontStyle style = FontStyle.Regular, bool allowWordWrapping = false, float? maxWidth = null, float? maxHeight = null)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("text cannot be null or empty.", "text");
            if (maxWidth != null && maxWidth.Value < 1)
                throw new ArgumentException("Maximum width cannot be zero or negative.", "maxWidth");
            if (maxHeight != null && maxHeight.Value < 1)
                throw new ArgumentException("Maximum height cannot be zero or negative.", "maxHeight");
            if (maxWidth == null && maxHeight == null)
                throw new ArgumentException("maxWidth and maxHeight cannot both be null.", "maxHeight");
            if (maxWidth == null && allowWordWrapping)
                throw new ArgumentException("maxWidth cannot be null if allowWordWrapping is true.", "maxWidth");

            float low = 1;
            float? high = null;
            string[] words = allowWordWrapping ? Regex.Matches(text, @"\S+", RegexOptions.Singleline).Cast<Match>().Select(m => m.Value).ToArray() : null;
            while (high == null || high.Value - low > 0.1)
            {
                float trySize = high == null ? low + 1024 : (low + high.Value) / 2;
                var font = new Font(fontFamily, trySize, style);

                float width, height;
                if (allowWordWrapping)
                {
                    var sz = graphics.MeasureString(text, font, (int) maxWidth.Value);
                    width = sz.Width;
                    height = sz.Height;
                    foreach (var word in words)
                    {
                        var sz2 = graphics.MeasureString(word, font);
                        if (sz2.Width > width)
                            width = sz2.Width;
                        if (sz2.Height > height)
                            height = sz2.Height;
                    }
                }
                else
                {
                    var sz = graphics.MeasureString(text, font);
                    width = sz.Width;
                    height = sz.Height;
                }

                if ((maxWidth != null && width > maxWidth.Value) || (maxHeight != null && height > maxHeight.Value))
                    high = trySize;
                else
                    low = trySize;
            }
            return low;
        }

        /// <summary>Sets the interpolation mode, smoothing mode, text rendering hint and compositing quality for the
        /// specified Graphics object to high quality.</summary>
        public static void SetHighQuality(this Graphics g)
        {
            g.InterpolationMode = InterpolationMode.High;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
        }

        /// <summary>Returns a rectangle that has the same aspect ratio as <paramref name="fitWhat"/> but fits into <paramref name="fitInto"/>.</summary>
        /// <param name="fitWhat">Specifies the aspect ratio of the desired rectangle.</param>
        /// <param name="fitInto">The rectangle into which to fit the result rectangle.</param>
        /// <returns>The result rectangle which fits into <paramref name="fitInto"/>.</returns>
        public static Rectangle FitIntoMaintainAspectRatio(this Size fitWhat, Rectangle fitInto)
        {
            int x, y, w, h;

            if ((double) fitWhat.Width / (double) fitWhat.Height > (double) fitInto.Width / (double) fitInto.Height)
            {
                w = fitInto.Width;
                x = fitInto.Left;
                h = (int) ((double) fitWhat.Height / (double) fitWhat.Width * (double) fitInto.Width);
                y = fitInto.Top + fitInto.Height / 2 - h / 2;
            }
            else
            {
                h = fitInto.Height;
                y = fitInto.Top;
                w = (int) ((double) fitWhat.Width / (double) fitWhat.Height * (double) fitInto.Height);
                x = fitInto.Left + fitInto.Width / 2 - w / 2;
            }

            return new Rectangle(x, y, w, h);
        }
    }
}
