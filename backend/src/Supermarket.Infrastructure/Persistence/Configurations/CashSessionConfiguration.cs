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

            builder.HasIndex(s => new { s.EmployeeId, s.Status });

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
