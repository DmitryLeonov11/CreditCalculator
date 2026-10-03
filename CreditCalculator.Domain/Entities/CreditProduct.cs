using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Domain.Entities;

public class CreditProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CreditPurpose Purpose { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int MinTermMonths { get; set; }
    public int MaxTermMonths { get; set; }
    public decimal BaseRate { get; set; }
    public bool IsBelarusianMade { get; set; }
    public bool IsActive { get; set; }
}
