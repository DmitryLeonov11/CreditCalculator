using CreditCalculator.Scoring.Messages.Messages;

namespace CreditCalculator.Scoring.Service.Scoring;

public interface IScoringEngine
{
    ScoringResult Evaluate(ScoringRequest request);
}

public sealed class ScoringEngine : IScoringEngine
{
    private readonly decimal _minimumLivingWageByn;
    private readonly decimal _belarusianGoodsPdnThreshold;

    public ScoringEngine(IConfiguration configuration)
    {
        _minimumLivingWageByn = configuration.GetValue<decimal>("Scoring:MinimumLivingWageByn", 500m);
        _belarusianGoodsPdnThreshold = configuration.GetValue<decimal>("Scoring:BelarusianGoodsPdnThreshold", 0.50m);
    }

    public ScoringResult Evaluate(ScoringRequest request)
    {
        var results = new List<ScoringRuleResultDto>();

        // Возрастное правило (21+, срок заканчивается до пенсии)
        var ageResult = EvaluateAgeRule(request);
        results.Add(ageResult);

        // Стаж работы
        var employmentResult = EvaluateEmploymentRule(request);
        results.Add(employmentResult);

        // ПДН
        var pdnResult = EvaluatePdnRule(request);
        results.Add(pdnResult);

        // Зависимые
        var dependentsResult = EvaluateDependentsRule(request);
        results.Add(dependentsResult);

        // Сумма кредита / доход
        var loanToIncomeResult = EvaluateLoanToIncomeRule(request);
        results.Add(loanToIncomeResult);

        var stopped = results.Any(r => !r.Passed && IsStopRule(r.RuleCode));
        var totalScore = Math.Clamp(results.Sum(r => r.Points), 0, 100);

        var approved = !stopped && totalScore >= 40;

        return new ScoringResult
        {
            ApplicationId = request.ApplicationId,
            TotalScore = totalScore,
            Approved = approved,
            RuleResults = results
        };
    }

    private static bool IsStopRule(string ruleCode) => ruleCode switch
    {
        "AGE" or "PDN" => true,
        _ => false
    };

    private ScoringRuleResultDto EvaluateAgeRule(ScoringRequest request)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - request.BirthDate.Year;
        if (request.BirthDate.Date > today.AddYears(-age)) age++;

        var retirementAge = request.Gender.Equals("M", StringComparison.OrdinalIgnoreCase) ? 63 : 58;
        var loanEndAge = age + (request.TermMonths / 12);

        if (age < 21 || loanEndAge > retirementAge)
            return new ScoringRuleResultDto { RuleCode = "AGE", RuleName = "Возраст", Passed = false, Points = 0, Details = "Возраст не соответствует требованиям" };

        return new ScoringRuleResultDto { RuleCode = "AGE", RuleName = "Возраст", Passed = true, Points = 20, Details = $"Возраст {age} лет" };
    }

    private ScoringRuleResultDto EvaluateEmploymentRule(ScoringRequest request)
    {
        if (request.EmploymentMonths < 3)
            return new ScoringRuleResultDto { RuleCode = "EMPLOYMENT", RuleName = "Стаж работы", Passed = false, Points = 0, Details = "Стаж менее 3 месяцев" };

        var points = request.EmploymentMonths > 24 ? 15 : 10;
        return new ScoringRuleResultDto { RuleCode = "EMPLOYMENT", RuleName = "Стаж работы", Passed = true, Points = points, Details = $"Стаж {request.EmploymentMonths} мес." };
    }

    private ScoringRuleResultDto EvaluatePdnRule(ScoringRequest request)
    {
        var monthlyPayment = CalculateAnnuityPayment(request.Amount, 0.165m, request.TermMonths);
        var totalMonthly = monthlyPayment + request.ExistingMonthlyPayments;
        var pdn = request.MonthlyIncome > 0 ? totalMonthly / request.MonthlyIncome : 1m;

        var threshold = request.CreditProductPurpose.Equals("Consumer", StringComparison.OrdinalIgnoreCase)
            ? _belarusianGoodsPdnThreshold
            : 0.40m;

        if (pdn > threshold)
            return new ScoringRuleResultDto { RuleCode = "PDN", RuleName = "ПДН", Passed = false, Points = 0, Details = $"ПДН {pdn:P0} превышает порог {threshold:P0}" };

        var points = pdn <= 0.25m ? 25 : pdn <= 0.40m ? 15 : 5;
        return new ScoringRuleResultDto { RuleCode = "PDN", RuleName = "ПДН", Passed = true, Points = points, Details = $"ПДН {pdn:P0}" };
    }

    private ScoringRuleResultDto EvaluateDependentsRule(ScoringRequest request)
    {
        var points = request.Dependents switch
        {
            <= 1 => 10,
            <= 3 => 5,
            _ => 0
        };
        var passed = request.Dependents <= 4;
        return new ScoringRuleResultDto { RuleCode = "DEPENDENTS", RuleName = "Зависимые", Passed = passed, Points = points, Details = $"Зависимых: {request.Dependents}" };
    }

    private ScoringRuleResultDto EvaluateLoanToIncomeRule(ScoringRequest request)
    {
        var ratio = request.MonthlyIncome > 0 ? request.Amount / (request.MonthlyIncome * 12) : 1m;
        var points = ratio switch
        {
            <= 3 => 20,
            <= 5 => 10,
            _ => 0
        };
        var passed = ratio <= 7;
        return new ScoringRuleResultDto { RuleCode = "LOAN_TO_INCOME", RuleName = "Соотношение кредита к доходу", Passed = passed, Points = points, Details = $"Кредит к годовому доходу: {ratio:F1}" };
    }

    private static decimal CalculateAnnuityPayment(decimal amount, decimal annualRate, int months)
    {
        if (annualRate == 0) return amount / months;
        var monthlyRate = annualRate / 12 / 100;
        var x = 1m + monthlyRate;
        var power = Math.Pow((double)x, months);
        return amount * (decimal)power * (decimal)monthlyRate / ((decimal)power - 1);
    }
}