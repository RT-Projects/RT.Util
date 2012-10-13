using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 1591

namespace RT.KitchenSink.Lexing
{
    public sealed class LexException : Exception
    {
        public SourceSpan ErrorLocation { get; private set; }

        public LexException(SourceSpan errorLocation, string message)
            : base(message)
        {
            ErrorLocation = errorLocation;
        }
    }
}
