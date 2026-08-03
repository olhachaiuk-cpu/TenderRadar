using System.Text.Json.Serialization;

namespace TenderRadar.Infrastructure.Sources.Ted.Dto;

public sealed class TedSearchResponse
{
    [JsonPropertyName("notices")] 
    public List<TedNotice> Notices { get; init; } = [];
    
    [JsonPropertyName("totalNoticeCount")] 
    public int TotalNoticeCount { get; init; }
    
    [JsonPropertyName("iterationNextToken")] 
    public string? IterationNextToken { get; init; }
    
    [JsonPropertyName("timedOut")] 
    public bool TimedOut { get; init; }
}