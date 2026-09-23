namespace CreditCalculator.Api.Contracts;

public sealed record ScheduleItemResponse(
    int Number,
    DateOnly Date,
    decimal Payment,
    decimal InterestPart,
    decimal PrincipalPart,
    decimal RemainingBalance);
