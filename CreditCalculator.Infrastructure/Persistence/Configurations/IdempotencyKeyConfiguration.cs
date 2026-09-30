using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        // Ключ уникален для всех пользователей: чужой ключ не позволяет создать или получить чужую заявку.
        builder.HasKey(idempotencyKey => idempotencyKey.Key);
        builder.Property(idempotencyKey => idempotencyKey.Key).HasMaxLength(128);
        builder.Property(idempotencyKey => idempotencyKey.RequestBodyHash).HasMaxLength(64);

        builder.HasOne(idempotencyKey => idempotencyKey.User)
            .WithMany()
            .HasForeignKey(idempotencyKey => idempotencyKey.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(idempotencyKey => idempotencyKey.Application)
            .WithMany()
            .HasForeignKey(idempotencyKey => idempotencyKey.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
