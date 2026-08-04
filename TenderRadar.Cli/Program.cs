using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenderRadar.Application.Configuration;
using TenderRadar.Application.Services;
using TenderRadar.Infrastructure.Persistence;
using TenderRadar.Infrastructure.Sources.Ted;
using TenderRadar.Infrastructure.Sources.Ted.Dto;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Services.Configure<TedOptions>(
    builder.Configuration.GetSection(TedOptions.SectionName));
builder.Services.Configure<ScoringOptions>(
    builder.Configuration.GetSection(ScoringOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITenderRepository, TenderRepository>();

builder.Services.AddSingleton(_ =>
{
    using var doc = JsonDocument.Parse(File.ReadAllText("cpv-codes.json"));
    var root = doc.RootElement;

    string[] Read(string name) => root.TryGetProperty(name, out var el)
        ? el.EnumerateArray().Select(x => x.GetString()!).ToArray()
        : [];

    return new CpvCatalog(Read("DirectHit"), Read("Development"));
});

builder.Services.AddSingleton(sp => new RelevanceScorer(
    sp.GetRequiredService<IOptions<ScoringOptions>>().Value,
    sp.GetRequiredService<CpvCatalog>()));

builder.Services.AddHttpClient<TedApiClient>((sp, c) =>
{
    var opt = sp.GetRequiredService<IOptions<TedOptions>>().Value;
    c.BaseAddress = new Uri(opt.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
});

var host = builder.Build();

var client = host.Services.GetRequiredService<TedApiClient>();
var repo = host.Services.GetRequiredService<ITenderRepository>();
var tedOptions = host.Services.GetRequiredService<IOptions<TedOptions>>().Value;
var scoringOptions = host.Services.GetRequiredService<IOptions<ScoringOptions>>().Value;
var scorer = host.Services.GetRequiredService<RelevanceScorer>();

var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-tedOptions.LookbackDays);

var request = new TedSearchRequest
{
    Query = $"classification-cpv IN ({string.Join(' ', tedOptions.CpvCodes)}) " +
            $"AND publication-date >= {from:yyyyMMdd}",
    Fields = tedOptions.Fields,
    Limit = 250
};

var result = await client.SearchAsync(request);
Console.WriteLine($"Query: {request.Query}");
Console.WriteLine($"Total: {result.TotalNoticeCount}");

var tenders = result.Notices
    .Select(TedNoticeMapper.Map)
    .Where(t => t is not null)
    .Select(t => t!)
    .ToList();

foreach (var t in tenders)
    scorer.Score(t);

await repo.UpsertRangeAsync(tenders);

var relevant = tenders
    .Where(t => t.Score >= scoringOptions.MinScore)
    .OrderByDescending(t => t.Score)
    .ToList();

Console.WriteLine($"Отримано: {tenders.Count}, збережено в БД: {tenders.Count}, релевантних: {relevant.Count} (поріг {scoringOptions.MinScore})");

foreach (var t in relevant)
{
    Console.WriteLine($"""
                       {t.PublicationNumber} | {t.Country} | {t.PublicationDate:yyyy-MM-dd} | score {t.Score}
                         {t.ShortTitle ?? t.Title}
                         Збіги:    {string.Join(", ", t.MatchedKeywords)}
                         Замовник: {t.BuyerName}
                         Дедлайн:  {t.SubmissionDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "-"}
                         CPV:      {string.Join(", ", t.CpvCodes.Take(5))}
                         Сума:     {t.EstimatedValue?.ToString() ?? "-"} {t.Currency}
                         {t.Url}
                       """);
}