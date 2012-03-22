using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.ParseCs
{
    public sealed class ParseException : Exception
    {
        private int _index;
        private object _incompleteResult;
        public ParseException(string message, int index) : this(message, index, null, null) { }
        public ParseException(string message, int index, object incompleteResult) : this(message, index, incompleteResult, null) { }
        public ParseException(string message, int index, object incompleteResult, Exception inner)
            : base(message, inner)
        {
            _index = index;
            _incompleteResult = incompleteResult;
        }
        public int Index { get { return _index; } }
        public object IncompleteResult { get { return _incompleteResult; } }
    }

    public sealed class InternalErrorException : Exception
    {
        public InternalErrorException(string message = null, Exception inner = null) : base(message, inner) { }
    }
}
