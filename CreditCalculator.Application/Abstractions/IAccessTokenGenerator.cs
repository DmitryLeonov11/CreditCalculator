using CreditCalculator.Domain.Entities;

namespace CreditCalculator.Application.Abstractions;

public interface IAccessTokenGenerator
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
