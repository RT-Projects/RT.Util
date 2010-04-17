using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 1591

namespace RT.KitchenSink.Lex
{
    public class LexException : Exception
    {
        private bool _frozen = false;
        private List<PositionWithDescription> _additionalPositions = new List<PositionWithDescription>();

        public LexPosition ErrorPosition { get; private set; }
        public string ErrorDescription { get; private set; }
        public IList<PositionWithDescription> AdditionalPositions { get { return _additionalPositions.AsReadOnly(); } }

        public LexException(LexPosition errorPosition, string errorDescription)
        {
            ErrorPosition = errorPosition;
            ErrorDescription = errorDescription;
        }

        public LexException AddPosition(LexPosition position, string description)
        {
            if (_frozen)
                throw new InvalidOperationException("This class is no longer mutable.");

            _additionalPositions.Add(new PositionWithDescription(position, description));
            return this;
        }

        public LexException Freeze()
        {
            _frozen = true;
            return this;
        }

        public class PositionWithDescription
        {
            public LexPosition Position { get; private set; }
            public string Description { get; private set; }
            public PositionWithDescription(LexPosition position, string description)
            {
                Position = position;
                Description = description;
            }
        }
    }
}
