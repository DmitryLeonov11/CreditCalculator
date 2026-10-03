using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class CreditProductConfiguration : IEntityTypeConfiguration<CreditProduct>
{
    public void Configure(EntityTypeBuilder<CreditProduct> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Purpose).HasConversion<string>().HasMaxLength(32);

        builder.Property(product => product.BaseRate).HasPrecision(9, 4);
        builder.Property(product => product.IsBelarusianMade).HasDefaultValue(false);
    }
}
