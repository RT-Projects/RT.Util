using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using RT.Util.ExtensionMethods;
using RT.Util.Geometry;

namespace RT.Util.Drawing
{
    /// <summary>Encodes axes direction mode for <see cref="Canvas"/>.</summary>
    public enum CoordinateAxesDirection
    {
        /// <summary>X axis grows towards the right and Y axis grows downwards (normal computer screen coordinate direction)</summary>
        RightDown,
        /// <summary>X axis grows towards the right and Y axis grows upwards.</summary>
        RightUp
    }

    /// <summary>Specifies text alignment for <see cref="Canvas"/>.</summary>
    public enum TextAnchor
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        Center,
        TopLeft, TopRight, BottomLeft, BottomRight,
        TopCenter, LeftCenter, BottomCenter, RightCenter,
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// <para>Wraps a <see cref="System.Drawing.Graphics"/> to provide a hopefully more convenient interface.
    /// The major bits of functionality are:</para>
    /// <list type="bullet">
    /// <item><description>all coordinates in doubles or <see cref="RT.Util.Geometry"/> structs;</description></item>
    /// <item><description>support for the Y axis growing upwards;</description></item>
    /// <item><description>functions targeted at drawing a 2d world onto a viewport of a specified size.</description></item>
    /// </list>
    /// <para>Common terms and abbreviations:</para>
    /// <list type="table">
    /// <item><term>Screen</term><description>Rectangular area that will be the final destination of the drawing.</description></item>
    /// <item><term>Viewport</term><description>The region, defined in terms of the world coordinates, that is viewable on the screen.</description></item>
    /// <item><term>World coordinates</term><description>Coordinates of the underlying "world" being represented.</description></item>
    /// <item><term>Screen coordinates</term><description>Coordinates on the screen, can be used to directly draw on the underlying Graphics.</description></item>
    /// <item><term>WX, WY, WW, WH</term><description>World X, Y, Width, Height, respectively</description></item>
    /// <item><term>SX, SY, SW, SH</term><description>Screen X, Y, Width, Height, respectively</description></item>
    /// </list>
    /// </summary>
    public sealed class Canvas
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        /// <summary>
        /// The underlying Graphics. All the actual drawing is done onto this instance.
        /// </summary>
        public Graphics Graphics = null;

        /// <summary>
        /// Selects a coordinate axes mode.
        /// </summary>
        public CoordinateAxesDirection CoordinateAxesDirection = CoordinateAxesDirection.RightDown;

        /// <summary>
        /// Stores the screen size. "Screen" here refers to the final surface that this Canvas is destined for.
        /// The screen size is used for "set viewport" methods which do not take a screen location: they assume
        /// the relevant screen edge is meant instead.
        /// </summary>
        public Size ScreenSize;

        /// <summary>
        /// The font to be used by text drawing functions if no font is specified.
        /// </summary>
        public Font DefaultFont = new Font("Arial", 10f);

        // Really try to keep these private if at all possible. ScaleX/Y must be greater than zero.
        private double _scaleX = 1;
        private double _scaleY = 1;
        private double _offsetX = 0;
        private double _offsetY = 0;

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
        /// Sets the viewport so that the specified world coordinate is in the centre of the
        /// viewable screen, using the specified scaling factor.
        /// </summary>
        public void SetViewport(double centerWX, double centerWY, double scale)
        {
            _scaleX = _scaleY = scale;
            MoveViewport(ScreenSize.Width / 2, ScreenSize.Height / 2, centerWX, centerWY);
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

            _scaleX = ScreenSize.Width / (rightWX - leftWX);
            _offsetX = -leftWX * _scaleX;
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
            {
                _scaleY = ScreenSize.Height / (topWY - bottomWY);
                _offsetY = topWY * _scaleY;
            }
            else
            {
                _scaleY = ScreenSize.Height / (bottomWY - topWY);
                _offsetY = -topWY * _scaleY;
            }

            if (maintainAspect)
            {
                _scaleX = Math.Min(_scaleX, _scaleY);
                _scaleY = Math.Min(_scaleX, _scaleY);
                MoveViewport((float) ScreenSize.Width / 2f, (float) ScreenSize.Height / 2f, (leftWX + rightWX) / 2, (topWY + bottomWY) / 2);
            }
        }

        public void SetViewport(double leftWX, double topWY, double rightWX, double bottomWY, double aspectXY)
        {
            SetViewport(leftWX, topWY, rightWX, bottomWY, false);
            double asp = _scaleX / _scaleY;
            if (asp < aspectXY)
                _scaleY = _scaleX / aspectXY;
            else
                _scaleX = _scaleY * aspectXY;
            MoveViewport((float) ScreenSize.Width / 2f, (float) ScreenSize.Height / 2f, (leftWX + rightWX) / 2, (topWY + bottomWY) / 2);
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
            _offsetX = sx - wx * _scaleX;
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                _offsetY = sy + wy * _scaleY;
            else
                _offsetY = sy - wy * _scaleY;
        }

        #region World-to-screen conversion

        /// <summary>
        /// Converts world X coordinate into screen X. Screen X is zero at the leftmost pixel
        /// and Screen.Width at the rightmost pixel.
        /// </summary>
        public float SX(double wx)
        {
            return (float) (wx * _scaleX + _offsetX);
        }

        /// <summary>
        /// Converts world Y coordinate into screen Y. Screen Y is zero at the topmost pixel
        /// and Screen.Height at the bottommost pixel.
        /// </summary>
        public float SY(double wy)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return (float) (-wy * _scaleY + _offsetY);
            else
                return (float) (wy * _scaleY + _offsetY);
        }

        /// <summary>
        /// Converts world width into screen width. Screen width is measured in pixels.
        /// </summary>
        public float SW(double ww)
        {
            return (float) (ww * _scaleX);
        }

        /// <summary>
        /// Converts world height into screen height. Screen height is measured in pixels.
        /// </summary>
        public float SH(double wh)
        {
            return (float) (wh * _scaleY);
        }

        /// <summary>
        /// For internal use only. Converts world angle into screen angle as understood by
        /// GDI routines.
        /// </summary>
        private float sa(double wa)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return -(float) wa;
            else
                return (float) wa;
        }

        /// <summary>
        /// For internal use only.
        /// Given two world coordinates, one known to be smaller than the other one,
        /// returns the one that would be higher on the screen, converted to screen coordinates.
        /// </summary>
        private float sTop(double yMin, double yMax)
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
            return (sx - _offsetX) / _scaleX;
        }

        /// <summary>
        /// Converts screen Y coordinate into world Y. Screen Y is zero at the topmost pixel
        /// and Screen.Height at the bottommost pixel.
        /// </summary>
        public double WY(float sy)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                return -(sy - _offsetY) / _scaleY;
            else
                return (sy - _offsetY) / _scaleY;
        }

        /// <summary>
        /// Converts screen width into world width. Screen width is measured in pixels.
        /// </summary>
        public double WW(float sw)
        {
            return sw / _scaleX;
        }

        /// <summary>
        /// Converts screen height into world height. Screen height is measured in pixels.
        /// </summary>
        public double WH(float sh)
        {
            return sh / _scaleY;
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
            Graphics.DrawRectangle(pen, SX(xMin), sTop(yMin, yMin + height), SW(width) + 1, SH(height) + 1);
        }

        /// <summary>
        /// Draws a rectangle using the specified pen. The bounding box defines the coordinates.
        /// </summary>
        public void DrawRectangle(Pen pen, ref BoundingBoxD box)
        {
            Graphics.DrawRectangle(pen, SX(box.Xmin), sTop(box.Ymin, box.Ymax), SW(box.Xmax - box.Xmin) + 1, SH(box.Ymax - box.Ymin) + 1);
        }

        /// <summary>
        /// Fills a rectangle using the specified brush. "xMin" and "yMin" specify the corner
        /// that has the smallest coordinates, so the resulting rectangle will be on coordinates
        /// xMin, yMin, xMin+width, yMin+height.
        /// </summary>
        public void FillRectangle(Brush brush, double xMin, double yMin, double width, double height)
        {
            Graphics.FillRectangle(brush, SX(xMin), sTop(yMin, yMin + height), SW(width) + 1, SH(height) + 1);
        }

        /// <summary>
        /// Fills a rectangle using the specified brush. The bounding box defines the coordinates.
        /// </summary>
        public void FillRectangle(Brush brush, ref BoundingBoxD box)
        {
            Graphics.FillRectangle(brush, SX(box.Xmin), sTop(box.Ymin, box.Ymax), SW(box.Xmax - box.Xmin) + 1, SH(box.Ymax - box.Ymin) + 1);
        }

        /// <summary>
        /// Draws a circle using the specified pen.
        /// </summary>
        public void DrawCircle(Pen pen, PointD center, double radius)
        {
            Graphics.DrawEllipse(pen,
                SX(center.X - radius), sTop(center.Y - radius, center.Y + radius),
                SW(2 * radius), SH(2 * radius));
        }

        /// <summary>
        /// Draws a circle using the specified pen.
        /// </summary>
        public void DrawCircle(Pen pen, double centerX, double centerY, double radius)
        {
            Graphics.DrawEllipse(pen,
                SX(centerX - radius), sTop(centerY - radius, centerY + radius),
                SW(2 * radius), SH(2 * radius));
        }

        /// <summary>
        /// Fills a circle using the specified pen.
        /// </summary>
        public void FillCircle(Brush brush, PointD center, double radius)
        {
            Graphics.FillEllipse(brush,
                SX(center.X - radius), sTop(center.Y - radius, center.Y + radius),
                SW(2 * radius), SH(2 * radius));
        }

        /// <summary>
        /// Fills a circle using the specified pen.
        /// </summary>
        public void FillCircle(Brush brush, double centerX, double centerY, double radius)
        {
            Graphics.FillEllipse(brush,
                SX(centerX - radius), sTop(centerY - radius, centerY + radius),
                SW(2 * radius), SH(2 * radius));
        }

        /// <summary>
        /// Draws an arc using the specified pen.
        /// </summary>
        public void DrawArc(Pen pen, PointD center, double radius, double startAngle, double sweepAngle)
        {
            // DrawArc angles are in fricken degrees! I bet they are converted to radians internally before use...
            Graphics.DrawArc(pen,
                SX(center.X) - SW(radius), SY(center.Y) - SH(radius),
                SW(2 * radius), SH(2 * radius),
                sa(startAngle / Math.PI * 180), sa(sweepAngle / Math.PI * 180));
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
                SW(2 * radius), SH(2 * radius),
                sa(startAngle / Math.PI * 180), sa(sweepAngle / Math.PI * 180));
        }

        /// <summary>
        /// Draws a "pie" using the specified pen. A pie is a circular arc whose endpoints are
        /// connected to the centre with straight lines.
        /// </summary>
        public void DrawPie(Pen pen, double centerX, double centerY, double radius, double startAngle, double sweepAngle)
        {
            // DrawPie angles are in fricken degrees! I bet they are converted to radians internally before use...
            Graphics.DrawPie(pen,
                SX(centerX - radius), sTop(centerY - radius, centerY + radius),
                SW(2 * radius), SH(2 * radius),
                sa(startAngle / Math.PI * 180), sa(sweepAngle / Math.PI * 180));
        }

        /// <summary>
        /// Draws text using the specified font and brush. The text's bounding box is centered on
        /// the specified point.
        /// </summary>
        public void DrawText(string text, Brush brush, Font font, double centerX, double centerY)
        {
            SizeF size = Graphics.MeasureString(text, font);
            Graphics.DrawString(text, font, brush, SX(centerX) - size.Width / 2, SY(centerY) - size.Height / 2);
        }

        /// <summary>
        /// Draws text using the specified font and brush. The text's bounding box is centered on
        /// the specified point.
        /// </summary>
        public void DrawText(string text, Brush brush, Font font, PointD center)
        {
            DrawText(text, brush, font, center.X, center.Y);
        }

        /// <summary>
        /// Draws text using the default font and the specified brush. The text's bounding box is centered on
        /// the specified point.
        /// </summary>
        public void DrawText(string text, Brush brush, double centerX, double centerY)
        {
            DrawText(text, brush, DefaultFont, centerX, centerY);
        }

        private void convertAnchor(TextAnchor anchor, out int horzAlign, out int vertAlign)
        {
            switch (anchor)
            {
                case TextAnchor.Center: horzAlign = 0; vertAlign = 0; return;
                case TextAnchor.TopLeft: horzAlign = -1; vertAlign = -1; return;
                case TextAnchor.TopRight: horzAlign = 1; vertAlign = -1; return;
                case TextAnchor.BottomLeft: horzAlign = -1; vertAlign = 1; return;
                case TextAnchor.BottomRight: horzAlign = 1; vertAlign = 1; return;
                case TextAnchor.TopCenter: horzAlign = 0; vertAlign = -1; return;
                case TextAnchor.LeftCenter: horzAlign = -1; vertAlign = 0; return;
                case TextAnchor.BottomCenter: horzAlign = 0; vertAlign = 1; return;
                case TextAnchor.RightCenter: horzAlign = 1; vertAlign = 0; return;
                default: throw new InternalErrorException("72634hfdj");
            }
        }

        public void DrawText2(string text, Brush brush, double x, double y, Font font = null, TextAnchor anchor = TextAnchor.Center)
        {
            if (font == null)
                font = DefaultFont;
            SizeF size = Graphics.MeasureString(text, font);
            int horzAlign, vertAlign;
            convertAnchor(anchor, out horzAlign, out vertAlign);
            Graphics.DrawString(text, font, brush, SX(x) - (size.Width / 2) * (horzAlign + 1), SY(y) - (size.Height / 2) * (vertAlign + 1));
        }

        public void DrawTextOutline(string text, Pen pen, Font font, double centerX, double centerY)
        {
            SizeF size = Graphics.MeasureString(text, font);
            var gp = new GraphicsPath();
            gp.AddString(text, font.FontFamily, (int) font.Style, font.Size * Graphics.DpiX / 72f, new PointF(SX(centerX) - size.Width / 2, SY(centerY) - size.Height / 2), StringFormat.GenericDefault);
            Graphics.DrawPath(pen, gp);
        }

        public void DrawTextOutline(string text, Pen pen, Font font, PointD center)
        {
            DrawTextOutline(text, pen, font, center.X, center.Y);
        }

        public void DrawTextOutlineSim(string text, Brush brush, Font font, double shift, PointD center)
        {
            var ss = shift;
            var sd = shift * 0.70710678;
            DrawText(text, brush, font, center.X + ss, center.Y);
            DrawText(text, brush, font, center.X + sd, center.Y + sd);
            DrawText(text, brush, font, center.X, center.Y + ss);
            DrawText(text, brush, font, center.X - sd, center.Y + sd);
            DrawText(text, brush, font, center.X - ss, center.Y);
            DrawText(text, brush, font, center.X - sd, center.Y - sd);
            DrawText(text, brush, font, center.X, center.Y - ss);
            DrawText(text, brush, font, center.X + sd, center.Y - sd);
        }

        /// <summary>
        /// Draws a GraphicsPath using the specified pen.
        /// </summary>
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            Graphics.DrawPath(pen, path);
        }

        /// <summary>
        /// Fills a GraphicsPath using the specified brush.
        /// </summary>
        public void FillPath(Brush brush, GraphicsPath path)
        {
            Graphics.FillPath(brush, path);
        }

        #endregion

        #region Helper functions

        public GraphicsPath MakeRoundedHorzVertPath(PointD position, double radius, IEnumerable<double> lengths)
        {
            double x = position.X;
            double y = position.Y;
            bool horz = true;
            var gp = new GraphicsPath();
            var prevpair = Tuple.Create(lengths.Last(), lengths.First());
            foreach (var pair in lengths.ConsecutivePairs(true))
            {
                double radp = Math.Min(Math.Min(Math.Abs(prevpair.Item1), Math.Abs(prevpair.Item2)) / 2, radius);
                double radf = Math.Min(Math.Min(Math.Abs(pair.Item1), Math.Abs(pair.Item2)) / 2, radius);
                if (horz && pair.Item1 > 0)
                {
                    gp.AddLine(SX(x + radp), SY(y), SX(x + pair.Item1 - radf), SY(y));
                    gp.AddArc(SX(x + pair.Item1 - 2 * radf), SY(y - (pair.Item2 > 0 ? 0 : 2 * radf)), SW(2 * radf), SH(2 * radf), pair.Item2 > 0 ? 270f : 90f, pair.Item2 > 0 ? 90f : -90f);
                }
                else if (horz && pair.Item1 < 0)
                {
                    gp.AddLine(SX(x - radp), SY(y), SX(x + pair.Item1 + radf), SY(y));
                    gp.AddArc(SX(x + pair.Item1), SY(y - (pair.Item2 > 0 ? 0 : 2 * radf)), SW(2 * radf), SH(2 * radf), pair.Item2 > 0 ? 270f : 90f, pair.Item2 > 0 ? -90f : 90f);
                }
                else if (!horz && pair.Item1 > 0)
                {
                    gp.AddLine(SX(x), SY(y + radp), SX(x), SY(y + pair.Item1 - radf));
                    gp.AddArc(SX(x - (pair.Item2 > 0 ? 0 : 2 * radf)), SY(y + pair.Item1 - 2 * radf), SW(2 * radf), SH(2 * radf), pair.Item2 > 0 ? 180f : 0f, pair.Item2 > 0 ? -90f : 90f);
                }
                else if (!horz && pair.Item1 < 0)
                {
                    gp.AddLine(SX(x), SY(y - radp), SX(x), SY(y + pair.Item1 + radf));
                    gp.AddArc(SX(x - (pair.Item2 > 0 ? 0 : 2 * radf)), SY(y + pair.Item1), SW(2 * radf), SH(2 * radf), pair.Item2 > 0 ? 180f : 0f, pair.Item2 > 0 ? 90f : -90f);
                }
                if (horz) x += pair.Item1;
                else y += pair.Item1;
                prevpair = pair;
                horz = !horz;
            }
            gp.CloseFigure();
            return gp;
        }

        #endregion

        public void SetViewportHorz(double worldLeft, double worldRight, float screenLeft, float screenRight, bool maintainAspect = false)
        {
            _scaleX = (screenRight - screenLeft) / (worldRight - worldLeft);
            _offsetX = screenLeft - worldLeft * _scaleX;
            if (maintainAspect)
                _scaleY = _scaleX;
        }

        public void SetViewportVert(double worldTop, double worldBottom, float screenTop, float screenBottom, bool maintainAspect = false)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightDown)
            {
                _scaleY = (screenBottom - screenTop) / (worldBottom - worldTop);
                _offsetY = screenTop - worldTop * _scaleY;
            }
            else
            {
                _scaleY = (screenTop - screenBottom) / (worldBottom - worldTop);
                _offsetY = screenTop + worldTop * _scaleY;
            }
            if (maintainAspect)
                _scaleX = _scaleY;
        }

        public void SetViewportHorz(double worldX, float screenX)
        {
            _offsetX = screenX - worldX * _scaleX;
        }

        public void SetViewportVert(double worldY, float screenY)
        {
            if (CoordinateAxesDirection == CoordinateAxesDirection.RightUp)
                _offsetY = screenY + worldY * _scaleY;
            else
                _offsetY = screenY - worldY * _scaleY;
        }

        public void SetViewportHorz(double worldLeft, double worldRight)
        {
            SetViewportHorz(worldLeft, worldRight, 0, ScreenSize.Width - 1);
        }

        public void SetViewportVert(double worldTop, double worldBottom)
        {
            SetViewportVert(worldTop, worldBottom, 0, ScreenSize.Height - 1);
        }

        public void SetViewportWidth(double worldWidth, float screenWidth, bool maintainAspect = false)
        {
            _scaleX = screenWidth / worldWidth;
            if (maintainAspect)
                _scaleY = _scaleX;
        }

        public void SetViewportHeight(double worldHeight, float screenHeight, bool maintainAspect = false)
        {
            _scaleY = screenHeight / worldHeight;
            if (maintainAspect)
                _scaleX = _scaleY;
        }

        public float ViewportCenterSX { get { return ScreenSize.Width / 2f; } }
        public float ViewportCenterSY { get { return ScreenSize.Height / 2f; } }
        public double ViewportCenterWX { get { return WX(ViewportCenterSX); } }
        public double ViewportCenterWY { get { return WY(ViewportCenterSY); } }

        public double ViewportTopWY { get { return (0 - _offsetY) / _scaleY; } }
        public double ViewportBottomWY { get { return (ScreenSize.Height - _offsetY) / _scaleY; } }
        public double ViewportLeftWX { get { return (0 - _offsetX) / _scaleX; } }
        public double ViewportRightWX { get { return (ScreenSize.Width - _offsetX) / _scaleX; } }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }
}
