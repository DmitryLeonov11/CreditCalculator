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
    IReadOnlyList<ApplicationStatusHistoryResponse> StatusHistory);

public sealed record ApplicationStatusHistoryResponse(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    DateTimeOffset ChangedAt,
    string? Comment);
