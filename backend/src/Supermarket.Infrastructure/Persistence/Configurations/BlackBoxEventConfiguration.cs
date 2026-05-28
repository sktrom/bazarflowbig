using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class BlackBoxEventConfiguration : IEntityTypeConfiguration<BlackBoxEvent>
    {
        public void Configure(EntityTypeBuilder<BlackBoxEvent> builder)
        {
            builder.ToTable("BLACK_BOX_EVENTS");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.DeviceCode).HasMaxLength(100);
            builder.Property(e => e.Route).HasMaxLength(300);
            builder.Property(e => e.PageName).HasMaxLength(150);
            builder.Property(e => e.ActionType).IsRequired().HasMaxLength(80);
            builder.Property(e => e.ElementKey).HasMaxLength(150);
            builder.Property(e => e.EntityType).HasMaxLength(100);
            builder.Property(e => e.EntityId).HasMaxLength(100);
            builder.Property(e => e.Result).IsRequired().HasMaxLength(50);
            builder.Property(e => e.Message).HasMaxLength(500);
            builder.Property(e => e.MetadataTruncated).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.IpAddress).HasMaxLength(64);
            builder.Property(e => e.UserAgent).HasMaxLength(500);
            builder.Property(e => e.CreatedAtUtc).IsRequired();

            builder.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.CreatedAtUtc);
            builder.HasIndex(e => new { e.EmployeeId, e.CreatedAtUtc });
            builder.HasIndex(e => new { e.SessionId, e.CreatedAtUtc });
            builder.HasIndex(e => new { e.DeviceCode, e.CreatedAtUtc });
            builder.HasIndex(e => new { e.ActionType, e.CreatedAtUtc });
            builder.HasIndex(e => new { e.PageName, e.CreatedAtUtc });
            builder.HasIndex(e => new { e.EntityType, e.EntityId });
        }
    }
}
