using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Profile> Profiles { get; }
    DbSet<CreditProduct> CreditProducts { get; }
    DbSet<CreditCalculator.Domain.Entities.Application> Applications { get; }
    DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
