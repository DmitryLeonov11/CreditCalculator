using CreditCalculator.Calculations.Models;

namespace CreditCalculator.Calculations.Schedules;

public static class EffectiveRateCalculator
{
    private const int MaxIterations = 200;
    private const decimal Tolerance = 0.0000001m;

    public static decimal CalculateMonthlyRate(decimal principal, IReadOnlyList<PaymentScheduleItem> payments)
    {
        if (principal <= 0m)
            throw new ArgumentOutOfRangeException(nameof(principal), principal, "Сумма кредита должна быть больше нуля.");

        var low = 0m;
        var high = 1m;

        while (PresentValue(payments, high) > principal)
            high *= 2;

        for (var i = 0; i < MaxIterations; i++)
        {
            var mid = (low + high) / 2;
            var presentValue = PresentValue(payments, mid);

            if (Math.Abs(presentValue - principal) < Tolerance)
                return mid;

            if (presentValue > principal)
                low = mid;
            else
                high = mid;
        }

        return (low + high) / 2;
    }

    public static decimal Calculate(decimal principal, IReadOnlyList<PaymentScheduleItem> payments)
    {
        var monthlyRate = CalculateMonthlyRate(principal, payments);
        return Math.Round((DecimalPow(1m + monthlyRate, 12) - 1m) * 100m, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal PresentValue(IReadOnlyList<PaymentScheduleItem> payments, decimal monthlyRate)
    {
        var presentValue = 0m;
        foreach (var payment in payments)
            presentValue += payment.Payment / DecimalPow(1m + monthlyRate, payment.Number);

        return presentValue;
    }

    private static decimal DecimalPow(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
        {
            try
            {
                result *= value;
            }
            catch (OverflowException)
            {
                return decimal.MaxValue;
            }
        }

        return result;
    }
}
