using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class EmployeeScreenPermissionRepository : IEmployeeScreenPermissionRepository
    {
        private readonly SupermarketDbContext _context;

        public EmployeeScreenPermissionRepository(SupermarketDbContext context) => _context = context;

        public async Task<IReadOnlyList<string>> GetAllowedScreenKeysAsync(long employeeId)
        {
            var keys = await _context.EmployeeScreenPermissions
                .Include(p => p.Screen)
                .Where(p => p.EmployeeId == employeeId && p.CanAccess && p.Screen != null)
                .Select(p => p.Screen!.ScreenKey)
                .ToListAsync();

            return keys.AsReadOnly();
        }
    }
}
