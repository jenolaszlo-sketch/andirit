namespace Corpus.Solo;

/// <summary>
/// Valid dependency-free source unit with no accepted semantic occurrences.
/// Used to demonstrate a complete-empty analysis result that is distinct from
/// an incomplete or unavailable analysis.
/// </summary>
public sealed class Declarations
{
    public int Value { get; set; }

    public int Field;

    public void Noop()
    {
        Value = Field;
    }

    public sealed class Nested
    {
        public int Inner { get; set; }
    }
}

public interface IMarker
{
    void Touch();
}
