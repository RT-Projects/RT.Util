#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public sealed class LexPosition
{
    private LexReader _reader;
    private int _offset;
    private int _line = -1;
    private int _col = -1;
    private string _snippet;

    public int Offset
    {
        get { return _offset; }
        set
        {
            _offset = value;
            _line = _col = -1;
            _snippet = null;
        }
    }

    public int Line
    {
        get
        {
            if (_line < 0)
                _reader.OffsetToLineCol(_offset, out _line, out _col);
            return _line;
        }
    }

    public int Col
    {
        get
        {
            if (_col < 0)
                _reader.OffsetToLineCol(_offset, out _line, out _col);
            return _col;
        }
    }

    public string Snippet
    {
        get
        {
            if (_snippet == null)
                _snippet = _reader.GetSnippet();
            return _snippet;
        }
    }

    public LexPosition(LexReader reader) : this(reader, 0) { }

    public LexPosition(LexReader reader, int offset)
    {
        _reader = reader;
        _offset = offset;
    }
}
