using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Supermarket.Domain.Entities;

namespace Supermarket.Infrastructure.Persistence.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("EMPLOYEES");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(e => e.Username).IsUnique();

            builder.Property(e => e.Phone)
                .HasMaxLength(30);

            builder.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsRequired();
        }
    }
}
