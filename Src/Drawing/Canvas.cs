using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using RT.Util.Geometry;

namespace RT.Util.Drawing
{
    /// <summary>
    /// Encodes axes direction mode for <see cref="Canvas"/>.
    /// </summary>
    public enum CoordinateAxesDirection
    {
        /// <summary>X axis grows towards the right and Y axis grows downwards (normal computer screen coordinate direction)</summary>
        RightDown,
        /// <summary>X axis grows towards the right and Y axis grows upwards.</summary>
        RightUp
    }

    /// <summary>
    /// Wraps a <see cref="System.Drawing.Graphics"/> to provide a hopefully more convenient interface.
    /// The major bits of functionality are:
    /// <list type="bullet">
    /// <item>all coordinates in doubles or RT.Util.Geometry structs;</item>
    /// <item>support for the Y axis growing upwards;</item>
    /// <item>functions targeted at drawing a 2d world onto a viewport of a specified size.</item>
    /// </list>
    /// 
    /// <para>Common terms and abbreviations:
    /// <list type="table">
    /// <item><term>Screen</term><description>Rectangular area that will be the final destination of the drawing.</description></item>
    /// <item><term>Viewport</term><description>The region, defined in terms of the world coordinates, that is viewable on the screen.</description></item>
    /// <item><term>World coordinates</term><description>Coordinates of the underlying "world" being represented.</description></item>
    /// <item><term>Screen coordinates</term><description>Coordinates on the screen, can be used to directly draw on the underlying Graphics.</description></item>
    /// <item><term>WX, WY, WW, WH</term><description>World X, Y, Width, Height, respectively</description></item>
    /// <item><term>SX, SY, SW, SH</term><description>Screen X, Y, Width, Height, respectively</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class Canvas
    {
        /// <summary>
        /// The underlying Graphics. All the actual drawing is done onto this instance.
        /// </summary>
        public Graphics Graphics = null;

        /// <summary>
        /// Selects a coordinate axes mode.
        /// </summary>
        public CoordinateAxesDirection CoordinateAxesDirection = CoordinateAxesDirection.RightDown;

        /// <summary>
        /// Stores the screen size. "Screen" here refers to the final surface that this Canvas is destined
        /// for. This field may be left unspecified if no "viewport" functions are used (eg, <see cref="SetViewport"/>).
        /// </summary>
        public Size ScreenSize;

        /// <summary>
        /// The font to be used by text drawing functions if no font is specified.
        /// </summary>
        public Font DefaultFont = new Font("Arial", 10f);

        // Really try to keep these private if at all possible. ScaleX/Y must be greater than zero.
        private double ScaleX = 1;
        private double ScaleY = 1;
        private double OffsetX = 0;
        private double OffsetY = 0;

        /// <summary>
        /// Creates an instance without initializing any of the required fields.
        /// </summary>
        public Canvas()
        {
        }

        /// <summary>
        /// Creates an instance using the specified Graphics as the underlying drawing surface.
        /// </summary>
        public Canvas(Graphics graphics)
        {
            Graphics = graphics;
        }

        /// <summary>
        /// Creates an instance using the specified graphics and screenSize. See <see cref="ScreenSize"/>
        /// for more info.
        /// </summary>
        public Canvas(Graphics graphics, Size screenSize)
        {
            Graphics = graphics;
            ScreenSize = screenSize;
        }

        /// <summary>
        /// Resets the scaling and offset so that world coordinates correspond to screen pixels.
        /// Maintains the offsets so that all visible pixels have positive coordinates and one of
        /// the corners is 0,0.
        /// </summary>
        public void ResetViewport()
        {
        }

        /// <summary>
        /// Sets the scaling and offset so that the world coordinate "leftWX" corresponds to the leftmost
        /// coordinate on the screen, world "topWY" to the topmost screen coordinate, etc. If "maintainAspect"
        /// is "true", ensures that X and Y scaling is the same, by making one of the axes show more than implied by
        /// the arguments passed in.
        /// </summary>
        public void SetViewport(double leftWX, double topWY, double rightWX, double bottomWY, bool maintainAspect)
        {
            switch (CoordinateAxesDirection)
            {
                case CoordinateAxesDirection.RightUp:
                    if (leftWX >= rightWX || bottomWY >= topWY)
                        throw new RTException("RightUp coordinate system requires that leftWX < rightWX and bottomWY < topWY.");
                    break;
                case CoordinateAxesDirection.RightDown:
                    if (leftWX >= rightWX || topWY >= bottomWY)
                        throw new RTException("RightDown coordinate system requires that leftWX < rightWX and topWY < bottomWY.");
                    break;
            }
            if (leftWX > rightWX && (CoordinateAxesDirection == CoordinateAxesDirection.RightUp || CoordinateAxesDirection == CoordinateAxesDirection.RightDown))
                throw new RTException("Coordinate systems with the X axis going from right towards the left are not supported (yet)");

            // Want to make it so that:
            //   0 == SX(leftWX)
            //   0 == SY(topWY)
            //   ScreenSize.Width == SX(rightWX)
            //   ScreenSize.Height == SY(bottomWY)

             ScaleX = ScreenSize.Width / (rightWX - leftWX);
             OffsetX = -leftWX * ScaleX;
             if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
             {
                 ScaleY = ScreenSize.Height / (topWY - bottomWY);
                 OffsetY = topWY * ScaleY;
             }
             else
             {
                 ScaleY = ScreenSize.Height / (bottomWY - topWY);
                 OffsetY = -topWY * ScaleY;
             }

            if (maintainAspect)
            {
                ScaleX = Math.Min(ScaleX, ScaleY);
                ScaleY = Math.Min(ScaleX, ScaleY);
                MoveViewport((float)ScreenSize.Width / 2f, (float)ScreenSize.Height / 2f, (leftWX + rightWX) / 2, (topWY + bottomWY) / 2);
            }
        }

        /// <summary>
        /// Adjusts the offsets so that the world coordinate wx,wy is at the
        /// screen coordinate sx,sy. Does not modify the scaling.
        /// </summary>
        public void MoveViewport(float sx, float sy, double wx, double wy)
        {
            // Want this to be true:
            //   SX(wx) == sx;
            //   SY(wy) == sy;
            OffsetX = sx - wx * ScaleX;
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                OffsetY = sy + wy * ScaleY;
            else
                OffsetY = sy - wy * ScaleY;
        }

        #region World-to-screen conversion

        /// <summary>
        /// Converts world X coordinate into screen X. Screen X is zero at the leftmost pixel
        /// and Screen.Width at the rightmost pixel.
        /// </summary>
        public float SX(double wx)
        {
            return (float)(wx * ScaleX + OffsetX);
        }

        /// <summary>
        /// Converts world Y coordinate into screen Y. Screen Y is zero at the topmost pixel
        /// and Screen.Height at the bottommost pixel.
        /// </summary>
        public float SY(double wy)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return (float)(-wy * ScaleY + OffsetY);
            else
                return (float)(wy * ScaleY + OffsetY);
        }

        /// <summary>
        /// Converts world width into screen width. Screen width is measured in pixels.
        /// </summary>
        public float SW(double ww)
        {
            return (float)(ww * ScaleX);
        }

        /// <summary>
        /// Converts world height into screen height. Screen height is measured in pixels.
        /// </summary>
        public float SH(double wh)
        {
            return (float)(wh * ScaleY);
        }

        /// <summary>
        /// For internal use only. Converts world angle into screen angle as understood by
        /// GDI routines.
        /// </summary>
        private float SA(double wa)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return -(float)wa;
            else
                return (float)wa;
        }

        /// <summary>
        /// For internal use only.
        /// Given two world coordinates, one known to be smaller than the other one,
        /// returns the one that would be higher on the screen, converted to screen coordinates.
        /// </summary>
        private float STop(double yMin, double yMax)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightDown)
                return SY(yMin);
            else
                return SY(yMax);
        }

        #endregion

        #region Screen-to-world conversion

        /// <summary>
        /// Converts screen X coordinate into world X. Screen X is zero at the leftmost pixel
        /// and Screen.Width at the rightmost pixel.
        /// </summary>
        public double WX(float sx)
        {
            return (sx - OffsetX) / ScaleX;
        }

        /// <summary>
        /// Converts screen Y coordinate into world Y. Screen Y is zero at the topmost pixel
        /// and Screen.Height at the bottommost pixel.
        /// </summary>
        public double WY(float sy)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return -(sy - OffsetY) / ScaleY;
            else
                return (sy - OffsetY) / ScaleY;
        }

        /// <summary>
        /// Converts screen width into world width. Screen width is measured in pixels.
        /// </summary>
        public double WW(float sw)
        {
            return sw / ScaleX;
        }

        /// <summary>
        /// Converts screen height into world height. Screen height is measured in pixels.
        /// </summary>
        public double WH(float sh)
        {
            return sh / ScaleY;
        }

        #endregion

        #region Drawing functions

        /// <summary>
        /// Fills the entire "screen" with the specified color.
        /// </summary>
        public void Clear(Color color)
        {
            Graphics.Clear(color);
        }

        /// <summary>
        /// Draws a straight line using the specified pen.
        /// </summary>
        public void DrawLine(Pen pen, EdgeD segment)
        {
            Graphics.DrawLine(pen,
                SX(segment.Start.X), SY(segment.Start.Y),
                SX(segment.End.X), SY(segment.End.Y));
        }

        /// <summary>
        /// Draws a straight line using the specified pen.
        /// </summary>
        public void DrawLine(Pen pen, PointD pt1, PointD pt2)
        {
            Graphics.DrawLine(pen, SX(pt1.X), SY(pt1.Y), SX(pt2.X), SY(pt2.Y));
        }

        /// <summary>
        /// Draws a straight line using the specified pen.
        /// </summary>
        public void DrawLine(Pen pen, double x1, double y1, double x2, double y2)
        {
            Graphics.DrawLine(pen, SX(x1), SY(y1), SX(x2), SY(y2));
        }

        /// <summary>
        /// Draws a rectangle using the specified pen. "xMin" and "yMin" specify the corner
        /// that has the smallest coordinates, so the resulting rectangle will be on coordinates
        /// xMin, yMin, xMin+width, yMin+height.
        /// </summary>
        public void DrawRectangle(Pen pen, double xMin, double yMin, double width, double height)
        {
            Graphics.DrawRectangle(pen, SX(xMin), STop(yMin, yMin+height), SW(width), SH(height));
        }

        /// <summary>
        /// Draws a rectangle using the specified pen. The bounding box defines the coordinates.
        /// </summary>
        public void DrawRectangle(Pen pen, ref BoundingBoxD box)
        {
            Graphics.DrawRectangle(pen, SX(box.Xmin), STop(box.Ymin, box.Ymax), SW(box.Xmax-box.Xmin), SH(box.Ymax-box.Ymin));
        }

        /// <summary>
        /// Fills a rectangle using the specified brush. "xMin" and "yMin" specify the corner
        /// that has the smallest coordinates, so the resulting rectangle will be on coordinates
        /// xMin, yMin, xMin+width, yMin+height.
        /// </summary>
        public void FillRectangle(Brush brush, double xMin, double yMin, double width, double height)
        {
            Graphics.FillRectangle(brush, SX(xMin), STop(yMin, yMin+height), SW(width), SH(height));
        }

        /// <summary>
        /// Fills a rectangle using the specified brush. The bounding box defines the coordinates.
        /// </summary>
        public void FillRectangle(Brush brush, ref BoundingBoxD box)
        {
            Graphics.FillRectangle(brush, SX(box.Xmin), STop(box.Ymin, box.Ymax), SW(box.Xmax-box.Xmin), SH(box.Ymax-box.Ymin));
        }

        /// <summary>
        /// Draws a circle using the specified pen.
        /// </summary>
        public void DrawCircle(Pen pen, PointD center, double radius)
        {
            Graphics.DrawEllipse(pen,
                SX(center.X - radius), STop(center.Y - radius, center.Y + radius),
                SW(2*radius), SH(2*radius));
        }

        /// <summary>
        /// Draws a circle using the specified pen.
        /// </summary>
        public void DrawCircle(Pen pen, double centerX, double centerY, double radius)
        {
            Graphics.DrawEllipse(pen,
                SX(centerX - radius), STop(centerY - radius, centerY + radius),
                SW(2*radius), SH(2*radius));
        }

        /// <summary>
        /// Draws a "pie" using the specified pen. A pie is a circular arc whose endpoints are
        /// connected to the centre with straight lines.
        /// </summary>
        public void DrawPie(Pen pen, PointD center, double radius, double startAngle, double sweepAngle)
        {
            // DrawPie angles are in fricken degrees! I bet they are converted to radians internally before use...
            Graphics.DrawPie(pen,
                SX(center.X) - SW(radius), SY(center.Y) - SH(radius),
                SW(2*radius), SH(2*radius),
                SA(startAngle / Math.PI * 180), SA(sweepAngle / Math.PI * 180));
        }

        /// <summary>
        /// Draws a "pie" using the specified pen. A pie is a circular arc whose endpoints are
        /// connected to the centre with straight lines.
        /// </summary>
        public void DrawPie(Pen pen, double centerX, double centerY, double radius, double startAngle, double sweepAngle)
        {
            // DrawPie angles are in fricken degrees! I bet they are converted to radians internally before use...
            Graphics.DrawPie(pen,
                SX(centerX - radius), STop(centerY - radius, centerY + radius),
                SW(2*radius), SH(2*radius),
                SA(startAngle / Math.PI * 180), SA(sweepAngle / Math.PI * 180));
        }

        /// <summary>
        /// Draws text using the specified font and brush. The text's bounding box is centered on
        /// the specified point.
        /// </summary>
        public void DrawText(string text, Font font, Brush brush, double centerX, double centerY)
        {
            SizeF size = Graphics.MeasureString(text, font);
            Graphics.DrawString(text, font, brush, SX(centerX) - size.Width/2, SY(centerY) - size.Height/2);
        }

        /// <summary>
        /// Draws text using the default font and the specified brush. The text's bounding box is centered on
        /// the specified point.
        /// </summary>
        public void DrawText(string text, Brush brush, double centerX, double centerY)
        {
            DrawText(text, DefaultFont, brush, centerX, centerY);
        }

        #endregion

    }
}
