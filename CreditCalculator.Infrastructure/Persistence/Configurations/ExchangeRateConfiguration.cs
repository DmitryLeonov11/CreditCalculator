using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(rate => rate.Id);

        builder.Property(rate => rate.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(rate => rate.RateToByn).HasPrecision(18, 6).IsRequired();
        builder.Property(rate => rate.Source).HasMaxLength(32).IsRequired();

        builder.HasIndex(rate => new { rate.CurrencyCode, rate.UpdatedAt });
    }
}