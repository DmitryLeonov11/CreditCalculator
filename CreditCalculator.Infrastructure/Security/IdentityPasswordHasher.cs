using CreditCalculator.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = CreditCalculator.Application.Abstractions.IPasswordHasher;

namespace CreditCalculator.Infrastructure.Security;

// PBKDF2 с солью и итерациями из ASP.NET Core Identity — без собственной криптографии.
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string Hash(User user, string password) => _passwordHasher.HashPassword(user, password);

    public bool Verify(User user, string password) =>
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
