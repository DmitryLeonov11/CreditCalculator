namespace CreditCalculator.Application.Contracts;

public sealed record EmployeeStatisticsResponse(
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    int TotalApplications,
    int ApprovedApplications,
    decimal? ApprovalRate,
    decimal? AverageAmount,
    decimal? AverageTermMonths);
