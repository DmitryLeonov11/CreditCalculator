namespace CreditCalculator.Application.Contracts;

public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);
