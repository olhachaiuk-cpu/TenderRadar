using System.Text.RegularExpressions;
using TenderRadar.Application.Configuration;
using TenderRadar.Domain;

namespace TenderRadar.Application.Services;

public sealed class RelevanceScorer(ScoringOptions options)
{
    public void Score(Tender tender)
    {
        var haystack = string.IsNullOrEmpty(tender.SearchText)
            ? tender.Title
            : tender.SearchText;
        var matched = new List<string>();
        var score = 0;

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
}