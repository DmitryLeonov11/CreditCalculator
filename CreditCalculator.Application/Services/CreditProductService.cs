using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Services;

public sealed class CreditProductService : ICreditProductService
{
    private const string CacheKey = "credit:products:active";

    private readonly IAppDbContext _dbContext;
    private readonly ICacheService _cache;

    public CreditProductService(IAppDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CreditProductResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<CreditProductResponse>>(CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var products = await _dbContext.CreditProducts
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

        await _cache.SetAsync(CacheKey, products, TimeSpan.FromMinutes(30), cancellationToken);
        return products;
    }
}
