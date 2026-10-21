namespace CreditCalculator.Scoring.Messages.Messages;

public sealed record ScoringRequest
{
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public int TermMonths { get; init; }
    public decimal MonthlyIncome { get; init; }
    public decimal ExistingMonthlyPayments { get; init; }
    public int EmploymentMonths { get; init; }
    public int Dependents { get; init; }
    public string Gender { get; init; } = string.Empty;
    public DateTime BirthDate { get; init; }
    public string CreditProductPurpose { get; init; } = string.Empty;
}

public sealed record ScoringResult
{
    public Guid ApplicationId { get; init; }
    public int TotalScore { get; init; }
    public bool Approved { get; init; }
    public IReadOnlyList<ScoringRuleResultDto> RuleResults { get; init; } = Array.Empty<ScoringRuleResultDto>();
}

public sealed record ScoringRuleResultDto
{
    public string RuleCode { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public int Points { get; init; }
    public string? Details { get; init; }
}