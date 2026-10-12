using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record ApplicationResponse(
    Guid Id,
    Guid CreditProductId,
    string ProductName,
    decimal Amount,
    int TermMonths,
    decimal InterestRate,
    ApplicationStatus Status,
    int? Score,
    decimal? IncomeAtApply,
    decimal? ExistingPaymentsAtApply,
    int? AgeAtApply,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ApplicationStatusHistoryResponse> StatusHistory,
    IReadOnlyList<ApplicationScoringResultResponse> ScoringResults);

public sealed record ApplicationStatusHistoryResponse(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    DateTimeOffset ChangedAt,
    string? Comment);

public sealed record ApplicationScoringResultResponse(
    string RuleCode,
    string RuleName,
    bool Passed,
    int Points,
    string Details);
