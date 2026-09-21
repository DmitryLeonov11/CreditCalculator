using CreditCalculator.Calculations.Models;

namespace CreditCalculator.Calculations.Validation;

internal static class ScheduleInputValidator
{
    public static void Validate(decimal principal, decimal annualRatePercent, int termMonths)
    {
        if (principal <= 0m)
            throw new ArgumentOutOfRangeException(nameof(principal), principal, "Сумма кредита должна быть больше нуля.");

        if (termMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(termMonths), termMonths, "Срок кредита должен быть больше нуля месяцев.");

        if (annualRatePercent < 0m)
            throw new ArgumentOutOfRangeException(nameof(annualRatePercent), annualRatePercent, "Ставка не может быть отрицательной.");
    }

    public static void ValidateEarlyRepayments(IReadOnlyList<EarlyRepayment> earlyRepayments, int termMonths)
    {
        foreach (var earlyRepayment in earlyRepayments)
        {
            if (earlyRepayment.Month <= 0 || earlyRepayment.Month > termMonths)
                throw new ArgumentOutOfRangeException(nameof(earlyRepayments), earlyRepayment.Month, "Месяц досрочного погашения выходит за пределы срока кредита.");

            if (earlyRepayment.Amount <= 0m)
                throw new ArgumentOutOfRangeException(nameof(earlyRepayments), earlyRepayment.Amount, "Сумма досрочного погашения должна быть больше нуля.");
        }
    }
}
