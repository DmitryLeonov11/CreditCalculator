using CreditCalculator.Calculations.Models;
using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record EmployeeApplicationListItemResponse(
    Guid Id,
    Guid CreditProductId,
    string ProductName,
    decimal Amount,
    int TermMonths,
    decimal InterestRate,
    ApplicationStatus Status,
    int? Score,
    DateTimeOffset CreatedAt);

public sealed record EmployeeApplicationDetailsResponse(
    ApplicationResponse Application,
    IReadOnlyList<EmployeeScoringResultResponse> ScoringResults,
    ProposedPaymentScheduleResponse ProposedSchedule);

public sealed record EmployeeScoringResultResponse(
    string RuleCode,
    string RuleName,
    bool Passed,
    int Points,
    string Details);

public sealed record ProposedPaymentScheduleResponse(
    DateOnly FirstPaymentDate,
    PaymentScheduleResult Schedule);
