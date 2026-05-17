using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class AdjustmentRequestConfiguration : IEntityTypeConfiguration<AdjustmentRequest>
    {
        public void Configure(EntityTypeBuilder<AdjustmentRequest> builder)
        {
            builder.ToTable("ADJUSTMENT_REQUESTS");

            builder.HasKey(ar => ar.Id);

            builder.Property(ar => ar.RequestType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(ar => ar.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(ar => ar.Reason)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(ar => ar.CreatedAt).IsRequired();

            builder.HasIndex(ar => new { ar.InvoiceId, ar.Status });

            builder.HasOne(ar => ar.Invoice)
                .WithMany()
                .HasForeignKey(ar => ar.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.RequestedByEmployee)
                .WithMany()
                .HasForeignKey(ar => ar.RequestedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ar => ar.ReviewedByEmployee)
                .WithMany()
                .HasForeignKey(ar => ar.ReviewedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
