using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Application.Scoring;
using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Scoring.Rules;

public sealed class AgeScoringRule : IScoringRule
{
    public const string Code = "AGE_ELIGIBILITY";
    private const int MinimumAge = 21;

    public ScoringRuleResult Evaluate(ApplicationEntity application)
    {
        if (application.BirthDateAtApply is not { } birthDate || application.GenderAtApply is not { } gender)
        {
            return new ScoringRuleResult(Code, "Возраст клиента", false, 0, "В снимке заявки отсутствуют дата рождения или пол клиента.");
        }

        var appliedAt = DateOnly.FromDateTime(application.CreatedAt.UtcDateTime);
        var ageAtApply = GetAgeOn(birthDate, appliedAt);
        var retirementAge = gender switch
        {
            Gender.Male => 63,
            Gender.Female => 58,
            _ => throw new ArgumentOutOfRangeException(nameof(application), "Неизвестное значение пола клиента.")
        };
        var maturityDate = appliedAt.AddMonths(application.TermMonths);
        var retirementDate = birthDate.AddYears(retirementAge);
        var passed = ageAtApply >= MinimumAge && maturityDate < retirementDate;
        var details = passed
            ? $"Возраст на дату заявки — {ageAtApply} лет; кредит будет погашен до достижения пенсионного возраста ({retirementAge} лет)."
            : ageAtApply < MinimumAge
                ? $"Возраст на дату заявки — {ageAtApply} лет; минимальный возраст — {MinimumAge} лет."
                : $"Срок кредита заканчивается {maturityDate:dd.MM.yyyy}, после достижения пенсионного возраста ({retirementAge} лет) {retirementDate:dd.MM.yyyy}.";

        return new ScoringRuleResult(Code, "Возраст клиента", passed, 0, details);
    }

    private static int GetAgeOn(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        return date < birthDate.AddYears(age) ? age - 1 : age;
    }
}