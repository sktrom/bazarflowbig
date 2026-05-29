using System;
using System.Threading.Tasks;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Services
{
    public class LoginThrottleService : ILoginThrottleService
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(10);

        private readonly IAppLoginAttemptRepository _repository;

        public LoginThrottleService(IAppLoginAttemptRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> IsBlockedAsync(string username, string ipAddress)
        {
            var normalizedUsername = (username ?? string.Empty).Trim().ToLowerInvariant();
            
            // Check if explicitly blocked in the last 10 minutes
            var blockSinceUtc = DateTime.UtcNow.Subtract(BlockDuration);
            if (await _repository.HasRecentBlockAsync(normalizedUsername, ipAddress, blockSinceUtc))
                return true;

            // Otherwise check if reached max failures in the last 5 minutes
            var windowSinceUtc = DateTime.UtcNow.Subtract(WindowDuration);
            var failedCount = await _repository.CountRecentFailedAttemptsAsync(normalizedUsername, ipAddress, windowSinceUtc);

            return failedCount >= MaxAttempts;
        }

        public async Task RecordFailedAttemptAsync(string username, string ipAddress, string userAgent, string reason)
        {
            var attempt = new AppLoginAttempt
            {
                UsernameNormalized = (username ?? string.Empty).Trim().ToLowerInvariant(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Result = "Failed",
                FailureReason = reason,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(attempt);
        }

        public async Task RecordBlockedAttemptAsync(string username, string ipAddress, string userAgent)
        {
            var attempt = new AppLoginAttempt
            {
                UsernameNormalized = (username ?? string.Empty).Trim().ToLowerInvariant(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Result = "Blocked",
                FailureReason = "LOGIN_THROTTLED",
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repository.AddAsync(attempt);
        }

        public async Task ResetAsync(string username, string ipAddress)
        {
            var normalizedUsername = (username ?? string.Empty).Trim().ToLowerInvariant();
            await _repository.ClearAttemptsAsync(normalizedUsername, ipAddress);
        }
    }
}
