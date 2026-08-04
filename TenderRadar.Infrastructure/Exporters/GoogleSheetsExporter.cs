using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using TenderRadar.Application.Configuration;
using TenderRadar.Domain;

namespace TenderRadar.Infrastructure.Exporters;

public interface ITenderExporter
{
    Task<int> ExportNewAsync(IReadOnlyCollection<Tender> tenders, CancellationToken ct = default);
}

public sealed class GoogleSheetsExporter : ITenderExporter
{
    private static readonly string[] Scopes = [SheetsService.Scope.Spreadsheets];
    private static readonly string[] Headers =
    [
        "Додано", "Дата публікації", "Дедлайн", "Назва", "Назва ENG",
        "Замовник", "Опис", "Опис ENG", "Країна", "CPV", "Сума", "Валюта", "Ключові слова", "Посилання"
    ];

    private readonly GoogleSheetsOptions _options;
    private readonly SheetsService _service;

    public GoogleSheetsExporter(GoogleSheetsOptions options)
    {
        _options = options;

        var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

        GoogleCredential credential;
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            credential = GoogleCredential.FromJson(credentialsJson).CreateScoped(Scopes);
        }
        else
        {
            var credPath = Path.Combine(AppContext.BaseDirectory, options.CredentialsPath);
            credential = GoogleCredential.FromFile(credPath).CreateScoped(Scopes);
        }

        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "TenderRadar"
        });
    }

    private async Task EnsureHeaderAsync(CancellationToken ct)
    {
        var range = $"{_options.SheetName}!A1:{ColumnLetter(Headers.Length)}1";
        var response = await _service.Spreadsheets.Values.Get(_options.SpreadsheetId, range).ExecuteAsync(ct);

        if (response.Values is { Count: > 0 }) return;

        var body = new ValueRange { Values = [Headers.Cast<object>().ToList()] };
        var request = _service.Spreadsheets.Values.Update(body, _options.SpreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await request.ExecuteAsync(ct);
    }
    
    private static string ColumnLetter(int count)
    {
        var letter = (char)('A' + count - 1);
        return letter.ToString();
    }
    
    private static string FormatMatches(Tender t)
    {
        var parts = new List<string>();

        var keywords = t.MatchedKeywords.Where(m => !m.StartsWith("cpv:"));
        parts.AddRange(keywords);

        return string.Join(", ", parts);
    }
    
    public async Task<int> ExportNewAsync(IReadOnlyCollection<Tender> tenders, CancellationToken ct = default)
    {
        if (tenders.Count == 0) return 0;

        await EnsureHeaderAsync(ct);

        var startRow = await GetNextRowNumberAsync(ct);

        var rows = tenders
            .Select((t, i) => ToRow(t, startRow + i))
            .ToList();

        var range = $"{_options.SheetName}!A{startRow}";
        var body = new ValueRange { Values = rows };

        var request = _service.Spreadsheets.Values.Update(body, _options.SpreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        await request.ExecuteAsync(ct);

        return tenders.Count;
    }

    private async Task<int> GetNextRowNumberAsync(CancellationToken ct)
    {
        var range = $"{_options.SheetName}!A:A";
        var response = await _service.Spreadsheets.Values.Get(_options.SpreadsheetId, range).ExecuteAsync(ct);
        var filledRows = response.Values?.Count ?? 0;
        return filledRows + 1;
    }

    private static IList<object> ToRow(Tender t, int row) =>
    [
        t.FirstSeenAt.ToString("yyyy-MM-dd"),
        t.PublicationDate.ToString("yyyy-MM-dd"),
        t.SubmissionDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "",
        t.ShortTitle ?? t.Title,
        $"=GOOGLETRANSLATE(D{row};\"auto\";\"en\")",
        t.BuyerName ?? "",
        t.Summary ?? "",
        $"=GOOGLETRANSLATE(G{row};\"auto\";\"en\")",
        t.Country ?? "",
        string.Join(", ", t.CpvCodes.Take(5)),
        t.EstimatedValue?.ToString() ?? "",
        t.Currency ?? "",
        FormatMatches(t),
        t.Url
    ];
}