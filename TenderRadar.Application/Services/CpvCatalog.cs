namespace TenderRadar.Application.Services;

public sealed record CpvMatch(int DirectCount, int DevelopmentCount, int Total);

public sealed class CpvCatalog(IEnumerable<string> directHit, IEnumerable<string> development)
{
    private readonly HashSet<string> _directHit = new(directHit, StringComparer.Ordinal);
    private readonly HashSet<string> _development = new(development, StringComparer.Ordinal);

    public CpvMatch Match(IReadOnlyCollection<string> codes) => new(
        DirectCount: codes.Count(_directHit.Contains),
        DevelopmentCount: codes.Count(_development.Contains),
        Total: codes.Count);
}

