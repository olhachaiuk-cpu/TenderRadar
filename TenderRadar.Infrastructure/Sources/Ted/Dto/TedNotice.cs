using System.Text.Json.Serialization;

namespace TenderRadar.Infrastructure.Sources.Ted.Dto;

public sealed class TedNotice
{
    [JsonPropertyName("publication-number")]
    public string PublicationNumber { get; init; } = default!;

    [JsonPropertyName("notice-title")]
    public Dictionary<string, string>? NoticeTitle { get; init; }

    [JsonPropertyName("buyer-name")]
    public Dictionary<string, List<string>>? BuyerName { get; init; }

    [JsonPropertyName("buyer-country")]
    public List<string>? BuyerCountry { get; init; }

    [JsonPropertyName("publication-date")]
    public string? PublicationDate { get; init; }

    [JsonPropertyName("deadline-receipt-request")]
    public List<string>? DeadlineReceiptRequest { get; init; }

    [JsonPropertyName("classification-cpv")]
    public List<string>? ClassificationCpv { get; init; }

    [JsonPropertyName("total-value")]
    public decimal? TotalValue { get; init; }

    [JsonPropertyName("total-value-cur")]
    public List<string>? TotalValueCurrency { get; init; }
    
    [JsonPropertyName("description-lot")]
    public Dictionary<string, List<string>>? DescriptionLot { get; init; }
}