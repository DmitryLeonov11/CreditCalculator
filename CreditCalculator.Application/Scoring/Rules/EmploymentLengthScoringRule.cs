using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class EmploymentLengthScoringRule : IScoringRule
{
    public const string Code = "EMPLOYMENT_LENGTH";
    private const int MinimumEmploymentMonths = 3;
    private const int BonusEmploymentMonths = 24;
    private const int BonusPoints = 5;

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.EmploymentMonthsAtApply is not { } employmentMonths)
            return new ScoringRuleResult(Code, "Стаж работы", false, 0, "В снимке заявки отсутствует стаж работы.", IsStopRule: true);

        if (employmentMonths < MinimumEmploymentMonths)
        {
            return new ScoringRuleResult(
                Code,
                "Стаж работы",
                false,
                0,
                $"Стаж работы — {employmentMonths} мес.; минимальный стаж — {MinimumEmploymentMonths} мес.",
                IsStopRule: true);
        }

        var points = employmentMonths > BonusEmploymentMonths ? BonusPoints + 10 : 10;
        var details = points > 0
            ? $"Стаж работы — {employmentMonths} мес.; начислено {points} бонусных баллов."
            : $"Стаж работы — {employmentMonths} мес.; бонус начисляется при стаже более {BonusEmploymentMonths} мес.";

        return new ScoringRuleResult(Code, "Стаж работы", true, points, details);
    }
}
