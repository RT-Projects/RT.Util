using System.Text.RegularExpressions;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public sealed class LexReader
{
    private string _text;
    private int _offset;
    private int[] _lineStarts;

    public LexReader(string text)
    {
        if (text == null)
            throw new InvalidOperationException("text argument cannot be null");
        _text = text;
        _offset = 0;

        // Build a list containing the offset of the starting character of every line in the template
        var ls = new List<int> { 0 };
        for (int i = 0; i < _text.Length; i++)
            if (_text[i] == '\n' || (_text[i] == '\r' && (i == _text.Length - 1 || _text[i + 1] != '\n')))
                ls.Add(i + 1);
        _lineStarts = ls.ToArray();
    }

    public void OffsetToLineCol(int offset, out int line, out int col)
    {
        int index = Array.BinarySearch(_lineStarts, offset);
        line = (index >= 0) ? index : ((~index) - 1);
        col = offset - _lineStarts[line - 1] + 1;
    }

    public string GetSnippet()
    {
        return _text.Length - _offset > 15 ? (_text.Substring(_offset, 12) + "...") : _text.Substring(_offset);
    }

    public LexPosition GetPosition()
    {
        return new LexPosition(this, _offset);
    }

    public LexPosition GetPosition(int offsetFromCurrent)
    {
        return new LexPosition(this, _offset + offsetFromCurrent);
    }

    public bool EndOfFile()
    {
        return _offset >= _text.Length;
    }

    public bool HasAtLeastChars(int count)
    {
        return _text.Length - _offset >= count;
    }

    public bool ContinuesWith(string text)
    {
        if (!HasAtLeastChars(text.Length))
            return false;
        return _text.Substring(_offset, text.Length) == text;
    }

    public void ConsumeAnyWhitespace()
    {
        while (_offset < _text.Length)
        {
            switch (_text[_offset])
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    _offset++;
                    break;
                default:
                    return;
            }
        }
    }

    public void Consume(int count)
    {
        if (count < 0) throw new ArgumentException("Count cannot be negative", nameof(count));
        if (_offset + count > _text.Length) throw new InvalidOperationException("There aren't enough characters left in the input");
        _offset += count;
    }

    public char ConsumeChar()
    {
        if (_offset + 1 > _text.Length) throw new InvalidOperationException("There aren't any characters left in the input");
        char c = _text[_offset];
        _offset++;
        return c;
    }

    public string ConsumeString(int count)
    {
        if (count < 0) throw new ArgumentException("Count cannot be negative", nameof(count));
        if (_offset + count > _text.Length) throw new InvalidOperationException("There aren't enough characters left in the input");
        string result = _text.Substring(_offset, count);
        _offset += count;
        return result;
    }

    public string ConsumeStringWhile(Func<char, bool> condition)
    {
        return ConsumeStringWhile(condition, int.MaxValue);
    }

    public string ConsumeStringWhile(Func<char, bool> condition, int maxCount)
    {
        int endOffset = _offset;
        int maxOffset = maxCount > int.MaxValue / 2 ? _text.Length : Math.Min(_offset + maxCount, _text.Length);
        while ((endOffset < maxOffset) && condition(_text[endOffset]))
            endOffset++;
        string result = _text.Substring(_offset, endOffset - _offset);
        _offset = endOffset;
        return result;
    }

    public string ConsumeEntireMatch(Regex regex)
    {
        var match = regex.Match(_text, _offset);
        if (!match.Success || match.Index != _offset)
            return null;
        string result = _text.Substring(_offset, match.Length);
        _offset += match.Length;
        return result;
    }

    public char this[int offsetFromCurrent]
    {
        get
        {
            int offs = _offset + offsetFromCurrent;
            if (offs < 0) throw new InvalidOperationException("Resulting offset is out of range (too small)");
            if (offs >= _text.Length) throw new InvalidOperationException("Resulting offset is out of range (too large)");
            return _text[offs];
        }
    }
}
