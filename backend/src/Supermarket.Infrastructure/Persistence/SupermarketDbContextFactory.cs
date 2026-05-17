using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Supermarket.Infrastructure.Persistence
{
    public class SupermarketDbContextFactory : IDesignTimeDbContextFactory<SupermarketDbContext>
    {
        public SupermarketDbContext CreateDbContext(string[] args)
        {
            var fallbackConnection = "Server=DESKTOP-G6MOPDS\\SQLEXPRESS;Database=SupermarketDb;User Id=sa;Password=123456;TrustServerCertificate=True;Encrypt=False;";
            var connectionString = fallbackConnection;

            try
            {
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Supermarket.Api");
                if (Directory.Exists(basePath))
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddJsonFile("appsettings.Development.json", optional: true);

                    var configuration = builder.Build();
                    var parsedConn = configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrWhiteSpace(parsedConn))
                    {
                        connectionString = parsedConn;
                    }
                }
            }
            catch
            {
                // Ignore any configuration parsing errors during design time and rely on the fallback
            }

            var optionsBuilder = new DbContextOptionsBuilder<SupermarketDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SupermarketDbContext(optionsBuilder.Options);
        }
    }
}
