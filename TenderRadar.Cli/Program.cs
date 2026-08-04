using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using TenderRadar.Application.Configuration;
using TenderRadar.Application.Services;
using TenderRadar.Domain;
using TenderRadar.Infrastructure.Exporters;
using TenderRadar.Infrastructure.Persistence;
using TenderRadar.Infrastructure.Sources.Ted;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.Configure<TedOptions>(
    builder.Configuration.GetSection(TedOptions.SectionName));
builder.Services.Configure<ScoringOptions>(
    builder.Configuration.GetSection(ScoringOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITenderRepository, TenderRepository>();

builder.Services.Configure<GoogleSheetsOptions>(
    builder.Configuration.GetSection(GoogleSheetsOptions.SectionName));

builder.Services.AddSingleton(sp =>
    new GoogleSheetsExporter(sp.GetRequiredService<IOptions<GoogleSheetsOptions>>().Value));

builder.Services.AddSingleton<ITenderExporter>(sp =>
    sp.GetRequiredService<GoogleSheetsExporter>());

builder.Services.AddSingleton(_ =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "cpv-codes.json");
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
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
})
.AddResilienceHandler("ted-retry", pipeline =>
{
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(2),
        UseJitter = true,
        ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            || args.Outcome.Result?.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
    });
});

var host = builder.Build();

var client = host.Services.GetRequiredService<TedApiClient>();
var repo = host.Services.GetRequiredService<ITenderRepository>();
var tedOptions = host.Services.GetRequiredService<IOptions<TedOptions>>().Value;
var scoringOptions = host.Services.GetRequiredService<IOptions<ScoringOptions>>().Value;
var scorer = host.Services.GetRequiredService<RelevanceScorer>();

var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-tedOptions.LookbackDays);

var query = $"classification-cpv IN ({string.Join(' ', tedOptions.CpvCodes)}) " +
            $"AND publication-date >= {from:yyyyMMdd}";

Console.WriteLine($"Query: {query}");

var tenders = new List<Tender>();

await foreach (var notice in client.SearchAllAsync(
                   query, tedOptions.Fields, tedOptions.PageLimit, tedOptions.MaxPages))
{
    var tender = TedNoticeMapper.Map(notice);
    if (tender is not null)
        tenders.Add(tender);
}

tenders = tenders
    .GroupBy(t => (t.Source, t.PublicationNumber))
    .Select(g => g.Last())
    .ToList();

Console.WriteLine($"Отримано (унікальних): {tenders.Count}");

foreach (var t in tenders)
    scorer.Score(t);

await repo.UpsertRangeAsync(tenders);

var relevant = tenders
    .Where(t => t.Score >= scoringOptions.MinScore)
    .OrderByDescending(t => t.Score)
    .ToList();

Console.WriteLine($"Отримано: {tenders.Count}, релевантних: {relevant.Count} (поріг {scoringOptions.MinScore})");

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

var exporter = host.Services.GetRequiredService<ITenderExporter>();
var exported = await exporter.ExportNewAsync(relevant);
Console.WriteLine($"Експортовано в Sheets: {exported}");