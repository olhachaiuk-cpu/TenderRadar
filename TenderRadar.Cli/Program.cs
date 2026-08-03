using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenderRadar.Application.Configuration;
using TenderRadar.Infrastructure.Sources.Ted;
using TenderRadar.Infrastructure.Sources.Ted.Dto;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Services.Configure<TedOptions>(
    builder.Configuration.GetSection(TedOptions.SectionName));
builder.Services.Configure<ScoringOptions>(
    builder.Configuration.GetSection(ScoringOptions.SectionName));

builder.Services.AddHttpClient<TedApiClient>((sp, c) =>
{
    var opt = sp.GetRequiredService<IOptions<TedOptions>>().Value;
    c.BaseAddress = new Uri(opt.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
});

var host = builder.Build();

var client = host.Services.GetRequiredService<TedApiClient>();
var tedOptions = host.Services.GetRequiredService<IOptions<TedOptions>>().Value;

var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-tedOptions.LookbackDays);

var request = new TedSearchRequest
{
    Query = $"classification-cpv IN ({string.Join(' ', tedOptions.CpvCodes)}) " +
            $"AND publication-date >= {from:yyyyMMdd}",
    Fields = tedOptions.Fields,
    Limit = 5
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
{
    Console.WriteLine($"""
                       {t.PublicationNumber} | {t.Country} | {t.PublicationDate}
                         {t.Title}
                         Замовник: {t.BuyerName}
                         Дедлайн:  {t.SubmissionDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "-"}
                         CPV:      {string.Join(", ", t.CpvCodes.Take(5))}
                         Сума:     {t.EstimatedValue?.ToString() ?? "-"} {t.Currency}
                         {t.Url}
                       """);
}