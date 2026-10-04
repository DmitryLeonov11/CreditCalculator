using System.Globalization;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Calculations.Schedules;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class DebtToIncomeScoringRule : IScoringRule
{
    public const string Code = "DEBT_TO_INCOME";
    private const decimal MaximumStandardRatio = 0.40m;
    private const decimal MaximumScoringRatio = 0.25m;
    private const int MaximumRatioPoints = 20;
    private const int MediumRatioPoints = 10;

    private readonly ScoringOptions _options;

    public DebtToIncomeScoringRule(IOptions<ScoringOptions> options)
    {
        _options = options.Value;
    }

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.ExistingPaymentsAtApply is not { } existingPayments
            || ScoringIncome.CalculateAvailableIncome(application, _options.MinimumLivingWageByn) is not { } availableIncome)
        {
            return new ScoringRuleResult(Code, "Показатель долговой нагрузки", false, 0, "В снимке заявки отсутствуют необходимые данные об анкете.", IsStopRule: true);
        }

        if (application.CreditProduct is null)
            return new ScoringRuleResult(Code, "Показатель долговой нагрузки", false, 0, "В заявке отсутствует кредитный продукт.", IsStopRule: true);

        if (availableIncome <= 0m)
        {
            return new ScoringRuleResult(
                Code,
                "Показатель долговой нагрузки",
                false,
                0,
                "Доступный доход после учёта иждивенцев не превышает ноль.",
                IsStopRule: true);
        }

        var monthlyPayment = AnnuityScheduleCalculator.CalculateMonthlyPayment(
            application.Amount,
            application.InterestRate,
            application.TermMonths);
        var ratio = (monthlyPayment + existingPayments) / availableIncome;
        var limit = application.CreditProduct.IsBelarusianMade
            ? _options.BelarusianGoodsPdnThreshold
            : MaximumStandardRatio;

        if (ratio > limit)
        {
            return new ScoringRuleResult(
                Code,
                "Показатель долговой нагрузки",
                false,
                0,
                $"ПДН — {ratio.ToString("P1", CultureInfo.InvariantCulture)}; предельное значение — {limit.ToString("P0", CultureInfo.InvariantCulture)}.",
                IsStopRule: true);
        }

        var points = ratio <= MaximumScoringRatio ? MaximumRatioPoints + 20 : MediumRatioPoints + 15;
        var details = $"ПДН — {ratio.ToString("P1", CultureInfo.InvariantCulture)}; начислено {points} баллов. Доступный доход рассчитан с учётом иждивенцев.";
        return new ScoringRuleResult(Code, "Показатель долговой нагрузки", true, points, details);
    }
}
