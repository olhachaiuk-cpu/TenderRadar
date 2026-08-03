using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TenderRadar.Infrastructure.Sources.Ted.Dto;

namespace TenderRadar.Infrastructure.Sources.Ted;

public sealed class TedApiClient (HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<TedSearchResponse> SearchAsync(
        TedSearchRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "v3/notices/search", request, JsonOptions, ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TedSearchResponse>(
            JsonOptions, ct) ?? throw new InvalidOperationException("Empty response");
    }
}