using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("INVOICE_LINES");

            builder.HasKey(il => il.Id);

            builder.Property(il => il.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(il => il.UnitPriceUsdOriginal)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(il => il.LineTotalUsdOriginal)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(il => il.LineTotalUsdEffective)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(il => il.IsPriceOverridden).IsRequired();
            builder.Property(il => il.SortOrder).IsRequired();

            builder.HasIndex(il => il.InvoiceId);

            builder.HasOne(il => il.Invoice)
                .WithMany()
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(il => il.Product)
                .WithMany()
                .HasForeignKey(il => il.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(il => il.Offer)
                .WithMany()
                .HasForeignKey(il => il.OfferId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
