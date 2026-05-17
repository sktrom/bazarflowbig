using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class InvoiceLineBatchAllocationConfiguration : IEntityTypeConfiguration<InvoiceLineBatchAllocation>
    {
        public void Configure(EntityTypeBuilder<InvoiceLineBatchAllocation> builder)
        {
            builder.ToTable("INVOICE_LINE_BATCH_ALLOCATIONS");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Quantity)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(a => a.AllocationStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(a => new { a.InvoiceLineId, a.BatchId });

            builder.HasOne(a => a.InvoiceLine)
                .WithMany()
                .HasForeignKey(a => a.InvoiceLineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Batch)
                .WithMany()
                .HasForeignKey(a => a.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
