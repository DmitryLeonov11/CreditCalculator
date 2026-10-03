using System.Globalization;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class DependentsScoringRule : IScoringRule
{
    public const string Code = "DEPENDENTS";

    private readonly ScoringOptions _options;

    public DependentsScoringRule(IOptions<ScoringOptions> options)
    {
        _options = options.Value;
    }

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.DependentsAtApply is not { } dependents
            || application.IncomeAtApply is not { } income)
        {
            return new ScoringRuleResult(Code, "Иждивенцы", false, 0, "В снимке заявки отсутствуют данные о доходе или количестве иждивенцев.");
        }

        var deduction = dependents * _options.MinimumLivingWageByn;
        var availableIncome = income - deduction;
        var details = $"Иждивенцев: {dependents}; вычет — {deduction.ToString("F2", CultureInfo.InvariantCulture)} BYN; доступный доход — {availableIncome.ToString("F2", CultureInfo.InvariantCulture)} BYN.";

        return new ScoringRuleResult(Code, "Иждивенцы", true, 0, details);
    }
}
