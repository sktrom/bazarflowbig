using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AdjustmentRequestLineConfiguration : IEntityTypeConfiguration<AdjustmentRequestLine>
    {
        public void Configure(EntityTypeBuilder<AdjustmentRequestLine> builder)
        {
            builder.ToTable("ADJUSTMENT_REQUEST_LINES");

            builder.HasKey(arl => arl.Id);

            builder.Property(arl => arl.ActionType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(arl => arl.RequestedQuantity)
                .HasColumnType("decimal(18,4)");

            builder.Property(arl => arl.RequestedLineTotalUsd)
                .HasColumnType("decimal(18,4)");

            builder.HasIndex(arl => arl.AdjustmentRequestId);
            builder.HasIndex(arl => arl.InvoiceLineId);

            builder.HasOne(arl => arl.AdjustmentRequest)
                .WithMany()
                .HasForeignKey(arl => arl.AdjustmentRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(arl => arl.InvoiceLine)
                .WithMany()
                .HasForeignKey(arl => arl.InvoiceLineId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
