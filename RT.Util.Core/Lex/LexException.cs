#pragma warning disable 1591

namespace RT.KitchenSink.Lex;

public sealed class LexException(LexPosition errorPosition, string errorDescription) : Exception
{
    private bool _frozen = false;
    private List<PositionWithDescription> _additionalPositions = [];

    public LexPosition ErrorPosition { get; private set; } = errorPosition;
    public string ErrorDescription { get; private set; } = errorDescription;
    public IList<PositionWithDescription> AdditionalPositions { get { return _additionalPositions.AsReadOnly(); } }

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

    public sealed class PositionWithDescription(LexPosition position, string description)
    {
        public LexPosition Position { get; private set; } = position;
        public string Description { get; private set; } = description;
    }
}
