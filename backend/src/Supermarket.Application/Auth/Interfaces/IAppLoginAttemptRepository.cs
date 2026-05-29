using System;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IAppLoginAttemptRepository
    {
        Task AddAsync(AppLoginAttempt attempt);
        Task<int> CountRecentFailedAttemptsAsync(string usernameNormalized, string ipAddress, DateTime sinceUtc);
        Task<bool> HasRecentBlockAsync(string usernameNormalized, string ipAddress, DateTime sinceUtc);
        Task ClearAttemptsAsync(string usernameNormalized, string ipAddress);
    }
}
