using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BazarFlow.PerformanceTests;

public sealed class BazarFlowApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, int> _statusCodes = new(StringComparer.OrdinalIgnoreCase);
    private string? _sessionToken;
    private long? _firstInvoiceId;
    private int _blackBoxWarningWritten;
    private int _invoiceWarningWritten;

    public BazarFlowApiClient(Uri baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = baseUrl,
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BazarFlowPerformanceSmoke/1.0");
    }

    public IReadOnlyDictionary<string, int> StatusCodes => _statusCodes;

    public async Task<ApiCallResult> GetSetupStatusAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Get, "api/setup/status", authenticated: false, cancellationToken);
    }

    public async Task<LoginResult> LoginAsync(string username, string password, string deviceCode, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            new { username, password, deviceCode },
            cancellationToken);

        await TrackAsync(response, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new LoginResult(false, $"Login failed with HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("sessionToken", out var tokenElement))
        {
            return new LoginResult(false, "Login response did not contain a session token.");
        }

        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new LoginResult(false, "Login response contained an empty session token.");
        }

        _sessionToken = token;
        return new LoginResult(true, "Login succeeded.");
    }

    public async Task<ApiCallResult> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return await SendAsync(HttpMethod.Get, "api/products", authenticated: true, cancellationToken);
    }

    public async Task<ApiCallResult> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(HttpMethod.Get, "api/invoices?page=1&pageSize=20", authenticated: true, cancellationToken);
        if (result.IsSuccess && _firstInvoiceId is null && !string.IsNullOrWhiteSpace(result.Body))
        {
            _firstInvoiceId = TryReadFirstInvoiceId(result.Body);
        }

        if (result.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized && Interlocked.Exchange(ref _invoiceWarningWritten, 1) == 0)
        {
            Console.WriteLine("Warning: invoice_list is optional and was not authorized for this user.");
        }

        return result;
    }

    public async Task<ApiCallResult> GetInvoiceDetailsAsync(CancellationToken cancellationToken = default)
    {
        var invoiceId = _firstInvoiceId;
        if (invoiceId is null)
        {
            return ApiCallResult.OptionalSkipped("invoice_read skipped because invoice_list returned no invoice id.");
        }

        return await SendAsync(HttpMethod.Get, $"api/invoices/{invoiceId.Value}/details", authenticated: true, cancellationToken);
    }

    public async Task<ApiCallResult> GetBlackBoxEventsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync(HttpMethod.Get, "api/black-box/events?page=1&pageSize=20", authenticated: true, cancellationToken);
        if (result.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized && Interlocked.Exchange(ref _blackBoxWarningWritten, 1) == 0)
        {
            Console.WriteLine("Warning: blackbox_list is optional and was not authorized for this user.");
        }

        return result;
    }

    public void PrintStatusCodeSummary(TextWriter output)
    {
        output.WriteLine("HTTP status code summary:");
        foreach (var pair in _statusCodes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            output.WriteLine($"  {pair.Key}: {pair.Value}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<ApiCallResult> SendAsync(HttpMethod method, string path, bool authenticated, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (authenticated)
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
            {
                return ApiCallResult.Fail(HttpStatusCode.Unauthorized, "No active session token in memory.");
            }

            request.Headers.TryAddWithoutValidation("X-Session-Token", _sessionToken);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return await TrackAsync(response, cancellationToken);
    }

    private async Task<ApiCallResult> TrackAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = ((int)response.StatusCode).ToString();
        _statusCodes.AddOrUpdate(statusCode, 1, (_, current) => current + 1);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ApiCallResult(response.StatusCode, response.IsSuccessStatusCode, body, body.Length);
    }

    private static long? TryReadFirstInvoiceId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() == 0)
            {
                return null;
            }

            var first = items[0];
            return first.TryGetProperty("invoiceId", out var idElement) && idElement.TryGetInt64(out var invoiceId)
                ? invoiceId
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

public sealed record LoginResult(bool IsSuccess, string Message);

public sealed record ApiCallResult(HttpStatusCode StatusCode, bool IsSuccess, string? Body, int SizeBytes)
{
    public static ApiCallResult OptionalSkipped(string message) => new(HttpStatusCode.NoContent, true, message, 0);

    public static ApiCallResult Fail(HttpStatusCode statusCode, string message) => new(statusCode, false, message, 0);
}
