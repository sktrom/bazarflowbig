using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            builder.ToTable("APP_SETTINGS");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.SettingKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(a => a.SettingKey).IsUnique();

            builder.Property(a => a.SettingValue)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.UpdatedAt)
                .IsRequired();

            builder.HasOne(a => a.UpdatedByEmployee)
                .WithMany()
                .HasForeignKey(a => a.UpdatedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            var fixedSeedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            builder.HasData(
                new AppSetting { Id = 1, SettingKey = "exchange_rate_syp", SettingValue = "15000", UpdatedAt = fixedSeedDate },
                new AppSetting { Id = 2, SettingKey = "stock_alert_threshold", SettingValue = "10", UpdatedAt = fixedSeedDate },
                new AppSetting { Id = 3, SettingKey = "expiry_alert_days", SettingValue = "30", UpdatedAt = fixedSeedDate },
                new AppSetting { Id = 4, SettingKey = "setup_completed", SettingValue = "false", UpdatedAt = fixedSeedDate },
                new AppSetting { Id = 5, SettingKey = "store_name", SettingValue = "BazarFlow", UpdatedAt = fixedSeedDate }
            );
        }
    }
}
