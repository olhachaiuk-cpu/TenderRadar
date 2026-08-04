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
        "Додано", "Дата публікації", "Дедлайн", "Score", "Назва",
        "Замовник", "Опис", "Країна", "CPV", "Сума", "Валюта", "Ключові слова", "Посилання"
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

    public async Task<int> ExportNewAsync(IReadOnlyCollection<Tender> tenders, CancellationToken ct = default)
    {
        if (tenders.Count == 0) return 0;

        await EnsureHeaderAsync(ct);

        var rows = tenders.Select(ToRow).ToList();

        var range = $"{_options.SheetName}!A:{ColumnLetter(Headers.Length)}";
        var body = new ValueRange { Values = rows };

        var request = _service.Spreadsheets.Values.Append(body, _options.SpreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync(ct);

        return tenders.Count;
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

    private static IList<object> ToRow(Tender t) =>
    [
        t.FirstSeenAt.ToString("yyyy-MM-dd"),
        t.PublicationDate.ToString("yyyy-MM-dd"),
        t.SubmissionDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "",
        t.Score,
        t.ShortTitle ?? t.Title,
        t.BuyerName ?? "",
        t.Summary ?? "",
        t.Country ?? "",
        string.Join(", ", t.CpvCodes.Take(5)),
        t.EstimatedValue?.ToString() ?? "",
        t.Currency ?? "",
        string.Join(", ", t.MatchedKeywords),
        t.Url
    ];
}