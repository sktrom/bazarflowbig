using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("INVOICES");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

            builder.Property(i => i.CustomerName)
                .HasMaxLength(200);

            builder.Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(i => i.SuspensionReason)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(i => i.InvoiceDiscountType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(i => i.InvoiceDiscountValue)
                .HasColumnType("decimal(18,4)");

            builder.Property(i => i.SubtotalUsd)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(i => i.TotalUsd)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(i => i.ExchangeRateSypSnapshot)
                .HasColumnType("decimal(18,4)");

            builder.Property(i => i.TotalSyp)
                .HasColumnType("decimal(18,4)");

            builder.Property(i => i.HasManualPriceEdit).IsRequired();
            builder.Property(i => i.HasAdjustmentRequest).IsRequired();
            
            builder.Property(i => i.CreatedAt).IsRequired();

            builder.HasOne(i => i.OriginalEmployee)
                .WithMany()
                .HasForeignKey(i => i.OriginalEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
