using BazarFlow.DbMigrator;
using Xunit;

namespace Supermarket.UnitTests.DbMigrator
{
    public class DbMigratorTests
    {
        [Fact]
        public async Task RunAsync_MissingConnectionString_ReturnsExitCodeOne()
        {
            var originalDefaultConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            var originalBazarFlowConnection = Environment.GetEnvironmentVariable("BAZARFLOW_CONNECTION_STRING");
            var output = new StringWriter();
            var error = new StringWriter();

            try
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
                Environment.SetEnvironmentVariable("BAZARFLOW_CONNECTION_STRING", null);

                var exitCode = await DbMigratorApp.RunAsync([], output, error);

                Assert.Equal(MigrationExitCodes.MissingConnectionString, exitCode);
                Assert.Contains("DefaultConnection is not configured", error.ToString());
            }
            finally
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", originalDefaultConnection);
                Environment.SetEnvironmentVariable("BAZARFLOW_CONNECTION_STRING", originalBazarFlowConnection);
            }
        }

        [Fact]
        public void Resolver_CommandLineConnectionString_WinsOverEnvironment()
        {
            var resolver = new ConnectionStringResolver();

            var connectionString = resolver.Resolve(
                ["--connection", "Server=cli;Database=BazarFlow;"],
                name => name == "ConnectionStrings__DefaultConnection" ? "Server=env;Database=BazarFlow;" : null);

            Assert.Equal("Server=cli;Database=BazarFlow;", connectionString);
        }

        [Fact]
        public void Resolver_UsesDefaultConnectionEnvironmentVariable()
        {
            var resolver = new ConnectionStringResolver();

            var connectionString = resolver.Resolve(
                [],
                name => name == "ConnectionStrings__DefaultConnection" ? "Server=env;Database=BazarFlow;" : null);

            Assert.Equal("Server=env;Database=BazarFlow;", connectionString);
        }

        [Fact]
        public void Resolver_UsesBazarFlowFallbackEnvironmentVariable()
        {
            var resolver = new ConnectionStringResolver();

            var connectionString = resolver.Resolve(
                [],
                name => name == "BAZARFLOW_CONNECTION_STRING" ? "Server=fallback;Database=BazarFlow;" : null);

            Assert.Equal("Server=fallback;Database=BazarFlow;", connectionString);
        }

        [Fact]
        public void Sanitizer_HidesPassword()
        {
            var sanitized = ConnectionStringSanitizer.Sanitize("Server=.;Database=BazarFlow;User Id=sa;Password=123456;");

            Assert.DoesNotContain("123456", sanitized);
            Assert.Contains("<redacted>", sanitized);
        }

        [Fact]
        public void Sanitizer_HidesUserId()
        {
            var sanitized = ConnectionStringSanitizer.Sanitize("Server=.;Database=BazarFlow;User Id=sa;Password=123456;");

            Assert.DoesNotContain("sa", sanitized);
            Assert.Contains("<redacted>", sanitized);
        }

        [Fact]
        public void DuplicateBarcodeFailure_DetectsUniqueIndexFailure()
        {
            var exception = new InvalidOperationException("Cannot create unique index UX_PRODUCTS_Barcode on PRODUCTS because duplicate Barcode values exist.");

            Assert.True(DuplicateBarcodeCheck.IsDuplicateBarcodeFailure(exception));
        }
    }
}
