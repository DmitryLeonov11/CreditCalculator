using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class PaymentScheduleConfiguration : IEntityTypeConfiguration<PaymentSchedule>
{
    public void Configure(EntityTypeBuilder<PaymentSchedule> builder)
    {
        builder.HasKey(schedule => schedule.Id);

        builder.HasOne(schedule => schedule.Application)
            .WithOne(application => application.PaymentSchedule)
            .HasForeignKey<PaymentSchedule>(schedule => schedule.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(schedule => schedule.Payments)
            .WithOne(payment => payment.PaymentSchedule)
            .HasForeignKey(payment => payment.PaymentScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("PaymentSchedules");
    }
}
