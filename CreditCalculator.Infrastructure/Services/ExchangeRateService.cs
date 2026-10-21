using CreditCalculator.Application.Abstractions;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditCalculator.Infrastructure.Services;

public sealed class ExchangeRateService : IExchangeRateService
{
    private readonly AppDbContext _dbContext;
    private readonly NbrbClient _nbrbClient;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        AppDbContext dbContext,
        NbrbClient nbrbClient,
        ILogger<ExchangeRateService> logger)
    {
        _dbContext = dbContext;
        _nbrbClient = nbrbClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetLatestRatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExchangeRates
            .AsNoTracking()
            .OrderBy(rate => rate.CurrencyCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal?> GetRefinancingRateAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CreditProducts
            .AsNoTracking()
            .Where(p => p.RefinancingRateByn.HasValue)
            .Select(p => p.RefinancingRateByn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Получаем ставку рефинансирования
            var refinancingRate = await _nbrbClient.GetRefinancingRateAsync(cancellationToken);
            if (refinancingRate.HasValue)
            {
                _logger.LogInformation("Получена ставка рефинансирования НБРБ: {Rate}", refinancingRate.Value);

                // Обновляем RefinancingRateByn во всех активных продуктах
                var products = await _dbContext.CreditProducts
                    .Where(p => p.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var product in products)
                {
                    product.RefinancingRateByn = refinancingRate.Value;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Получаем курсы валют
            var rates = await _nbrbClient.GetExchangeRatesAsync(cancellationToken);
            _logger.LogInformation("Получено {Count} курсов валют от НБРБ", rates.Count);

            var now = DateTime.UtcNow;
            var existingCodes = await _dbContext.ExchangeRates
                .AsNoTracking()
                .Select(rate => rate.CurrencyCode)
                .ToListAsync(cancellationToken);

            var toAdd = new List<ExchangeRate>();
            var toUpdate = new List<ExchangeRate>();

            foreach (var rate in rates)
            {
                var entity = new ExchangeRate
                {
                    Id = Guid.CreateVersion7(),
                    CurrencyCode = rate.CurrencyCode,
                    RateToByn = rate.RateToByn,
                    UpdatedAt = rate.UpdatedAt,
                    Source = rate.Source
                };

                if (existingCodes.Contains(rate.CurrencyCode))
                {
                    var existing = await _dbContext.ExchangeRates
                        .Where(r => r.CurrencyCode == rate.CurrencyCode)
                        .OrderByDescending(r => r.UpdatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existing != null)
                    {
                        existing.RateToByn = rate.RateToByn;
                        existing.UpdatedAt = rate.UpdatedAt;
                        existing.Source = rate.Source;
                        toUpdate.Add(existing);
                    }
                }
                else
                {
                    toAdd.Add(entity);
                }
            }

            if (toAdd.Count > 0)
                await _dbContext.ExchangeRates.AddRangeAsync(toAdd, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Курсы валют обновлены: добавлено {Added}, обновлено {Updated}", toAdd.Count, toUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении курсов валют от НБРБ");
            throw;
        }
    }
}