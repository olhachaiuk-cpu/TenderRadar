using Microsoft.EntityFrameworkCore;
using TenderRadar.Domain;

namespace TenderRadar.Infrastructure.Persistence;

public interface ITenderRepository
{
    Task UpsertRangeAsync(IEnumerable<Tender> tenders, CancellationToken ct = default);
    
    Task<HashSet<(string Source, string PublicationNumber)>> GetExportedKeysAsync(
        IEnumerable<(string Source, string PublicationNumber)> keys, CancellationToken ct = default);
    
    Task MarkExportedAsync(IEnumerable<(string Source, string PublicationNumber)> keys, CancellationToken ct = default);
}

public sealed class TenderRepository(AppDbContext db) : ITenderRepository
{
    public async Task UpsertRangeAsync(IEnumerable<Tender> tenders, CancellationToken ct = default)
    {
        var incoming = tenders
            .GroupBy(t => (t.Source, t.PublicationNumber))
            .Select(g => g.Last())
            .ToList();

        var sources = incoming.Select(t => t.Source).Distinct().ToList();
        var numbers = incoming.Select(t => t.PublicationNumber).ToList();

        var existingList = await db.Tenders
            .Where(t => sources.Contains(t.Source) && numbers.Contains(t.PublicationNumber))
            .ToListAsync(ct);

        var existingMap = existingList.ToDictionary(t => (t.Source, t.PublicationNumber));

        foreach (var item in incoming)
        {
            if (existingMap.TryGetValue((item.Source, item.PublicationNumber), out var existing))
            {
                existing.Title = item.Title;
                existing.ShortTitle = item.ShortTitle;
                existing.BuyerName = item.BuyerName;
                existing.Country = item.Country;
                existing.SubmissionDeadline = item.SubmissionDeadline;
                existing.CpvCodes = item.CpvCodes;
                existing.EstimatedValue = item.EstimatedValue;
                existing.Currency = item.Currency;
                existing.SearchText = item.SearchText;
                existing.MatchedKeywords = item.MatchedKeywords;
                existing.Score = item.Score;
            }
            else
            {
                db.Tenders.Add(item);
            }
        }

        await db.SaveChangesAsync(ct);
    }
    
    public async Task<HashSet<(string Source, string PublicationNumber)>> GetExportedKeysAsync(
        IEnumerable<(string Source, string PublicationNumber)> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return [];

        var sources = keyList.Select(k => k.Source).Distinct().ToList();
        var numbers = keyList.Select(k => k.PublicationNumber).ToList();

        var exported = await db.Tenders
            .Where(t => sources.Contains(t.Source)
                        && numbers.Contains(t.PublicationNumber)
                        && t.ExportedAt != null)
            .Select(t => new { t.Source, t.PublicationNumber })
            .ToListAsync(ct);

        return exported.Select(t => (t.Source, t.PublicationNumber)).ToHashSet();
    }

    
    public async Task MarkExportedAsync(
        IEnumerable<(string Source, string PublicationNumber)> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return;

        var sources = keyList.Select(k => k.Source).Distinct().ToList();
        var numbers = keyList.Select(k => k.PublicationNumber).ToList();

        var tenders = await db.Tenders
            .Where(t => sources.Contains(t.Source) && numbers.Contains(t.PublicationNumber))
            .ToListAsync(ct);

        var keySet = keyList.ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var t in tenders)
            if (keySet.Contains((t.Source, t.PublicationNumber)))
                t.ExportedAt = now;

        await db.SaveChangesAsync(ct);
    }
}