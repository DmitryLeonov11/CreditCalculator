using CreditCalculator.Calculations.Models;

namespace CreditCalculator.Api.Contracts;

public sealed record ScheduleRequest(
    decimal Amount,
    int TermMonths,
    decimal AnnualRatePercent,
    PaymentType PaymentType,
    DateOnly FirstPaymentDate);
