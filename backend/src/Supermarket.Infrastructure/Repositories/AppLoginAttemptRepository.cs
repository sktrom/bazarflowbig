using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class AppLoginAttemptRepository : IAppLoginAttemptRepository
    {
        private readonly SupermarketDbContext _context;

        public AppLoginAttemptRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AppLoginAttempt attempt)
        {
            _context.AppLoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountRecentFailedAttemptsAsync(string usernameNormalized, string ipAddress, DateTime sinceUtc)
        {
            return await _context.AppLoginAttempts
                .Where(x => x.UsernameNormalized == usernameNormalized &&
                            x.IpAddress == ipAddress &&
                            x.CreatedAtUtc >= sinceUtc &&
                            x.Result == "Failed")
                .CountAsync();
        }

        public async Task<bool> HasRecentBlockAsync(string usernameNormalized, string ipAddress, DateTime sinceUtc)
        {
            return await _context.AppLoginAttempts
                .AnyAsync(x => x.UsernameNormalized == usernameNormalized &&
                               x.IpAddress == ipAddress &&
                               x.CreatedAtUtc >= sinceUtc &&
                               x.Result == "Blocked");
        }

        public async Task ClearAttemptsAsync(string usernameNormalized, string ipAddress)
        {
            var attempts = await _context.AppLoginAttempts
                .Where(x => x.UsernameNormalized == usernameNormalized && x.IpAddress == ipAddress)
                .ToListAsync();

            if (attempts.Any())
            {
                _context.AppLoginAttempts.RemoveRange(attempts);
                await _context.SaveChangesAsync();
            }
        }
    }
}
