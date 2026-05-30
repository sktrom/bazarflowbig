using BazarFlow.PerformanceSeeder;
using Xunit;

namespace Supermarket.UnitTests.PerformanceSeeder;

public class PerformanceSeederTests
{
    [Theory]
    [InlineData("BazarFlow")]
    [InlineData("BazarFlowProd")]
    [InlineData("Production")]
    [InlineData("Prod")]
    [InlineData("BazarFlowPerformanceProd")]
    [InlineData("CustomerMain")]
    public void UnsafeDatabaseName_IsRejected(string databaseName)
    {
        Assert.False(SafetyValidator.IsSafeDatabaseName(databaseName));
    }

    [Theory]
    [InlineData("BazarFlowPerformance")]
    [InlineData("BazarFlowPerf")]
    [InlineData("BazarFlowTest")]
    [InlineData("BazarFlowLoad")]
    public void SafeDatabaseName_IsAccepted(string databaseName)
    {
        Assert.True(SafetyValidator.IsSafeDatabaseName(databaseName));
    }

    [Fact]
    public void MissingConfirm_IsRejected()
    {
        var options = new SeederCliOptions("small", null, 12345, Confirm: false, Reset: false, ConfirmReset: false, DryRun: true);

        var result = SafetyValidator.Validate(options, "Server=.;Database=BazarFlowPerformance;");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Contains("--confirm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MissingProfile_IsRejected()
    {
        var options = new SeederCliOptions(null, null, 12345, Confirm: true, Reset: false, ConfirmReset: false, DryRun: true);

        var result = SafetyValidator.Validate(options, "Server=.;Database=BazarFlowPerformance;");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Contains("--profile", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Reset_RequiresConfirmReset()
    {
        var options = new SeederCliOptions("small", null, 12345, Confirm: true, Reset: true, ConfirmReset: false, DryRun: true);

        var result = SafetyValidator.Validate(options, "Server=.;Database=BazarFlowPerformance;");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Contains("--confirm-reset", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProfileSizes_AreCorrect()
    {
        Assert.Equal(new ProfileConfig("small", 10, 5, 500, 3, 3, 1_000), ProfileConfig.Get("small"));
        Assert.Equal(new ProfileConfig("medium", 50, 30, 5_000, 10, 10, 10_000), ProfileConfig.Get("medium"));
        Assert.Equal(new ProfileConfig("large", 150, 100, 20_000, 30, 30, 50_000), ProfileConfig.Get("large"));
    }

    [Fact]
    public void GeneratedBarcodes_AreUniqueForLargeProfile()
    {
        var plan = ReferenceDataGenerator.Generate(ProfileConfig.Get("large"), 12345);

        Assert.Equal(plan.Products.Count, plan.Products.Select(product => product.Barcode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GeneratedDeviceCodes_AreUniqueForLargeProfile()
    {
        var plan = ReferenceDataGenerator.Generate(ProfileConfig.Get("large"), 12345);

        Assert.Equal(plan.Devices.Count, plan.Devices.Select(device => device.DeviceCode).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GeneratedEmployeeUsernames_AreUniqueForLargeProfile()
    {
        var plan = ReferenceDataGenerator.Generate(ProfileConfig.Get("large"), 12345);

        Assert.Equal(plan.Employees.Count, plan.Employees.Select(employee => employee.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(plan.Employees, employee => Assert.EndsWith("@example.test", employee.Username));
    }

    [Fact]
    public void DeterministicGenerator_SameSeedSameOutput()
    {
        var first = SyntheticPreviewGenerator.Barcode(12345, 1);
        var second = SyntheticPreviewGenerator.Barcode(12345, 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void DeterministicGenerator_DifferentSeedDifferentOutput()
    {
        var first = SyntheticPreviewGenerator.ProductName(12345, 1);
        var second = SyntheticPreviewGenerator.ProductName(54321, 1);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ReferenceDataGenerator_SameSeedSameGeneratedData()
    {
        var first = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        var second = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);

        Assert.Equal(first.Products.Select(product => product.Barcode), second.Products.Select(product => product.Barcode));
        Assert.Equal(first.Devices.Select(device => device.DeviceCode), second.Devices.Select(device => device.DeviceCode));
        Assert.Equal(first.Employees.Select(employee => employee.Username), second.Employees.Select(employee => employee.Username));
    }

    [Fact]
    public void ReferenceDataGenerator_DifferentSeedDifferentGeneratedData()
    {
        var first = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 12345);
        var second = ReferenceDataGenerator.Generate(ProfileConfig.Get("small"), 54321);

        Assert.NotEqual(first.Products[0].Barcode, second.Products[0].Barcode);
        Assert.NotEqual(first.Devices[0].DeviceCode, second.Devices[0].DeviceCode);
        Assert.NotEqual(first.Employees[0].Username, second.Employees[0].Username);
    }

    [Fact]
    public async Task DryRun_DoesNotOpenWriter()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await PerformanceSeederApp.RunAsync(
            [
                "--profile", "small",
                "--connection", "Server=.;Database=BazarFlowPerformance;",
                "--confirm",
                "--dry-run"
            ],
            output,
            error,
            _ => throw new InvalidOperationException("Writer should not be opened during dry-run."));

        Assert.Equal(PerformanceSeederExitCodes.Success, exitCode);
        Assert.Contains("Dry-run summary", output.ToString());
    }

    [Fact]
    public async Task UnsafeDatabaseName_PreventsWriter()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await PerformanceSeederApp.RunAsync(
            [
                "--profile", "small",
                "--connection", "Server=.;Database=BazarFlow;",
                "--confirm"
            ],
            output,
            error,
            _ => throw new InvalidOperationException("Writer should not be opened for unsafe database names."));

        Assert.Equal(PerformanceSeederExitCodes.ValidationFailed, exitCode);
        Assert.Contains("Refusing database", error.ToString());
    }

    [Fact]
    public async Task ResetNonDryRun_IsRejectedBeforeWriter()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await PerformanceSeederApp.RunAsync(
            [
                "--profile", "small",
                "--connection", "Server=.;Database=BazarFlowPerformance;",
                "--confirm",
                "--reset",
                "--confirm-reset"
            ],
            output,
            error,
            _ => throw new InvalidOperationException("Writer should not be opened when reset is deferred."));

        Assert.Equal(PerformanceSeederExitCodes.ImplementationPending, exitCode);
        Assert.Contains("Reset is not implemented in V2-06B-2", error.ToString());
    }

    [Fact]
    public async Task ConnectionStringPassword_IsNotPrinted()
    {
        const string password = "super-secret-password";
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await PerformanceSeederApp.RunAsync(
            [
                "--profile", "small",
                "--connection", $"Server=.;Database=BazarFlowPerformance;User Id=sa;Password={password};",
                "--seed", "12345",
                "--confirm",
                "--dry-run"
            ],
            output,
            error);

        var combined = output.ToString() + error;

        Assert.Equal(PerformanceSeederExitCodes.Success, exitCode);
        Assert.DoesNotContain(password, combined);
        Assert.DoesNotContain("User Id=sa", combined);
        Assert.Contains("BazarFlowPerformance", combined);
    }

    [Fact]
    public void Sanitizer_RedactsPassword()
    {
        var sanitized = ConnectionStringInspector.SanitizeForDiagnostics("Server=.;Database=BazarFlowPerformance;User Id=sa;Password=secret;");

        Assert.DoesNotContain("secret", sanitized);
        Assert.Contains("<redacted>", sanitized);
    }

    [Fact]
    public async Task WriteFailure_PrintsClassifiedSanitizedError()
    {
        const string password = "top-secret";
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await PerformanceSeederApp.RunAsync(
            [
                "--profile", "small",
                "--connection", $"Server=.;Database=BazarFlowPerformance;User Id=sa;Password={password};",
                "--confirm"
            ],
            output,
            error,
            _ => new ThrowingReferenceDataWriter(new InvalidOperationException(
                "An exception has been raised that is likely due to a transient failure.",
                new Exception($"A network-related error occurred. User Id=sa;Password={password};"))));

        var text = error.ToString();

        Assert.Equal(PerformanceSeederExitCodes.SeedFailed, exitCode);
        Assert.Contains("SQL_CONNECTION_FAILED", text);
        Assert.Contains("InvalidOperationException", text);
        Assert.Contains("Inner exception: Exception", text);
        Assert.Contains("Run BazarFlow.DbMigrator against the performance database before seeding.", text);
        Assert.DoesNotContain(password, text);
        Assert.DoesNotContain("User Id=sa", text);
    }

    private sealed class ThrowingReferenceDataWriter : IReferenceDataWriter
    {
        private readonly Exception _exception;

        public ThrowingReferenceDataWriter(Exception exception)
        {
            _exception = exception;
        }

        public Task<ReferenceDataSeedResult> SeedAsync(ReferenceDataPlan plan, TextWriter output, CancellationToken cancellationToken = default)
        {
            throw _exception;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
