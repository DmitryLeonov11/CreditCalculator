using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class ScoringResultConfiguration : IEntityTypeConfiguration<ScoringResult>
{
    public void Configure(EntityTypeBuilder<ScoringResult> builder)
    {
        builder.HasKey(result => result.Id);

        builder.Property(result => result.RuleCode).HasMaxLength(64);
        builder.Property(result => result.RuleName).HasMaxLength(200);
        builder.Property(result => result.Details).HasMaxLength(2000);

        builder.HasOne(result => result.Application)
            .WithMany(application => application.ScoringResults)
            .HasForeignKey(result => result.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(result => new { result.ApplicationId, result.RuleCode }).IsUnique();
    }
}