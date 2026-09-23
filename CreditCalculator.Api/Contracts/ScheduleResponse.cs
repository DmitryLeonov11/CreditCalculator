namespace CreditCalculator.Api.Contracts;

public sealed record ScheduleResponse(
    IReadOnlyList<ScheduleItemResponse> Payments,
    decimal MonthlyPayment,
    decimal TotalPaid,
    decimal Overpayment,
    decimal EffectiveRate);
