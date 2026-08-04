using Microsoft.EntityFrameworkCore;
using TenderRadar.Domain;

namespace TenderRadar.Infrastructure.Persistence;

public interface ITenderRepository
{
    Task UpsertRangeAsync(IEnumerable<Tender> tenders, CancellationToken ct = default);
}

public sealed class TenderRepository(AppDbContext db) : ITenderRepository
{
    public async Task UpsertRangeAsync(IEnumerable<Tender> tenders, CancellationToken ct = default)
    {
        var incoming = tenders.ToList();
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
}