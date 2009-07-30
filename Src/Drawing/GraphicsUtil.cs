using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using RT.Util.Collections;
using System.Drawing.Drawing2D;
using System;

namespace RT.Util.Drawing
{
    /// <summary>
    /// Contains static methods for various graphics-related operations.
    /// </summary>
    public static class GraphicsUtil
    {
        /// <summary>
        /// Blends the specified colors together. Amount specifies how much
        /// of the Color to keep, "on top of" the BackColor.
        /// </summary>
        public static Color ColorBlend(Color color, Color backColor, double amount)
        {
            byte r = (byte) ((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte) ((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte) ((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Draws the specified Image into the destination rectangle DestRect of the Graphics object g using the specified Opacity.
        /// </summary>
        /// <param name="g">Graphics object to alpha-blend the image onto.</param>
        /// <param name="image">Image to draw.</param>
        /// <param name="destRect">Destination rectangle within the target Graphics canvas.</param>
        /// <param name="opacity">Opacity level to use when drawing the image. 0 means nothing changes.
        /// 1 means the image is drawn normally. 0.5 means a 50% blend between source and destination.</param>
        public static void DrawImageAlpha(Graphics g, Image image, Rectangle destRect, float opacity)
        {
            ColorMatrix matrix = new ColorMatrix(new float[][] {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, opacity, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            ImageAttributes attr = new ImageAttributes();
            attr.SetColorMatrix(matrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);
            g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
        }

        /// <summary>
        /// Returns a <see cref="GraphicsPath"/> object that represents a rounded rectangle.
        /// </summary>
        /// <param name="rectangle">Position of the rectangle.</param>
        /// <param name="radius">Radius of the rounding of each corner of the rectangle.</param>
        public static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
        {
            return RoundedRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, radius);
        }

        /// <summary>
        /// Returns a <see cref="GraphicsPath"/> object that represents a rounded rectangle.
        /// </summary>
        /// <param name="x">Left edge of the rounded rectangle.</param>
        /// <param name="y">Top edge of the rounded rectangle.</param>
        /// <param name="width">Width of the rounded rectangle.</param>
        /// <param name="height">Height of the rounded rectangle.</param>
        /// <param name="radius">Radius of the rounding of each corner of the rectangle.</param>
        public static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            if (width <= 0)
                throw new ArgumentException("'width' must be positive.", "width");
            if (height <= 0)
                throw new ArgumentException("'height' must be positive.", "height");
            if (radius <= 0)
                throw new ArgumentException("'radius' must be positive.", "radius");
            if (width < 2 * radius || height < 2 * radius)
                throw new ArgumentException("'radius' is too large to fit into the width and/or height.", "radius");

            GraphicsPath g = new GraphicsPath();
            g.AddArc(x, y, 2 * radius, 2 * radius, 180, 90);
            g.AddArc(x + width - 2 * radius, y, 2 * radius, 2 * radius, 270, 90);
            g.AddArc(x + width - 2 * radius, y + height - 2 * radius, 2 * radius, 2 * radius, 360, 90);
            g.AddArc(x, y + height - 2 * radius, 2 * radius, 2 * radius, 450, 90);
            return g;
        }
    }
}
