using System.Text.RegularExpressions;
using TenderRadar.Application.Configuration;
using TenderRadar.Domain;

namespace TenderRadar.Application.Services;

public sealed class RelevanceScorer(ScoringOptions options, CpvCatalog cpvCatalog)
{
    public void Score(Tender tender)
    {
        var score = 0;
        var matched = new List<string>();

        var cpv = cpvCatalog.Match(tender.CpvCodes);

        if (IsSignificant(cpv.DirectCount, cpv.Total))
        {
            score += options.CpvWeights.DirectHit;
            matched.Add($"cpv:direct {cpv.DirectCount}/{cpv.Total}");
        }
        else if (IsSignificant(cpv.DevelopmentCount, cpv.Total))
        {
            score += options.CpvWeights.Development;
            matched.Add($"cpv:dev {cpv.DevelopmentCount}/{cpv.Total}");
        }

        var haystack = string.IsNullOrEmpty(tender.SearchText)
            ? tender.Title
            : tender.SearchText;

        foreach (var rule in options.Keywords)
        {
            if (Matches(haystack, rule))
            {
                matched.Add(rule.Phrase);
                score += rule.Weight;
            }
        }

        tender.MatchedKeywords = matched.ToArray();
        tender.Score = score;
    }

    private static bool Matches(string text, KeywordRule rule)
    {
        if (rule.WholeWordOnly)
        {
            var pattern = $@"\b{Regex.Escape(rule.Phrase)}\b";
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
        }

        return text.Contains(rule.Phrase, StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool IsSignificant(int count, int total)
        => count > 0 && (count >= 2 || (double)count / total >= 0.34);
}