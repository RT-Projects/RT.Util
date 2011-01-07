using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace RT.Util.Drawing
{
    /// <summary>Provides a way to temporarily modify the Transform of a System.Drawing.Graphics object by enclosing the affected code in a “using” scope.</summary>
    /// <example>
    /// <para>The following example demonstrates how GraphicsTransformer can be used to render graphics translated.</para>
    /// <code>
    ///     var g = Graphics.FromImage(...);
    ///     using (new GraphicsTransformer(g).Translate(15, 10))
    ///     {
    ///         // As this is inside the scope of the GraphicsTransformer, the rectangle is translated 15 pixels to the right and 10 down.
    ///         g.DrawRectangle(20, 20, 100, 100);
    ///     }
    ///     
    ///     // As this statement is outside the scope of the GraphicsTransformer, the rectangle is not translated.
    ///     // The net effect is that two rectangles are rendered even though both calls use the same co-ordinates.
    ///     g.DrawRectangle(20, 20, 100, 100);
    /// </code>
    /// </example>
    public class GraphicsTransformer : IDisposable
    {
        private Graphics _graphics;
        private Matrix _previousTransform;
        private Matrix _currentTransform;

        /// <summary>Instantiates a new <see cref="GraphicsTransformer"/> instance. Use this in a “using” statement.</summary>
        /// <param name="g">The Graphics object whose Transform to modify.</param>
        public GraphicsTransformer(Graphics g)
        {
            _graphics = g;
            _previousTransform = g.Transform.Clone();
            _currentTransform = g.Transform.Clone();
        }

        /// <summary>Translates the graphics by the specified amount.</summary>
        public GraphicsTransformer Translate(float offsetX, float offsetY)
        {
            _currentTransform.Translate(offsetX, offsetY);
            _graphics.Transform = _currentTransform;
            return this;
        }

        /// <summary>Returns the Transform of the Graphics object back to its original value.</summary>
        public void Dispose()
        {
            _graphics.Transform = _previousTransform;
        }
    }
}
