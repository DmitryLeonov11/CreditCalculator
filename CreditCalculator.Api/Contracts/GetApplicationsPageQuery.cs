namespace CreditCalculator.Api.Contracts;

public sealed record GetApplicationsPageQuery(int Page = 1, int PageSize = 10);
