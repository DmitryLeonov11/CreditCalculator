namespace CreditCalculator.Calculations.Models;

public sealed record PaymentScheduleResult(
    IReadOnlyList<PaymentScheduleItem> Payments,
    decimal TotalPaid,
    decimal Overpayment);
