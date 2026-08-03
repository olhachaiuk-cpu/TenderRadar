using System.Text.Json.Serialization;

namespace TenderRadar.Infrastructure.Sources.Ted.Dto;

public sealed class TedSearchRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    [JsonPropertyName("fields")]
    public required string[] Fields { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = "ACTIVE";
}