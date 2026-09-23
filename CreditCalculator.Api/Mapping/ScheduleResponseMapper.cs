using CreditCalculator.Api.Contracts;
using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Schedules;

namespace CreditCalculator.Api.Mapping;

public static class ScheduleResponseMapper
{
    public static ScheduleResponse ToResponse(decimal principal, PaymentScheduleResult scheduleResult)
    {
        var payments = scheduleResult.Payments
            .Select(item => new ScheduleItemResponse(
                item.Number,
                item.Date,
                item.Payment,
                item.InterestPart,
                item.PrincipalPart,
                item.RemainingBalance))
            .ToList();

        var effectiveRate = EffectiveRateCalculator.Calculate(principal, scheduleResult.Payments);

        return new ScheduleResponse(
            payments,
            payments[0].Payment,
            scheduleResult.TotalPaid,
            scheduleResult.Overpayment,
            effectiveRate);
    }
}
