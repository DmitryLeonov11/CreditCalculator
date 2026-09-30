namespace CreditCalculator.Application.Contracts;

public sealed record CreateApplicationResult(ApplicationResponse Application, bool WasReplayed);
