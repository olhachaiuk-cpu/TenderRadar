namespace TenderRadar.Application.Configuration;

public sealed class TedOptions
{
    public const string SectionName = "Ted";

    public string BaseUrl { get; init; } = "https://api.ted.europa.eu/";
    public int TimeoutSeconds { get; init; } = 60;
    public int PageLimit { get; init; } = 250;
    public int LookbackDays { get; init; } = 7;
    public string[] CpvCodes { get; init; } = [];
    public string[] Fields { get; init; } = [];
    public int MaxPages { get; init; } = 20;
}