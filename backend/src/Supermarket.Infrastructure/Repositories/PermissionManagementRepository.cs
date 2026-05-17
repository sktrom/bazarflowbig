using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class PermissionManagementRepository : IPermissionManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public PermissionManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<EmployeePermissionView>> GetFullPermissionsAsync(long employeeId)
        {
            var allScreens = await _context.AppScreens.ToListAsync();
            var employeePermissions = await _context.EmployeeScreenPermissions
                .Where(p => p.EmployeeId == employeeId)
                .ToListAsync();

            return allScreens.Select(s => new EmployeePermissionView
            {
                ScreenId = s.Id,
                ScreenKey = s.ScreenKey,
                ScreenName = s.ScreenName,
                CanAccess = employeePermissions.FirstOrDefault(ep => ep.ScreenId == s.Id)?.CanAccess ?? false
            }).ToList();
        }

        public async Task ReplacePermissionsAsync(long employeeId, IReadOnlyList<(int ScreenId, bool CanAccess)> permissions)
        {
            var existingPermissions = await _context.EmployeeScreenPermissions
                .Where(p => p.EmployeeId == employeeId)
                .ToListAsync();

            _context.EmployeeScreenPermissions.RemoveRange(existingPermissions);

            var newPermissions = permissions.Select(p => new EmployeeScreenPermission
            {
                EmployeeId = employeeId,
                ScreenId = p.ScreenId,
                CanAccess = p.CanAccess
            });

            _context.EmployeeScreenPermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllForEmployeeAsync(long employeeId)
        {
            var permissions = await _context.EmployeeScreenPermissions
                .Where(p => p.EmployeeId == employeeId)
                .ToListAsync();

            _context.EmployeeScreenPermissions.RemoveRange(permissions);
            await _context.SaveChangesAsync();
        }

        public async Task<int?> GetScreenIdByKeyAsync(string screenKey)
        {
            var screen = await _context.AppScreens.FirstOrDefaultAsync(s => s.ScreenKey == screenKey);
            return screen?.Id;
        }
    }
}
