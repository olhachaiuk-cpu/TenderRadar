namespace TenderRadar.Application.Configuration;

public sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    public string SpreadsheetId { get; init; } = "";
    public string SheetName { get; init; } = "TED";
    public string CredentialsPath { get; init; } = "";
}