using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AppLoginAttemptConfiguration : IEntityTypeConfiguration<AppLoginAttempt>
    {
        public void Configure(EntityTypeBuilder<AppLoginAttempt> builder)
        {
            builder.ToTable("LOGIN_ATTEMPTS");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).UseIdentityColumn();

            builder.Property(x => x.UsernameNormalized).IsRequired().HasMaxLength(150);
            builder.Property(x => x.IpAddress).IsRequired().HasMaxLength(64);
            builder.Property(x => x.UserAgent).HasMaxLength(500);
            builder.Property(x => x.Result).IsRequired().HasMaxLength(50);
            builder.Property(x => x.FailureReason).HasMaxLength(100);
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasIndex(x => new { x.UsernameNormalized, x.IpAddress, x.CreatedAtUtc });
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
