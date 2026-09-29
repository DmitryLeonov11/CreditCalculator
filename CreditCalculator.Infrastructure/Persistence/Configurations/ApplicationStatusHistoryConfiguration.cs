using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(history => history.Comment)
            .HasMaxLength(1000);

        builder.HasOne(history => history.Application)
            .WithMany(application => application.StatusHistory)
            .HasForeignKey(history => history.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(history => new { history.ApplicationId, history.ChangedAt });
    }
}
