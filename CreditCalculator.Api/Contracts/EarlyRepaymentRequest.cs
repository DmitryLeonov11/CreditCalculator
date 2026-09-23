namespace CreditCalculator.Api.Contracts;

public sealed record EarlyRepaymentRequest(
    decimal Amount,
    int TermMonths,
    decimal AnnualRatePercent,
    DateOnly FirstPaymentDate,
    EarlyRepaymentMode Mode,
    IReadOnlyList<EarlyRepaymentItemRequest> EarlyRepayments);
