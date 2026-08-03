namespace TenderRadar.Application.Configuration;

public sealed class KeywordRule
{
    public required string Phrase { get; init; }
    public int Weight { get; init; } = 1;
    public bool WholeWordOnly { get; init; }
}