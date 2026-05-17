using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class EmployeeScreenPermissionConfiguration : IEntityTypeConfiguration<EmployeeScreenPermission>
    {
        public void Configure(EntityTypeBuilder<EmployeeScreenPermission> builder)
        {
            builder.ToTable("EMPLOYEE_SCREEN_PERMISSIONS");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.CanAccess)
                .IsRequired();

            builder.HasIndex(p => new { p.EmployeeId, p.ScreenId }).IsUnique();

            builder.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Screen)
                .WithMany()
                .HasForeignKey(p => p.ScreenId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
