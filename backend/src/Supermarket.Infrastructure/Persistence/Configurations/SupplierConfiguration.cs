using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("SUPPLIERS");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(s => s.Phone)
                .HasMaxLength(50);

            builder.Property(s => s.Email)
                .HasMaxLength(150);

            builder.Property(s => s.Address)
                .HasMaxLength(500);

            builder.Property(s => s.Notes)
                .HasMaxLength(1000);

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasIndex(s => s.IsActive);
            builder.HasIndex(s => new { s.Name, s.IsActive });
        }
    }
}
