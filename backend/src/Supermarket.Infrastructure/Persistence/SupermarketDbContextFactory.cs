using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace Supermarket.Infrastructure.Persistence
{
    public class SupermarketDbContextFactory : IDesignTimeDbContextFactory<SupermarketDbContext>
    {
        private const string ApiUserSecretsId = "bazarflow-supermarket-api";
        private const string MissingConnectionStringMessage =
            "DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection or user-secrets.";

        public SupermarketDbContext CreateDbContext(string[] args)
        {
            var basePath = ResolveApiProjectPath();
            var builder = new ConfigurationBuilder();

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                builder.SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true);
            }

            builder.AddUserSecrets(ApiUserSecretsId, reloadOnChange: false)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(MissingConnectionStringMessage);

            var optionsBuilder = new DbContextOptionsBuilder<SupermarketDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SupermarketDbContext(optionsBuilder.Options);
        }

        private static string? ResolveApiProjectPath()
        {
            var current = Directory.GetCurrentDirectory();
            var candidates = new[]
            {
                current,
                Path.Combine(current, "src", "Supermarket.Api"),
                Path.Combine(current, "..", "Supermarket.Api"),
                Path.Combine(current, "..", "src", "Supermarket.Api")
            };

            return candidates
                .Select(Path.GetFullPath)
                .FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")));
        }
    }
}
