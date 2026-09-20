namespace CreditCalculator.Calculations;

internal static class ScheduleInputValidator
{
    public static void Validate(decimal principal, decimal annualRatePercent, int termMonths)
    {
        if (principal <= 0m)
            throw new ArgumentOutOfRangeException(nameof(principal), principal, "Principal must be positive.");

        if (termMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(termMonths), termMonths, "Term must be positive.");

        if (annualRatePercent < 0m)
            throw new ArgumentOutOfRangeException(nameof(annualRatePercent), annualRatePercent, "Rate cannot be negative.");
    }
}
