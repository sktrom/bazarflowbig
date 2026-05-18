using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class PosDeviceConfiguration : IEntityTypeConfiguration<PosDevice>
    {
        public void Configure(EntityTypeBuilder<PosDevice> builder)
        {
            builder.ToTable("POS_DEVICES");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DeviceCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(d => d.DeviceCode).IsUnique();

            builder.Property(d => d.DeviceName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.HasData(
                new PosDevice
                {
                    Id = 1,
                    DeviceCode = "DEFAULT_DEVICE",
                    DeviceName = "Default POS Device",
                    IsActive = true
                }
            );
        }
    }
}
