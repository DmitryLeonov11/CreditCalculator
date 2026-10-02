using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCalculator.Infrastructure.Persistence.Configurations;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(profile => profile.UserId);

        builder.Property(profile => profile.FullName).HasMaxLength(200);
        builder.Property(profile => profile.Gender).HasConversion<string>().HasMaxLength(16);

        builder.HasOne(profile => profile.User)
            .WithOne(user => user.Profile)
            .HasForeignKey<Profile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
