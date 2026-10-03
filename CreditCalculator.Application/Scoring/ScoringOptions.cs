namespace CreditCalculator.Application.Scoring;

public sealed class ScoringOptions
{
    public const string SectionName = "Scoring";

    public decimal MinimumLivingWageByn { get; set; } = 500m;
    public decimal BelarusianGoodsPdnThreshold { get; set; } = 0.50m;
}
