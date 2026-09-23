namespace CreditCalculator.Api.Contracts;

public sealed record EarlyRepaymentResponse(
    ScheduleResponse Original,
    ScheduleResponse WithEarlyRepayments);
