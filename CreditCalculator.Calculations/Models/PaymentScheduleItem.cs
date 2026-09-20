namespace CreditCalculator.Calculations.Models;

public sealed record PaymentScheduleItem(
    int Number,
    DateOnly Date,
    decimal Payment,
    decimal InterestPart,
    decimal PrincipalPart,
    decimal RemainingBalance);
