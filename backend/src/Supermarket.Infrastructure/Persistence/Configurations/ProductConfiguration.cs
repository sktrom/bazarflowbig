using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("PRODUCTS");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Barcode)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(p => p.Barcode).IsUnique();

            builder.Property(p => p.BaseUnit)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.PriceUsd)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(p => p.CartonPriceUsd)
                .HasColumnType("decimal(18,4)");

            builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.HasCarton).IsRequired();
            builder.Property(p => p.HasExpiry).IsRequired();
            builder.Property(p => p.IsActive).HasDefaultValue(true);

            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.UpdatedAt).IsRequired();
        }
    }
}
