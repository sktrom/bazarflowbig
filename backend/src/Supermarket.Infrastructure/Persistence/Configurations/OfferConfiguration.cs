using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class OfferConfiguration : IEntityTypeConfiguration<Offer>
    {
        public void Configure(EntityTypeBuilder<Offer> builder)
        {
            builder.ToTable("OFFERS");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.DiscountType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(o => o.DiscountValue)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.UpdatedAt)
                .IsRequired();

            builder.HasIndex(o => new { o.ProductId, o.IsActive });

            builder.HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
