using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class AuthSessionRepository : IAuthSessionRepository
    {
        private readonly SupermarketDbContext _context;

        public AuthSessionRepository(SupermarketDbContext context) => _context = context;

        public async Task<CashSession?> GetActiveByEmployeeIdAsync(long employeeId)
        {
            return await _context.CashSessions
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Status == CashSessionStatus.Active);
        }

        public async Task<CashSession?> GetActiveByIdAsync(long sessionId)
        {
            return await _context.CashSessions
                .Include(s => s.Device)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == CashSessionStatus.Active);
        }

        public async Task<CashSession?> GetActiveByTokenAsync(string token)
        {
            return await _context.CashSessions
                .Include(s => s.Device)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.SessionToken == token && s.Status == CashSessionStatus.Active);
        }

        public async Task<CashSession?> GetByIdAsync(long sessionId)
        {
            return await _context.CashSessions
                .Include(s => s.Device)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task CreateAsync(CashSession session)
        {
            _context.CashSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task TouchAsync(long sessionId, DateTime lastSeenAt, DateTime expiresAt)
        {
            var session = await _context.CashSessions.FindAsync(sessionId);
            if (session != null && session.Status == CashSessionStatus.Active)
            {
                session.LastSeenAt = lastSeenAt;
                session.ExpiresAt = expiresAt;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ExpireAsync(long sessionId, DateTime endedAt)
        {
            var session = await _context.CashSessions.FindAsync(sessionId);
            if (session != null && session.Status == CashSessionStatus.Active)
            {
                session.Status = CashSessionStatus.ForceClosed;
                session.EndedAt = endedAt;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<System.Collections.Generic.IEnumerable<CashSession>> GetActiveSessionsAsync()
        {
            return await _context.CashSessions
                .AsNoTracking()
                .Include(s => s.Employee)
                .Include(s => s.Device)
                .Where(s => s.Status == CashSessionStatus.Active)
                .ToListAsync();
        }

        public async Task ForceCloseAsync(long sessionId, DateTime endedAt)
        {
            var session = await _context.CashSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Status = CashSessionStatus.ForceClosed;
                session.EndedAt = endedAt;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CloseAsync(long sessionId, DateTime closedAt)
        {
            var session = await _context.CashSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Status = CashSessionStatus.Closed;
                session.EndedAt = closedAt;
                await _context.SaveChangesAsync();
            }
        }
    }
}
