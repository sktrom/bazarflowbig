using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Supermarket.Application.BlackBox.Interfaces;

namespace Supermarket.Application.BlackBox.Services
{
    public class BlackBoxMetadataSanitizer : IBlackBoxMetadataSanitizer
    {
        private const int MaxMetadataBytes = 4096;

        private static readonly string[] SensitiveKeys =
        [
            "password",
            "pwd",
            "token",
            "secret",
            "connectionString",
            "authorization",
            "sessionToken",
            "passwordHash",
            "passwordSalt",
            "apiKey"
        ];

        public string? Sanitize(Dictionary<string, object?>? metadata, out bool metadataTruncated)
        {
            metadataTruncated = false;
            if (metadata == null || metadata.Count == 0)
            {
                return null;
            }

            try
            {
                var json = JsonSerializer.Serialize(metadata);
                var node = JsonNode.Parse(json);
                if (node == null)
                {
                    return null;
                }

                RemoveSensitiveKeys(node);

                var sanitizedJson = node.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                if (System.Text.Encoding.UTF8.GetByteCount(sanitizedJson) <= MaxMetadataBytes)
                {
                    return sanitizedJson;
                }

                metadataTruncated = true;
                return JsonSerializer.Serialize(new
                {
                    metadataTruncated = true,
                    message = $"Metadata exceeded {MaxMetadataBytes} bytes and was not stored."
                });
            }
            catch
            {
                return null;
            }
        }

        private static void RemoveSensitiveKeys(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var keysToRemove = obj
                    .Select(property => property.Key)
                    .Where(IsSensitiveKey)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    obj.Remove(key);
                }

                foreach (var child in obj.Select(property => property.Value).Where(value => value != null))
                {
                    RemoveSensitiveKeys(child!);
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var child in array.Where(value => value != null))
                {
                    RemoveSensitiveKeys(child!);
                }
            }
        }

        private static bool IsSensitiveKey(string key)
        {
            return SensitiveKeys.Any(sensitive =>
                key.Equals(sensitive, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
        }
    }
}
