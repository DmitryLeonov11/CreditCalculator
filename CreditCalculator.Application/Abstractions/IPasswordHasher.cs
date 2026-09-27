using CreditCalculator.Domain.Entities;

namespace CreditCalculator.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}
