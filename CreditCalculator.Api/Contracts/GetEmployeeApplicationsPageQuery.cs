using CreditCalculator.Application.Contracts;
using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Api.Contracts;

public sealed record GetEmployeeApplicationsPageQuery(
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
    bool Descending = true)
{
    public EmployeeApplicationsQuery ToApplicationQuery() => new(
        Page,
        PageSize,
        MinAmount,
        MaxAmount,
        CreatedFrom,
        CreatedTo,
        MinScore,
        MaxScore,
        Status,
        SortBy,
        Descending);
}
