using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Supermarket.Application.Common.Security
{
    public static class SafeFileNamePolicy
    {
        public const int MaxFileNameLength = 120;

        private static readonly string[] UnsafeSubstrings = new[]
        {
            "..", "/", "\\", ":"
        };

        public static bool IsSafe(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (UnsafeSubstrings.Any(sub => fileName.Contains(sub)))
                return false;

            if (fileName.Any(char.IsControl))
                return false;

            try
            {
                if (Path.IsPathRooted(fileName))
                    return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public static string GetSafeFileName(string? requestedName, string fallbackName)
        {
            if (!IsSafe(requestedName))
            {
                return Sanitize(fallbackName);
            }

            return Sanitize(requestedName!);
        }

        private static string Sanitize(string fileName)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string sanitized = Regex.Replace(fileName, $"[{invalidChars}]+", "_");

            if (sanitized.Length > MaxFileNameLength)
            {
                var ext = Path.GetExtension(sanitized);
                var name = Path.GetFileNameWithoutExtension(sanitized);
                
                int allowedNameLength = MaxFileNameLength - ext.Length;
                if (allowedNameLength > 0)
                {
                    sanitized = name.Substring(0, allowedNameLength) + ext;
                }
                else
                {
                    sanitized = sanitized.Substring(0, MaxFileNameLength);
                }
            }

            return sanitized;
        }
        
        public static string ValidateAndGetFormatExtension(string format)
        {
            string normalizedFormat = format?.ToLowerInvariant() ?? "";
            return normalizedFormat switch
            {
                "excel" => ".xlsx",
                "pdf" => ".pdf",
                _ => throw new InvalidOperationException("INVALID_EXPORT_FORMAT")
            };
        }
    }
}
