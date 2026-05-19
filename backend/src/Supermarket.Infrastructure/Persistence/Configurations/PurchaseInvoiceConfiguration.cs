using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
        {
            builder.ToTable("PURCHASE_INVOICES");

            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(pi => pi.InvoiceNumber).IsUnique();

            builder.Property(pi => pi.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PurchaseInvoiceStatus.Draft)
                .IsRequired();

            builder.Property(pi => pi.ExternalInvoiceNumber)
                .HasMaxLength(100);

            builder.Property(pi => pi.Notes)
                .HasMaxLength(1000);

            builder.Property(pi => pi.SubtotalUsd)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(pi => pi.TotalUsd)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(pi => pi.CreatedAt).IsRequired();
            builder.Property(pi => pi.UpdatedAt).IsRequired();

            builder.HasIndex(pi => pi.SupplierId);
            builder.HasIndex(pi => pi.CreatedByEmployeeId);
            builder.HasIndex(pi => pi.CompletedByEmployeeId);
            builder.HasIndex(pi => new { pi.Status, pi.CreatedAt });

            builder.HasOne(pi => pi.Supplier)
                .WithMany()
                .HasForeignKey(pi => pi.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pi => pi.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(pi => pi.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pi => pi.CompletedByEmployee)
                .WithMany()
                .HasForeignKey(pi => pi.CompletedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(pi => pi.Lines)
                .WithOne(line => line.PurchaseInvoice)
                .HasForeignKey(line => line.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
