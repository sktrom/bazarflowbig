using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
    {
        public void Configure(EntityTypeBuilder<CashSession> builder)
        {
            builder.ToTable("CASH_SESSIONS");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(s => s.StartedAt)
                .IsRequired();

            builder.Property(s => s.SessionToken)
                .HasMaxLength(128);

            builder.HasIndex(s => new { s.EmployeeId, s.Status });
            builder.HasIndex(s => s.SessionToken)
                .IsUnique()
                .HasFilter("[SessionToken] IS NOT NULL");
            builder.HasIndex(s => s.ExpiresAt);
            builder.HasIndex(s => new { s.Status, s.ExpiresAt });

            builder.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Device)
                .WithMany()
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
