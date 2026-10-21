namespace CreditCalculator.Scoring.Messages.RabbitMq;

public static class QueueNames
{
    public const string ScoringRequest = "scoring.requests";
    public const string ScoringResult = "scoring.results";
    public const string Exchange = "creditcalculator.scoring";
}