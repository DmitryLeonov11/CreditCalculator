using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class PaymentScheduleItemConfiguration : IEntityTypeConfiguration<PaymentScheduleItem>
{
    public void Configure(EntityTypeBuilder<PaymentScheduleItem> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.HasIndex(payment => new { payment.PaymentScheduleId, payment.Number })
            .IsUnique();

        builder.ToTable("PaymentScheduleItems", table =>
        {
            table.HasCheckConstraint("CK_PaymentScheduleItems_Number", "\"Number\" > 0");
            table.HasCheckConstraint("CK_PaymentScheduleItems_Amounts", "\"Payment\" >= 0 AND \"InterestPart\" >= 0 AND \"PrincipalPart\" >= 0 AND \"RemainingBalance\" >= 0");
        });
    }
}
