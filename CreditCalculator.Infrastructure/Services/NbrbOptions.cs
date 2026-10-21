namespace CreditCalculator.Infrastructure.Services;

public sealed class NbrbOptions
{
    public const string SectionName = "Nbrb";

    public string BaseUrl { get; set; } = "https://www.nbrb.by/api";
    public int UpdateIntervalHours { get; set; } = 6;
    public bool Enabled { get; set; } = true;

    public TimeSpan UpdateInterval => TimeSpan.FromHours(UpdateIntervalHours);
}