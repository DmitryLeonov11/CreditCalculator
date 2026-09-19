namespace CreditCalculator.Calculations;

public sealed record PaymentScheduleResult(
    IReadOnlyList<PaymentScheduleItem> Payments,
    decimal TotalPaid,
    decimal Overpayment);
