using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly SupermarketDbContext _context;

        public AuditLogRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
            long? employeeId,
            string? action,
            string? entityType,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .Include(a => a.Employee)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= dateTo.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<AuditLog?> GetByIdAsync(long id)
        {
            return await _context.AuditLogs
                .AsNoTracking()
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<(int TotalCount, DateTime? OldestCreatedAt, DateTime? NewestCreatedAt, int ApproximateLargeJsonCount)> GetStatusDetailsAsync()
        {
            var totalCount = await _context.AuditLogs.CountAsync();
            
            DateTime? oldestCreatedAt = null;
            DateTime? newestCreatedAt = null;
            
            if (totalCount > 0)
            {
                oldestCreatedAt = await _context.AuditLogs
                    .AsNoTracking()
                    .MinAsync(a => (DateTime?)a.CreatedAt);
                
                newestCreatedAt = await _context.AuditLogs
                    .AsNoTracking()
                    .MaxAsync(a => (DateTime?)a.CreatedAt);
            }

            var approximateLargeJsonCount = await _context.AuditLogs
                .AsNoTracking()
                .CountAsync(a => 
                    (a.BeforeJson != null && a.BeforeJson != "") || 
                    (a.AfterJson != null && a.AfterJson != "") || 
                    (a.MetadataJson != null && a.MetadataJson != "")
                );

            return (totalCount, oldestCreatedAt, newestCreatedAt, approximateLargeJsonCount);
        }
    }
}

