namespace TenderRadar.Domain;

public class Tender
{
    public required string PublicationNumber { get; set; }
    public required string Source { get; set; }
    public required string Title { get; set; }
    public string? ShortTitle { get; set; }
    public string? BuyerName { get; set; }
    public string? Country { get; set; }
    public DateOnly PublicationDate { get; set; }
    public DateTimeOffset? SubmissionDeadline { get; set; }
    public string[] CpvCodes { get; set; } = [];
    public decimal? EstimatedValue { get; set; }
    public string? Currency { get; set; }
    public string Url { get; set; } = "";
    public string[] MatchedKeywords { get; set; } = [];
    public int Score { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset? ExportedAt { get; set; }
}