using System.Globalization;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class MortgageDownPaymentScoringRule : IScoringRule
{
    public const string Code = "MORTGAGE_DOWN_PAYMENT";
    private const decimal MinimumDownPaymentRatio = 0.20m;
    private const int PenaltyPoints = -10;

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.CreditProduct is null)
            return new ScoringRuleResult(Code, "Первоначальный взнос", false, 0, "В заявке отсутствует кредитный продукт.", IsStopRule: true);

        if (application.CreditProduct.Purpose != CreditPurpose.Mortgage)
            return new ScoringRuleResult(Code, "Первоначальный взнос", true, 0, "Правило применяется только к ипотечным продуктам.");

        if (application.DownPaymentAmountAtApply is not { } downPayment || downPayment < 0m)
            return new ScoringRuleResult(Code, "Первоначальный взнос", false, 0, "В снимке ипотечной заявки отсутствует корректная сумма первоначального взноса.", IsStopRule: true);

        var propertyValue = application.Amount + downPayment;
        var ratio = propertyValue == 0m ? 0m : downPayment / propertyValue;
        var points = ratio < MinimumDownPaymentRatio ? PenaltyPoints : 0;
        var details = points < 0
            ? $"Первоначальный взнос — {ratio.ToString("P1", CultureInfo.InvariantCulture)} стоимости объекта; начислен штраф {Math.Abs(points)} баллов."
            : $"Первоначальный взнос — {ratio.ToString("P1", CultureInfo.InvariantCulture)} стоимости объекта.";

        return new ScoringRuleResult(Code, "Первоначальный взнос", true, points, details);
    }
}
