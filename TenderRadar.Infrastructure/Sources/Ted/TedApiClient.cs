using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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

    public async IAsyncEnumerable<TedNotice> SearchAllAsync(
        string query, string[] fields, int pageLimit, int maxPages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? token = null;
        var page = 0;

        do
        {
            var request = new TedSearchRequest
            {
                Query = query,
                Fields = fields,
                Limit = pageLimit,
                IterationNextToken = token
            };

            var response = await SearchAsync(request, ct);

            if (response.TimedOut)
                Console.Error.WriteLine("Warning: TED search timed out, results may be incomplete");

            foreach (var notice in response.Notices)
                yield return notice;

            token = response.IterationNextToken;
            page++;
        }
        while (token is not null && page < maxPages);
    }
}