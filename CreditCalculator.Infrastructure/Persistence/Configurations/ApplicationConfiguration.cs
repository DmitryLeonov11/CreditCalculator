using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class ApplicationConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.HasKey(application => application.Id);

        builder.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(application => application.InterestRate)
            .HasPrecision(9, 4);

        builder.Property(application => application.RowVersion)
            .IsRowVersion();

        builder.HasOne(application => application.User)
            .WithMany()
            .HasForeignKey(application => application.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.CreditProduct)
            .WithMany()
            .HasForeignKey(application => application.CreditProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(application => new { application.UserId, application.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(application => new { application.Status, application.CreatedAt });

        builder.ToTable("Applications", table =>
        {
            table.HasCheckConstraint("CK_Applications_Amount", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_Applications_TermMonths", "\"TermMonths\" BETWEEN 1 AND 360");
            table.HasCheckConstraint("CK_Applications_Rate", "\"InterestRate\" >= 0");
        });
    }
}
