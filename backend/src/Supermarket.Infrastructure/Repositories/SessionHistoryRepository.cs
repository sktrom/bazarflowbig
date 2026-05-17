using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Sessions.Interfaces;
using Supermarket.Contracts.Sessions;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class SessionHistoryRepository : ISessionHistoryRepository
    {
        private readonly SupermarketDbContext _context;

        public SessionHistoryRepository(SupermarketDbContext context) => _context = context;

        public async Task<SessionHistoryResponse> GetHistoryAsync(long employeeId, int pageIndex, int pageSize)
        {
            pageIndex = Math.Max(1, pageIndex);
            pageSize  = Math.Max(1, pageSize);

            var baseQuery = _context.CashSessions
                .AsNoTracking()
                .Include(s => s.Device)
                .Where(s => s.EmployeeId == employeeId);

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(s => s.StartedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SessionHistoryItem
                {
                    SessionId  = s.Id,
                    DeviceCode = s.Device != null ? s.Device.DeviceCode : string.Empty,
                    StartedAt  = s.StartedAt,
                    EndedAt    = s.EndedAt,
                    Status     = s.Status.ToString()
                })
                .ToListAsync();

            return new SessionHistoryResponse
            {
                TotalCount = totalCount,
                Items      = items
            };
        }
    }
}
