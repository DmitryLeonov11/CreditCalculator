using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(document => document.Id);

        builder.Property(document => document.FileName).HasMaxLength(500).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(document => document.Description).HasMaxLength(1000);

        builder.HasIndex(document => document.ApplicationId);
    }
}