using CreditCalculator.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Infrastructure.Services;

public sealed class ExchangeRateUpdateBackgroundService : BackgroundService
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateUpdateBackgroundService> _logger;
    private readonly NbrbOptions _options;

    public ExchangeRateUpdateBackgroundService(
        IExchangeRateService exchangeRateService,
        ILogger<ExchangeRateUpdateBackgroundService> logger,
        IOptions<NbrbOptions> options)
    {
        _exchangeRateService = exchangeRateService;
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
                await _exchangeRateService.UpdateRatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фонового обновления курсов НБРБ");
            }
        }
    }
}