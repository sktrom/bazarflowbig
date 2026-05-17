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
