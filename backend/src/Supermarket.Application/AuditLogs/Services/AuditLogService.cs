using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.AuditLogs.Services
{
    public class AuditLogService : IAuditLogService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly string[] SensitiveKeyParts =
        {
            "password",
            "passwordhash",
            "newpassword",
            "token",
            "connectionstring",
            "secret"
        };

        private readonly IAuditLogRepository _repository;
        private readonly ISessionContext _sessionContext;

        public AuditLogService(IAuditLogRepository repository, ISessionContext sessionContext)
        {
            _repository = repository;
            _sessionContext = sessionContext;
        }

        public async Task RecordAsync(
            string action,
            string entityType,
            string? entityId = null,
            string? entityDisplayName = null,
            object? before = null,
            object? after = null,
            object? metadata = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EmployeeId = _sessionContext.IsAuthenticated && _sessionContext.EmployeeId > 0
                        ? _sessionContext.EmployeeId
                        : null,
                    SessionId = _sessionContext.IsAuthenticated && _sessionContext.SessionId > 0
                        ? _sessionContext.SessionId
                        : null,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityDisplayName = entityDisplayName,
                    BeforeJson = SerializeSanitized(before),
                    AfterJson = SerializeSanitized(after),
                    MetadataJson = SerializeSanitized(metadata),
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.CreateAsync(auditLog);
            }
            catch
            {
                // Audit logging is best-effort and must not break business operations.
            }
        }

        public static string? SerializeSanitized(object? value)
        {
            if (value == null) return null;
            var sanitized = SanitizeValue(value);
            return JsonSerializer.Serialize(sanitized, JsonOptions);
        }

        private static object? SanitizeValue(object? value)
        {
            if (value == null) return null;
            if (value is string or ValueType) return value;

            if (value is IDictionary dictionary)
            {
                var result = new Dictionary<string, object?>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString() ?? string.Empty;
                    if (IsSensitiveKey(key)) continue;
                    result[key] = SanitizeValue(entry.Value);
                }
                return result;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var result = new List<object?>();
                foreach (var item in enumerable)
                    result.Add(SanitizeValue(item));
                return result;
            }

            return value.GetType()
                .GetProperties()
                .Where(property => property.GetIndexParameters().Length == 0 && !IsSensitiveKey(property.Name))
                .ToDictionary(
                    property => property.Name,
                    property => SanitizeValue(property.GetValue(value)));
        }

        private static bool IsSensitiveKey(string key)
        {
            var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

            return SensitiveKeyParts.Any(part => normalized.Contains(part));
        }
    }
}
