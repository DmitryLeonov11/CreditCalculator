using CreditCalculator.Domain.Entities;

namespace CreditCalculator.Application.Abstractions;

public interface IExchangeRateService
{
    Task<IReadOnlyList<ExchangeRate>> GetLatestRatesAsync(CancellationToken cancellationToken = default);
    Task<decimal?> GetRefinancingRateAsync(CancellationToken cancellationToken = default);
    Task UpdateRatesAsync(CancellationToken cancellationToken = default);
}