using CreditCalculator.Domain.Entities;

namespace CreditCalculator.Application.Contracts;

public sealed record ExchangeRateResponse(string CurrencyCode, decimal RateToByn, DateTime UpdatedAt, string Source);

public sealed record RefinancingRateResponse(decimal Rate, DateTime UpdatedAt, string Source);