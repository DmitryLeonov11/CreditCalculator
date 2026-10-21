using CreditCalculator.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CreditCalculator.Infrastructure.Services;

public sealed class NbrbClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly NbrbOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NbrbClient(HttpClient httpClient, IOptions<NbrbOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<decimal?> GetRefinancingRateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/refinancingrate", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // НБРБ возвращает объект с полями: Date, Rate, etc.
        var rate = root.GetProperty("Rate").GetDecimal();
        var date = root.GetProperty("Date").GetDateTime();
        return rate;
    }

    public async Task<IReadOnlyList<NbrbRate>> GetExchangeRatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/exrates/rates", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var rates = new List<NbrbRate>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var curId = element.GetProperty("Cur_ID").GetInt32();
            var curAbbrev = element.GetProperty("Cur_Abbreviation").GetString() ?? string.Empty;
            var curScale = element.GetProperty("Cur_Scale").GetInt32();
            var curOfficialRate = element.GetProperty("Cur_OfficialRate").GetDecimal();
            var date = element.GetProperty("Date").GetDateTime();

            // Курс за Cur_Scale единиц валюты, приводим к 1 единице
            var ratePerUnit = curOfficialRate / curScale;

            rates.Add(new NbrbRate(curAbbrev, ratePerUnit, date, "NBRB"));
        }

        return rates;
    }

    public void Dispose() => _httpClient.Dispose();
}

public sealed record NbrbRate(string CurrencyCode, decimal RateToByn, DateTime UpdatedAt, string Source);