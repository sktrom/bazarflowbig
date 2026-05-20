using System;
using System.Collections.Generic;
using Supermarket.Application.Auth.Interfaces;

namespace Supermarket.Application.Auth.Services
{
    public class LoginThrottleService : ILoginThrottleService
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        private readonly object _sync = new();
        private readonly Dictionary<string, LoginThrottleEntry> _attempts = new();

        public bool IsThrottled(string username, string deviceCode)
        {
            var now = DateTime.UtcNow;
            var key = BuildKey(username, deviceCode);

            lock (_sync)
            {
                CleanupExpired(now);

                if (!_attempts.TryGetValue(key, out var entry))
                    return false;

                return entry.LockoutUntilUtc.HasValue && entry.LockoutUntilUtc.Value > now;
            }
        }

        public void RecordFailedAttempt(string username, string deviceCode)
        {
            var now = DateTime.UtcNow;
            var key = BuildKey(username, deviceCode);

            lock (_sync)
            {
                CleanupExpired(now);

                if (!_attempts.TryGetValue(key, out var entry) || now - entry.WindowStartedAtUtc > WindowDuration)
                {
                    entry = new LoginThrottleEntry
                    {
                        Count = 0,
                        WindowStartedAtUtc = now
                    };
                }

                entry.Count++;
                if (entry.Count >= MaxAttempts)
                    entry.LockoutUntilUtc = now.Add(LockoutDuration);

                _attempts[key] = entry;
            }
        }

        public void Reset(string username, string deviceCode)
        {
            var key = BuildKey(username, deviceCode);

            lock (_sync)
            {
                _attempts.Remove(key);
            }
        }

        private static string BuildKey(string username, string deviceCode)
        {
            return $"{(username ?? string.Empty).Trim().ToLowerInvariant()}|{(deviceCode ?? string.Empty).Trim()}";
        }

        private void CleanupExpired(DateTime now)
        {
            var expiredKeys = new List<string>();
            foreach (var pair in _attempts)
            {
                var entry = pair.Value;
                var lockoutExpired = !entry.LockoutUntilUtc.HasValue || entry.LockoutUntilUtc.Value <= now;
                var windowExpired = now - entry.WindowStartedAtUtc > WindowDuration;

                if (lockoutExpired && windowExpired)
                    expiredKeys.Add(pair.Key);
            }

            foreach (var key in expiredKeys)
                _attempts.Remove(key);
        }

        private sealed class LoginThrottleEntry
        {
            public int Count { get; set; }
            public DateTime WindowStartedAtUtc { get; set; }
            public DateTime? LockoutUntilUtc { get; set; }
        }
    }
}
