using CreditCalculator.Application.Contracts;

namespace CreditCalculator.Application.Services;

public interface ICreditProductService
{
    Task<IReadOnlyList<CreditProductResponse>> GetActiveAsync(CancellationToken cancellationToken = default);
}
