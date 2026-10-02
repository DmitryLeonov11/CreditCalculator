using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<CreditProduct> CreditProducts => Set<CreditProduct>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<ScoringResult> ScoringResults => Set<ScoringResult>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Денежные поля по умолчанию — decimal(18,2); без явной точности провайдер может молча округлить.
        // Ставки переопределяют это на decimal(9,4) в конфигурации конкретной сущности.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
