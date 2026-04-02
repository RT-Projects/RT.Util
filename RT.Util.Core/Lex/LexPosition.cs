#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public sealed class LexPosition(LexReader reader, int offset)
{
    private int _line = -1;
    private int _col = -1;
    private string _snippet;

    public int Offset
    {
        get { return offset; }
        set
        {
            offset = value;
            _line = _col = -1;
            _snippet = null;
        }
    }

    public int Line
    {
        get
        {
            if (_line < 0)
                reader.OffsetToLineCol(offset, out _line, out _col);
            return _line;
        }
    }

    public int Col
    {
        get
        {
            if (_col < 0)
                reader.OffsetToLineCol(offset, out _line, out _col);
            return _col;
        }
    }

    public string Snippet => _snippet ??= reader.GetSnippet();

    public LexPosition(LexReader reader) : this(reader, 0) { }
}
