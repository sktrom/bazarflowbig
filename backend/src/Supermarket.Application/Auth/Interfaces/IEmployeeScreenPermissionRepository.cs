using System.Collections.Generic;
using System.Threading.Tasks;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IEmployeeScreenPermissionRepository
    {
        Task<IReadOnlyList<string>> GetAllowedScreenKeysAsync(long employeeId);
    }
}
