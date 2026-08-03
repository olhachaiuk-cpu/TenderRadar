namespace TenderRadar.Application.Configuration;

public sealed class ScoringOptions
{
    public const string SectionName = "Scoring";

    public int MinScore { get; init; } = 3;
    public KeywordRule[] Keywords { get; init; } = [];
}