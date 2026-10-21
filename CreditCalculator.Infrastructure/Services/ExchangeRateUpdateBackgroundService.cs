using CreditCalculator.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Infrastructure.Services;

public sealed class ExchangeRateUpdateBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExchangeRateUpdateBackgroundService> _logger;
    private readonly NbrbOptions _options;

    public ExchangeRateUpdateBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeRateUpdateBackgroundService> logger,
        IOptions<NbrbOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Фоновое обновление курсов НБРБ отключено в конфигурации.");
            return;
        }

        _logger.LogInformation("Фоновый сервис обновления курсов НБРБ запущен. Интервал: {Interval}", _options.UpdateInterval);

        using var timer = new PeriodicTimer(_options.UpdateInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // IExchangeRateService зарегистрирован как scoped, а hosted service — singleton,
                // поэтому создаём свой scope на каждый тик.
                using var scope = _scopeFactory.CreateScope();
                var exchangeRateService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
                await exchangeRateService.UpdateRatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фонового обновления курсов НБРБ");
            }
        }
    }
}