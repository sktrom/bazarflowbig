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
                new AppScreen { Id = 1, ScreenKey = "sales", ScreenName = "Sales" },
                new AppScreen { Id = 2, ScreenKey = "products", ScreenName = "Products" },
                new AppScreen { Id = 3, ScreenKey = "invoices", ScreenName = "Invoices" },
                new AppScreen { Id = 4, ScreenKey = "offers", ScreenName = "Offers" },
                new AppScreen { Id = 5, ScreenKey = "reports", ScreenName = "Reports" },
                new AppScreen { Id = 6, ScreenKey = "inventory", ScreenName = "Inventory" },
                new AppScreen { Id = 7, ScreenKey = "settings", ScreenName = "Settings" }
            );
        }
    }
}
