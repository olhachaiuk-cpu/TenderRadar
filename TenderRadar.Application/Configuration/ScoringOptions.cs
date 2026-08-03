namespace TenderRadar.Application.Configuration;

public sealed class ScoringOptions
{
    public const string SectionName = "Scoring";

    public int MinScore { get; init; } = 3;
    public CpvWeights CpvWeights { get; init; } = new();
    public KeywordRule[] Keywords { get; init; } = [];
}

public sealed class CpvWeights
{
    public int DirectHit { get; init; } = 5;
    public int Development { get; init; } = 2;
}