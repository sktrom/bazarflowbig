using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
        {
            builder.ToTable("PURCHASE_INVOICE_LINES");

            builder.HasKey(line => line.Id);

            builder.Property(line => line.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(line => line.UnitCostUsd)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(line => line.LineTotalUsd)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(line => line.Notes)
                .HasMaxLength(500);

            builder.Property(line => line.SortOrder).IsRequired();

            builder.HasIndex(line => line.PurchaseInvoiceId);
            builder.HasIndex(line => line.ProductId);
            builder.HasIndex(line => new { line.PurchaseInvoiceId, line.ProductId });

            builder.HasOne(line => line.PurchaseInvoice)
                .WithMany(invoice => invoice.Lines)
                .HasForeignKey(line => line.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(line => line.Product)
                .WithMany()
                .HasForeignKey(line => line.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
