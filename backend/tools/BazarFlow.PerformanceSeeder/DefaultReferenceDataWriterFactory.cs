using Microsoft.EntityFrameworkCore;
using Supermarket.Infrastructure.Persistence;

namespace BazarFlow.PerformanceSeeder;

public static class DefaultReferenceDataWriterFactory
{
    public static IReferenceDataWriter Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SupermarketDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new EfReferenceDataWriter(new SupermarketDbContext(options));
    }
}
