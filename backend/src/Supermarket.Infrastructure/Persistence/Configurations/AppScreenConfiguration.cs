using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AppScreenConfiguration : IEntityTypeConfiguration<AppScreen>
    {
        public void Configure(EntityTypeBuilder<AppScreen> builder)
        {
            builder.ToTable("APP_SCREENS");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.ScreenKey)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(s => s.ScreenKey).IsUnique();

            builder.Property(s => s.ScreenName)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasData(
                new AppScreen { Id = 1, ScreenKey = "Sales", ScreenName = "Sales" },
                new AppScreen { Id = 2, ScreenKey = "Products", ScreenName = "Products" },
                new AppScreen { Id = 3, ScreenKey = "Invoices", ScreenName = "Invoices" },
                new AppScreen { Id = 4, ScreenKey = "Offers", ScreenName = "Offers" },
                new AppScreen { Id = 5, ScreenKey = "Reports", ScreenName = "Reports" },
                new AppScreen { Id = 6, ScreenKey = "Inventory", ScreenName = "Inventory" },
                new AppScreen { Id = 7, ScreenKey = "Settings", ScreenName = "Settings" },
                new AppScreen { Id = 8, ScreenKey = "Purchases", ScreenName = "Purchases" },
                new AppScreen { Id = 9, ScreenKey = "BlackBox", ScreenName = "الصندوق الأسود" },
                new AppScreen { Id = 10, ScreenKey = "Backup", ScreenName = "النسخ الاحتياطي" },
                new AppScreen { Id = 11, ScreenKey = "AuditLogs", ScreenName = "سجل التدقيق" },
                new AppScreen { Id = 12, ScreenKey = "Employees", ScreenName = "الموظفون" },
                new AppScreen { Id = 13, ScreenKey = "Devices", ScreenName = "الأجهزة" }
            );
        }
    }
}
