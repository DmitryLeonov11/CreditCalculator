using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Services;

public sealed class CreditProductService : ICreditProductService
{
    private readonly IAppDbContext _dbContext;

    public CreditProductService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CreditProductResponse>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.CreditProducts
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Purpose)
            .ThenBy(product => product.Name)
            .Select(product => new CreditProductResponse(
                product.Id,
                product.Name,
                product.Purpose,
                product.MinAmount,
                product.MaxAmount,
                product.MinTermMonths,
                product.MaxTermMonths,
                product.BaseRate))
            .ToListAsync(cancellationToken);
}
