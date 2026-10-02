namespace CreditCalculator.Domain.Entities;

public class ScoringResult
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public int Points { get; set; }
    public string Details { get; set; } = string.Empty;

    public Application Application { get; set; } = null!;
}