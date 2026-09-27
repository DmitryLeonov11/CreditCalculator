using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record CreditProductResponse(
    Guid Id,
    string Name,
    CreditPurpose Purpose,
    decimal MinAmount,
    decimal MaxAmount,
    int MinTermMonths,
    int MaxTermMonths,
    decimal BaseRate);
