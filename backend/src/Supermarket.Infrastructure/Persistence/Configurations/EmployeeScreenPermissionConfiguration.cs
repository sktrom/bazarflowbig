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

            builder.HasData(
                new EmployeeScreenPermission { Id = 1, EmployeeId = 1, ScreenId = 1, CanAccess = true },
                new EmployeeScreenPermission { Id = 2, EmployeeId = 1, ScreenId = 2, CanAccess = true },
                new EmployeeScreenPermission { Id = 3, EmployeeId = 1, ScreenId = 3, CanAccess = true },
                new EmployeeScreenPermission { Id = 4, EmployeeId = 1, ScreenId = 4, CanAccess = true },
                new EmployeeScreenPermission { Id = 5, EmployeeId = 1, ScreenId = 5, CanAccess = true },
                new EmployeeScreenPermission { Id = 6, EmployeeId = 1, ScreenId = 6, CanAccess = true },
                new EmployeeScreenPermission { Id = 7, EmployeeId = 1, ScreenId = 7, CanAccess = true }
            );
        }
    }
}
