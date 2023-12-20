using System.Drawing.Drawing2D;

namespace RT.Util.Drawing;

/// <summary>
///     Provides a way to temporarily modify the Transform of a System.Drawing.Graphics object by enclosing the affected code
///     in a “using” scope.</summary>
/// <example>
///     <para>
///         The following example demonstrates how GraphicsTransformer can be used to render graphics translated.</para>
///     <code>
///         var g = Graphics.FromImage(...);
///         using (new GraphicsTransformer(g).Translate(15, 10))
///         {
///             // As this is inside the scope of the GraphicsTransformer, the rectangle is translated 15 pixels to the right and 10 down.
///             g.DrawRectangle(20, 20, 100, 100);
///         }
///
///         // As this statement is outside the scope of the GraphicsTransformer, the rectangle is not translated.
///         // The net effect is that two rectangles are rendered even though both calls use the same co-ordinates.
///         g.DrawRectangle(20, 20, 100, 100);</code></example>
public class GraphicsTransformer : IDisposable
{
    private Graphics _graphics;
    private static Dictionary<Graphics, Stack<Matrix>> _transforms = new Dictionary<Graphics, Stack<Matrix>>();

    /// <summary>
    ///     Instantiates a new <see cref="GraphicsTransformer"/> instance. Use this in a “using” statement.</summary>
    /// <param name="g">
    ///     The Graphics object whose Transform to modify.</param>
    public GraphicsTransformer(Graphics g)
    {
        _graphics = g;
        if (!_transforms.ContainsKey(g))
        {
            _transforms[g] = new Stack<Matrix>();
            _transforms[g].Push(g.Transform.Clone());
        }
        _transforms[g].Push(_transforms[g].Peek().Clone());
    }

    /// <summary>Translates the graphics by the specified amount.</summary>
    public GraphicsTransformer Translate(float offsetX, float offsetY)
    {
        var m = _transforms[_graphics].Peek();
        m.Translate(offsetX, offsetY, MatrixOrder.Append);
        _graphics.Transform = m;
        return this;
    }

    /// <summary>Translates the graphics by the specified amount.</summary>
    public GraphicsTransformer Translate(double offsetX, double offsetY) { return Translate((float) offsetX, (float) offsetY); }

    /// <summary>Scales the graphics by the specified factors.</summary>
    public GraphicsTransformer Scale(float scaleX, float scaleY)
    {
        var m = _transforms[_graphics].Peek();
        m.Scale(scaleX, scaleY, MatrixOrder.Append);
        _graphics.Transform = m;
        return this;
    }

    /// <summary>Scales the graphics by the specified factors.</summary>
    public GraphicsTransformer Scale(double scaleX, double scaleY) { return Scale((float) scaleX, (float) scaleY); }

    /// <summary>Rotates the graphics by the specified angle in degrees.</summary>
    public GraphicsTransformer Rotate(float angle)
    {
        var m = _transforms[_graphics].Peek();
        m.Rotate(angle, MatrixOrder.Append);
        _graphics.Transform = m;
        return this;
    }

    /// <summary>Rotates the graphics clockwise by the specified angle in degrees about the specified center point.</summary>
    public GraphicsTransformer RotateAt(float angle, PointF point)
    {
        var m = _transforms[_graphics].Peek();
        m.RotateAt(angle, point, MatrixOrder.Append);
        _graphics.Transform = m;
        return this;
    }

    /// <summary>Rotates the graphics clockwise by the specified angle in degrees about the specified center point.</summary>
    public GraphicsTransformer RotateAt(float angle, float x, float y) { return RotateAt(angle, new PointF(x, y)); }

    /// <summary>Returns the Transform of the Graphics object back to its original value.</summary>
    public void Dispose()
    {
        _transforms[_graphics].Pop();
        _graphics.Transform = _transforms[_graphics].Peek();
        if (_transforms[_graphics].Count == 1)
            _transforms.Remove(_graphics);
    }
}
