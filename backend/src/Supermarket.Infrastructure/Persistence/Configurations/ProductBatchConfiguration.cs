using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
    {
        public void Configure(EntityTypeBuilder<ProductBatch> builder)
        {
            builder.ToTable("PRODUCT_BATCHES");

            builder.HasKey(pb => pb.Id);

            builder.Property(pb => pb.QuantityReceived)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(pb => pb.QuantityAvailable)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(pb => pb.EntryInvoiceNumber)
                .HasMaxLength(100);

            builder.HasIndex(pb => new { pb.ProductId, pb.ExpiryDate });

            builder.HasOne(pb => pb.Product)
                .WithMany()
                .HasForeignKey(pb => pb.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pb => pb.EnteredByEmployee)
                .WithMany()
                .HasForeignKey(pb => pb.EnteredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
