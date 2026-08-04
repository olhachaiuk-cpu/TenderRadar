using System.Globalization;
using TenderRadar.Domain;
using TenderRadar.Infrastructure.Sources.Ted.Dto;

namespace TenderRadar.Infrastructure.Sources.Ted;

public static class TedNoticeMapper
{
    private const string Source = "TED";
    private const string PreferredLanguage = "eng";

    public static Tender? Map(TedNotice notice)
    {
        if (string.IsNullOrWhiteSpace(notice.PublicationNumber))
            return null;

        var title = PickText(notice.NoticeTitle);
        if (title is null)
            return null;

        return new Tender
        {
            PublicationNumber = notice.PublicationNumber,
            Source            = Source,
            Title             = title,
            ShortTitle        = ExtractShortTitle(title),
            BuyerName         = PickFirstFromLists(notice.BuyerName),
            Country           = notice.BuyerCountry?.Distinct().FirstOrDefault(),
            PublicationDate   = ParseDate(notice.PublicationDate),
            SubmissionDeadline= ParseEarliestDeadline(notice.DeadlineReceiptRequest),
            CpvCodes          = notice.ClassificationCpv?.Distinct().ToArray() ?? [],
            EstimatedValue    = notice.TotalValue,
            Currency          = notice.TotalValueCurrency?.Distinct().FirstOrDefault(),
            Url               = BuildUrl(notice.PublicationNumber),
            FirstSeenAt       = DateTimeOffset.UtcNow,
            SearchText        = BuildSearchText(notice.NoticeTitle),
        };
    }

    private static string? PickText(Dictionary<string, string>? dict)
    {
        if (dict is null || dict.Count == 0) return null;

        return dict.TryGetValue(PreferredLanguage, out var preferred)
            ? preferred
            : dict.Values.FirstOrDefault();
    }
    
    private static string? PickFirstFromLists(Dictionary<string, List<string>>? dict)
    {
        if (dict is null || dict.Count == 0) return null;

        var list = dict.TryGetValue(PreferredLanguage, out var preferred)
            ? preferred
            : dict.Values.FirstOrDefault();

        return list?.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private static DateOnly ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 10)
            return default;

        return DateOnly.TryParseExact(
            raw[..10], "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : default;
    }

    private static DateTimeOffset? ParseEarliestDeadline(List<string>? raw)
    {
        if (raw is null || raw.Count == 0) return null;

        var parsed = raw
            .Select(s => DateTimeOffset.TryParse(
                s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ? d : (DateTimeOffset?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value.ToUniversalTime())
            .ToList();

        return parsed.Count > 0 ? parsed.Min() : null;
    }
    
    private static string? ExtractShortTitle(string title)
    {
        const string sep = " – ";
        var first = title.IndexOf(sep, StringComparison.Ordinal);
        if (first < 0) return null;

        var second = title.IndexOf(sep, first + sep.Length, StringComparison.Ordinal);
        if (second < 0) return null;

        var result = title[(second + sep.Length)..].Trim();
        return result.Length > 0 ? result : null;
    }
    
    private static string BuildSearchText(Dictionary<string, string>? titles)
    {
        if (titles is null || titles.Count == 0) return string.Empty;

        return string.Join(" | ", titles.Values
            .Select(t => ExtractShortTitle(t) ?? t)
            .Distinct());
    }

    private static string BuildUrl(string publicationNumber)
        => $"https://ted.europa.eu/en/notice/-/detail/{publicationNumber}";
}