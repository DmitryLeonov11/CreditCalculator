namespace CreditCalculator.Calculations;

public sealed record PaymentScheduleItem(
    int Number,
    DateOnly Date,
    decimal Payment,
    decimal InterestPart,
    decimal PrincipalPart,
    decimal RemainingBalance);
