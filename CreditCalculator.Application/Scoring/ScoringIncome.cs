using ApplicationEntity = CreditCalculator.Domain.Entities.Application;

namespace CreditCalculator.Application.Scoring;

internal static class ScoringIncome
{
    public static decimal? CalculateAvailableIncome(ApplicationEntity application, decimal minimumLivingWageByn)
    {
        if (application.IncomeAtApply is not { } income || application.DependentsAtApply is not { } dependents)
            return null;

        return income - dependents * minimumLivingWageByn;
    }
}
