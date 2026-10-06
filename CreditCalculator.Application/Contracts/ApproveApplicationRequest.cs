namespace CreditCalculator.Application.Contracts;

public sealed record ApproveApplicationRequest(
    decimal Amount,
    int TermMonths,
    decimal InterestRate,
    string? Comment = null);
