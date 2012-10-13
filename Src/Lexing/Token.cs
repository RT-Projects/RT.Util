using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace RT.KitchenSink.Lexing
{
    public class SourceLocation
    {
        public string OriginalSource { get; internal set; }
        public int Index { get; internal set; }
        public SourceLocation(string origSource, int index) { OriginalSource = origSource; Index = index; }
        public SourceLocation() { }

        private int? _line, _column;
        public int Line { get { return (_line ?? (_line = OriginalSource.Substring(0, Index).Count(ch => ch == '\n') + 1)).Value; } }
        public int Column { get { return (_column ?? (_column = Index - OriginalSource.Substring(0, Index).LastIndexOf('\n'))).Value; } }
    }

    public class SourceSpan : SourceLocation
    {
        public int Length { get; internal set; }
        public int EndIndex { get { return Index + Length; } }
        public SourceSpan(string origSource, int index, int length) : base(origSource, index) { Length = length; }
        public SourceSpan() : base() { }

        private int? _endLine, _endColumn;
        public int EndLine { get { return (_endLine ?? (_endLine = OriginalSource.Substring(0, EndIndex).Count(ch => ch == '\n') + 1)).Value; } }
        public int EndColumn { get { return (_endColumn ?? (_endColumn = EndIndex - OriginalSource.Substring(0, EndIndex).LastIndexOf('\n'))).Value; } }

        public string Source { get { return OriginalSource.Substring(Index, Length); } }
    }

    public abstract class Token : SourceSpan
    {
        public Token(string origSource, int index, int length) : base(origSource, index, length) { }
        public Token() : base() { }
        public override string ToString() { return Source; }
    }

    public sealed class SimpleToken : Token
    {
        public string Item { get; private set; }
        public SimpleToken(string item) { Item = item; }
    }
}
