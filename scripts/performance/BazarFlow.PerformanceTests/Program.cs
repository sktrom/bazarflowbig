using BazarFlow.PerformanceTests;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

var parsed = PerformanceTestOptions.Parse(args);
if (!parsed.IsSuccess)
{
    Console.Error.WriteLine(parsed.Error);
    return 2;
}

var options = parsed.Options!;
Directory.CreateDirectory(options.Output);

Console.WriteLine("BazarFlow NBomber smoke test");
Console.WriteLine($"Base URL: {options.BaseUrl}");
Console.WriteLine($"Duration seconds: {options.DurationSeconds}");
Console.WriteLine($"Concurrent users: {options.Users}");
Console.WriteLine($"Output: {options.Output}");
Console.WriteLine("Credentials and session token are never printed.");
Console.WriteLine();

using var api = new BazarFlowApiClient(options.BaseUrl);

var setupStatus = await api.GetSetupStatusAsync();
if (!setupStatus.IsSuccess)
{
    Console.WriteLine($"Warning: setup/status preflight returned HTTP {(int)setupStatus.StatusCode}.");
}

var login = await api.LoginAsync(options.Username!, options.Password!, options.DeviceCode!);
if (!login.IsSuccess)
{
    Console.Error.WriteLine(login.Message);
    return 3;
}

var productPreflight = await api.GetProductsAsync();
if (!productPreflight.IsSuccess)
{
    Console.Error.WriteLine($"Product list preflight failed with HTTP {(int)productPreflight.StatusCode}. Ensure the test user has Products permission.");
    return 4;
}

var invoicePreflight = await api.GetInvoicesAsync();
if (!invoicePreflight.IsSuccess)
{
    Console.WriteLine($"Warning: invoice_list preflight returned HTTP {(int)invoicePreflight.StatusCode}; invoice scenarios remain optional.");
}

var blackBoxPreflight = await api.GetBlackBoxEventsAsync();
if (!blackBoxPreflight.IsSuccess)
{
    Console.WriteLine($"Warning: blackbox_list preflight returned HTTP {(int)blackBoxPreflight.StatusCode}; blackbox scenario remains optional.");
}

var duration = TimeSpan.FromSeconds(options.DurationSeconds);
var load = Simulation.KeepConstant(options.Users, duration);

var setupScenario = CreateScenario(
    "health_or_setup_status",
    "setup_status",
    () => api.GetSetupStatusAsync(),
    required: false,
    load);

var productsScenario = CreateScenario(
    "product_list",
    "get_products",
    () => api.GetProductsAsync(),
    required: true,
    load);

var invoicesScenario = CreateScenario(
    "invoice_list",
    "get_invoices",
    () => api.GetInvoicesAsync(),
    required: false,
    load);

var invoiceReadScenario = CreateScenario(
    "invoice_read",
    "get_invoice_details",
    () => api.GetInvoiceDetailsAsync(),
    required: false,
    load);

var blackBoxScenario = CreateScenario(
    "blackbox_list",
    "get_blackbox_events",
    () => api.GetBlackBoxEventsAsync(),
    required: false,
    load);

NBomberRunner
    .RegisterScenarios(setupScenario, productsScenario, invoicesScenario, invoiceReadScenario, blackBoxScenario)
    .WithTestSuite("BazarFlow")
    .WithTestName("V2-06D Smoke Test")
    .WithReportFolder(options.Output)
    .WithReportFileName($"bazarflow-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Txt, ReportFormat.Md)
    .Run();

api.PrintStatusCodeSummary(Console.Out);
return 0;

static ScenarioProps CreateScenario(
    string scenarioName,
    string stepName,
    Func<Task<ApiCallResult>> action,
    bool required,
    LoadSimulation load)
{
    return Scenario
        .Create(scenarioName, async context =>
        {
            var response = await Step.Run(stepName, context, async () =>
            {
                try
                {
                    var result = await action();
                    if (result.IsSuccess || !required)
                    {
                        return Response.Ok(
                            statusCode: ((int)result.StatusCode).ToString(),
                            sizeBytes: result.SizeBytes,
                            message: result.IsSuccess ? string.Empty : "optional endpoint unavailable");
                    }

                    return Response.Fail(
                        statusCode: ((int)result.StatusCode).ToString(),
                        message: "required endpoint failed",
                        sizeBytes: result.SizeBytes);
                }
                catch (Exception ex)
                {
                    return required
                        ? Response.Fail(statusCode: "EXCEPTION", message: ex.GetType().Name)
                        : Response.Ok(statusCode: "OPTIONAL_EXCEPTION", message: ex.GetType().Name);
                }
            });

            return response;
        })
        .WithoutWarmUp()
        .WithLoadSimulations(load);
}
