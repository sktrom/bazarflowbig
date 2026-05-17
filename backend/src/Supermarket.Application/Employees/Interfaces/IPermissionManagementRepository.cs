using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Employees.Interfaces
{
    public class EmployeePermissionView
    {
        public int ScreenId { get; set; }
        public string ScreenKey { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
    }

    public interface IPermissionManagementRepository
    {
        Task<IReadOnlyList<EmployeePermissionView>> GetFullPermissionsAsync(long employeeId);
        Task ReplacePermissionsAsync(long employeeId, IReadOnlyList<(int ScreenId, bool CanAccess)> permissions);
        Task DeleteAllForEmployeeAsync(long employeeId);
        Task<int?> GetScreenIdByKeyAsync(string screenKey);
    }
}
