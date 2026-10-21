using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/exchange-rates")]
public sealed class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRatesController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExchangeRateResponse>>> GetLatest(CancellationToken cancellationToken)
    {
        var rates = await _exchangeRateService.GetLatestRatesAsync(cancellationToken);
        return Ok(rates.Select(r => new ExchangeRateResponse(r.CurrencyCode, r.RateToByn, r.UpdatedAt, r.Source)));
    }

    [HttpGet("refinancing")]
    public async Task<ActionResult<RefinancingRateResponse>> GetRefinancing(CancellationToken cancellationToken)
    {
        var rate = await _exchangeRateService.GetRefinancingRateAsync(cancellationToken);
        if (!rate.HasValue)
            return NotFound("Ставка рефинансирования не найдена.");

        return Ok(new RefinancingRateResponse(rate.Value, DateTime.UtcNow, "NBRB"));
    }

    [HttpPost("update")]
    public async Task<ActionResult> Update(CancellationToken cancellationToken)
    {
        await _exchangeRateService.UpdateRatesAsync(cancellationToken);
        return Ok("Курсы валют и ставка рефинансирования обновлены.");
    }
}