using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record EmployeeApplicationsQuery(
    int Page = 1,
    int PageSize = 10,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    int? MinScore = null,
    int? MaxScore = null,
    ApplicationStatus? Status = null,
    EmployeeApplicationSortField SortBy = EmployeeApplicationSortField.CreatedAt,
    bool Descending = true);

public enum EmployeeApplicationSortField
{
    Amount,
    CreatedAt,
    Score,
    Status
}
