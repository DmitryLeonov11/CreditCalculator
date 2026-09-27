namespace CreditCalculator.Application.Contracts;

public sealed record RegisterRequest(string Email, string Password, bool PersonalDataConsent);
