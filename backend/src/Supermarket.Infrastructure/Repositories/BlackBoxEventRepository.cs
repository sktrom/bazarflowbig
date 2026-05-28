using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.BlackBox.Interfaces;
using Supermarket.Contracts.BlackBox;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class BlackBoxEventRepository : IBlackBoxEventRepository
    {
        private readonly SupermarketDbContext _context;

        public BlackBoxEventRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(BlackBoxEvent blackBoxEvent)
        {
            _context.BlackBoxEvents.Add(blackBoxEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<BlackBoxEvent> Items, int TotalCount)> GetPagedAsync(BlackBoxEventQuery query)
        {
            var dbQuery = BuildQuery(query)
                .AsNoTracking()
                .Include(e => e.Employee);

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderByDescending(e => e.CreatedAtUtc)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<BlackBoxEvent?> GetByIdAsync(long id)
        {
            return await _context.BlackBoxEvents
                .AsNoTracking()
                .Include(e => e.Employee)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        private IQueryable<BlackBoxEvent> BuildQuery(BlackBoxEventQuery query)
        {
            var dbQuery = _context.BlackBoxEvents.AsQueryable();

            if (query.DateFrom.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.CreatedAtUtc >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.CreatedAtUtc <= query.DateTo.Value);
            }

            if (query.EmployeeId.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.EmployeeId == query.EmployeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.DeviceCode))
            {
                dbQuery = dbQuery.Where(e => e.DeviceCode == query.DeviceCode);
            }

            if (!string.IsNullOrWhiteSpace(query.ActionType))
            {
                dbQuery = dbQuery.Where(e => e.ActionType == query.ActionType);
            }

            if (!string.IsNullOrWhiteSpace(query.PageName))
            {
                dbQuery = dbQuery.Where(e => e.PageName == query.PageName);
            }

            if (!string.IsNullOrWhiteSpace(query.Result))
            {
                dbQuery = dbQuery.Where(e => e.Result == query.Result);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                dbQuery = dbQuery.Where(e => e.EntityType == query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                dbQuery = dbQuery.Where(e => e.EntityId == query.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(query.Route))
            {
                dbQuery = dbQuery.Where(e => e.Route == query.Route);
            }

            return dbQuery;
        }
    }
}
