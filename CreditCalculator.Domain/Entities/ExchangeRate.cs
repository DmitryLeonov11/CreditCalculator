using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Domain.Entities;

public class ExchangeRate
{
    public Guid Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty; // ISO 4217: USD, EUR, RUB, BYN
    public decimal RateToByn { get; set; } // 1 unit of currency in BYN
    public DateTime UpdatedAt { get; set; }
    public string Source { get; set; } = string.Empty; // "NBRB"
}