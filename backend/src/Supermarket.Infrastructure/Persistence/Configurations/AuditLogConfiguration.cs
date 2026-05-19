using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AUDIT_LOGS");

            builder.HasKey(log => log.Id);

            builder.Property(log => log.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(log => log.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(log => log.EntityId)
                .HasMaxLength(100);

            builder.Property(log => log.EntityDisplayName)
                .HasMaxLength(250);

            builder.Property(log => log.IpAddress)
                .HasMaxLength(64);

            builder.Property(log => log.UserAgent)
                .HasMaxLength(500);

            builder.Property(log => log.CreatedAt)
                .IsRequired();

            builder.HasOne(log => log.Employee)
                .WithMany()
                .HasForeignKey(log => log.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(log => log.CreatedAt);
            builder.HasIndex(log => new { log.EntityType, log.EntityId });
            builder.HasIndex(log => new { log.EmployeeId, log.CreatedAt });
            builder.HasIndex(log => new { log.Action, log.CreatedAt });
        }
    }
}
