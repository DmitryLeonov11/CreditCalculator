namespace CreditCalculator.Api.Contracts;

public sealed record GetEmployeeStatisticsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);
