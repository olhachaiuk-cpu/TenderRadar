using System.Text.Json.Serialization;

namespace TenderRadar.Infrastructure.Sources.Ted.Dto;

public sealed class TedSearchRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    [JsonPropertyName("fields")]
    public required string[] Fields { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 250;

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = "ACTIVE";

    [JsonPropertyName("paginationMode")]
    public string PaginationMode { get; init; } = "ITERATION";

    [JsonPropertyName("onlyLatestVersions")]
    public bool OnlyLatestVersions { get; init; } = true;

    [JsonPropertyName("iterationNextToken")]
    public string? IterationNextToken { get; init; }
}