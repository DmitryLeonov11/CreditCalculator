using System.Globalization;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class LoanToIncomeScoringRule : IScoringRule
{
    public const string Code = "LOAN_TO_INCOME";
    private const decimal MaximumIncomeMultiple = 12m;
    private const int EligiblePoints = 20;
    private const int PenaltyPoints = -10;

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.IncomeAtApply is not { } income || income <= 0m)
            return new ScoringRuleResult(Code, "Сумма кредита к доходу", false, 0, "В снимке заявки отсутствует положительный среднемесячный доход.", IsStopRule: true);

        var multiple = application.Amount / income;
        var points = multiple > MaximumIncomeMultiple ? PenaltyPoints : EligiblePoints;
        var details = points < 0
            ? $"Сумма кредита составляет {multiple.ToString("F1", CultureInfo.InvariantCulture)} среднемесячных доходов; начислен штраф {Math.Abs(points)} баллов."
            : $"Сумма кредита составляет {multiple.ToString("F1", CultureInfo.InvariantCulture)} среднемесячных доходов.";

        return new ScoringRuleResult(Code, "Сумма кредита к доходу", true, points, details);
    }
}
