using System.Text.Json;

namespace BazarFlow.PerformanceSeeder;

public static class BlackBoxDataGenerator
{
    public const string SourceName = "BazarFlow.PerformanceSeeder";

    private static readonly DateTime AnchorUtc = new(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] ForbiddenMetadataKeys =
    [
        "password",
        "passwordHash",
        "token",
        "sessionToken",
        "connectionString",
        "authorization",
        "cookie",
        "apiKey",
        "secret"
    ];

    public static BlackBoxEventPlan Generate(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticProductRef> products,
        IReadOnlyList<SyntheticEmployeeRef> employees,
        IReadOnlyList<SyntheticDeviceRef> devices,
        IReadOnlyList<SyntheticInvoiceRef> invoices,
        IReadOnlyList<SyntheticPurchaseRef> purchases,
        int startIndex = 1,
        int? count = null)
    {
        if (products.Count == 0 || employees.Count == 0 || devices.Count == 0)
        {
            throw new InvalidOperationException("Run core reference data generation first.");
        }

        if (invoices.Count == 0 || purchases.Count == 0)
        {
            throw new InvalidOperationException("Run transactional generation first.");
        }

        var eventCount = count ?? profile.BlackBoxEvents;
        var events = new List<SyntheticBlackBoxEventSpec>(eventCount);
        for (var eventIndex = startIndex; eventIndex < startIndex + eventCount; eventIndex++)
        {
            events.Add(CreateEvent(seed, profile, products, employees, devices, invoices, purchases, eventIndex));
        }

        return new BlackBoxEventPlan(seed, profile, events);
    }

    public static string EventKey(int seed, int index) =>
        $"BF-PERF-BBX-{SyntheticPreviewGenerator.SeedToken(seed)}-{index:D6}";

    public static bool ContainsSensitiveMetadataKey(string metadataJson)
    {
        using var document = JsonDocument.Parse(metadataJson);
        return ContainsSensitiveProperty(document.RootElement);
    }

    private static SyntheticBlackBoxEventSpec CreateEvent(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticProductRef> products,
        IReadOnlyList<SyntheticEmployeeRef> employees,
        IReadOnlyList<SyntheticDeviceRef> devices,
        IReadOnlyList<SyntheticInvoiceRef> invoices,
        IReadOnlyList<SyntheticPurchaseRef> purchases,
        int eventIndex)
    {
        var action = SelectAction(eventIndex);
        var employee = employees[(eventIndex - 1) % employees.Count];
        var device = devices[(eventIndex * 7) % devices.Count];
        var eventKey = EventKey(seed, eventIndex);
        var metadata = CreateMetadata(seed, eventKey);
        var ipAddress = $"127.0.0.{(eventIndex % 250) + 1}";
        const string userAgent = "BazarFlowPerfSeeder/1.0";

        return action switch
        {
            "COMPLETE_INVOICE" => CreateInvoiceEvent(seed, profile, invoices, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", null, "/cashier", "Cashier"),
            "PRINT_RECEIPT" => CreateInvoiceEvent(seed, profile, invoices, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", "print-receipt", "/invoices", "Invoices"),
            "COMPLETE_PURCHASE" => CreatePurchaseEvent(seed, profile, purchases, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent),
            "CREATE_PRODUCT" => CreateProductEvent(seed, profile, products, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action),
            "UPDATE_PRODUCT" => CreateProductEvent(seed, profile, products, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action),
            "EXPORT_INVOICES" => CreateGenericEvent(seed, profile, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", "export-invoices", "/reports/invoices", "InvoiceReports"),
            "CREATE_BACKUP" => CreateGenericEvent(seed, profile, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", "create-backup", "/settings/backup", "Backup"),
            "FORCE_CLOSE_SESSION" => CreateGenericEvent(seed, profile, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", "force-close-session", "/sessions", "Sessions"),
            "LOGIN_FAILED" => CreateGenericEvent(seed, profile, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Failed", "login-submit", "/login", "Login"),
            _ => CreateGenericEvent(seed, profile, eventIndex, employee, device, eventKey, metadata, ipAddress, userAgent, action, "Success", "login-submit", "/login", "Login")
        };
    }

    private static SyntheticBlackBoxEventSpec CreateInvoiceEvent(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticInvoiceRef> invoices,
        int eventIndex,
        SyntheticEmployeeRef employee,
        SyntheticDeviceRef device,
        string eventKey,
        string metadata,
        string ipAddress,
        string userAgent,
        string actionType,
        string result,
        string? elementKey,
        string route,
        string pageName)
    {
        var invoice = invoices[(eventIndex * 17) % invoices.Count];
        return CreateSpec(
            employee.Id,
            device.DeviceCode,
            route,
            pageName,
            actionType,
            elementKey,
            "Invoice",
            invoice.Id.ToString(),
            result,
            eventKey,
            metadata,
            ipAddress,
            userAgent,
            NearEntityDate(seed, eventIndex, profile.DateWindowDays, invoice.CompletedAt ?? invoice.CreatedAt));
    }

    private static SyntheticBlackBoxEventSpec CreatePurchaseEvent(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticPurchaseRef> purchases,
        int eventIndex,
        SyntheticEmployeeRef employee,
        SyntheticDeviceRef device,
        string eventKey,
        string metadata,
        string ipAddress,
        string userAgent)
    {
        var purchase = purchases[(eventIndex * 19) % purchases.Count];
        return CreateSpec(
            employee.Id,
            device.DeviceCode,
            "/purchases",
            "Purchases",
            "COMPLETE_PURCHASE",
            null,
            "PurchaseInvoice",
            purchase.Id.ToString(),
            "Success",
            eventKey,
            metadata,
            ipAddress,
            userAgent,
            NearEntityDate(seed, eventIndex, profile.DateWindowDays, purchase.CompletedAt ?? purchase.CreatedAt));
    }

    private static SyntheticBlackBoxEventSpec CreateProductEvent(
        int seed,
        TransactionProfileConfig profile,
        IReadOnlyList<SyntheticProductRef> products,
        int eventIndex,
        SyntheticEmployeeRef employee,
        SyntheticDeviceRef device,
        string eventKey,
        string metadata,
        string ipAddress,
        string userAgent,
        string actionType)
    {
        var product = products[(eventIndex * 23) % products.Count];
        return CreateSpec(
            employee.Id,
            device.DeviceCode,
            "/products",
            "Products",
            actionType,
            actionType == "CREATE_PRODUCT" ? "create-product" : "update-product",
            "Product",
            product.Id.ToString(),
            "Success",
            eventKey,
            metadata,
            ipAddress,
            userAgent,
            CreateWindowDate(seed, eventIndex, profile.DateWindowDays));
    }

    private static SyntheticBlackBoxEventSpec CreateGenericEvent(
        int seed,
        TransactionProfileConfig profile,
        int eventIndex,
        SyntheticEmployeeRef employee,
        SyntheticDeviceRef device,
        string eventKey,
        string metadata,
        string ipAddress,
        string userAgent,
        string actionType,
        string result,
        string elementKey,
        string route,
        string pageName) =>
        CreateSpec(
            employee.Id,
            device.DeviceCode,
            route,
            pageName,
            actionType,
            elementKey,
            null,
            null,
            result,
            eventKey,
            metadata,
            ipAddress,
            userAgent,
            CreateWindowDate(seed, eventIndex, profile.DateWindowDays));

    private static SyntheticBlackBoxEventSpec CreateSpec(
        long? employeeId,
        string? deviceCode,
        string? route,
        string? pageName,
        string actionType,
        string? elementKey,
        string? entityType,
        string? entityId,
        string result,
        string eventKey,
        string metadata,
        string ipAddress,
        string userAgent,
        DateTime createdAtUtc) =>
        new(
            employeeId,
            deviceCode,
            route,
            pageName,
            actionType,
            elementKey,
            entityType,
            entityId,
            result,
            $"Synthetic event {eventKey}",
            metadata,
            false,
            ipAddress,
            userAgent,
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc));

    private static string SelectAction(int eventIndex)
    {
        var bucket = (eventIndex - 1) % 100;
        if (bucket < 45)
        {
            return "COMPLETE_INVOICE";
        }

        if (bucket < 60)
        {
            return "PRINT_RECEIPT";
        }

        if (bucket < 70)
        {
            return "LOGIN_SUCCESS";
        }

        if (bucket < 75)
        {
            return "LOGIN_FAILED";
        }

        if (bucket < 80)
        {
            return "CREATE_PRODUCT";
        }

        if (bucket < 85)
        {
            return "UPDATE_PRODUCT";
        }

        if (bucket < 92)
        {
            return "COMPLETE_PURCHASE";
        }

        if (bucket < 95)
        {
            return "EXPORT_INVOICES";
        }

        if (bucket < 98)
        {
            return "CREATE_BACKUP";
        }

        return "FORCE_CLOSE_SESSION";
    }

    private static string CreateMetadata(int seed, string eventKey)
    {
        var metadata = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["synthetic"] = true,
            ["source"] = SourceName,
            ["seed"] = seed,
            ["eventKey"] = eventKey
        });

        if (ContainsSensitiveMetadataKey(metadata))
        {
            throw new InvalidOperationException("Synthetic BlackBox metadata contains a forbidden key.");
        }

        return metadata;
    }

    private static DateTime NearEntityDate(int seed, int eventIndex, int dateWindowDays, DateTime entityDate)
    {
        var random = CreateRandom(seed, eventIndex, 2_202);
        var date = DateTime.SpecifyKind(entityDate, DateTimeKind.Utc).AddMinutes(random.Next(0, 11));
        return ClampToWindow(date, dateWindowDays);
    }

    private static DateTime CreateWindowDate(int seed, int eventIndex, int dateWindowDays)
    {
        var random = CreateRandom(seed, eventIndex, 2_203);
        return AnchorUtc
            .AddDays(-random.Next(0, dateWindowDays))
            .AddHours(random.Next(0, 24))
            .AddMinutes(random.Next(0, 60))
            .AddSeconds(random.Next(0, 60));
    }

    private static DateTime ClampToWindow(DateTime date, int dateWindowDays)
    {
        var earliest = AnchorUtc.AddDays(-dateWindowDays);
        var latest = AnchorUtc.AddDays(1).AddTicks(-1);
        if (date < earliest)
        {
            return earliest;
        }

        return date > latest ? latest : date;
    }

    private static Random CreateRandom(int seed, int index, int salt)
    {
        var mixedSeed = unchecked((seed * 397) ^ (index * 104729) ^ (salt * 7919));
        return new Random(mixedSeed);
    }

    private static bool ContainsSensitiveProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ForbiddenMetadataKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase) ||
                    ContainsSensitiveProperty(property.Value))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ContainsSensitiveProperty(item))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
