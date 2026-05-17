using System.Threading.Tasks;
using Supermarket.Contracts.Auth;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LogoutResponse> LogoutAsync(long sessionId);
        Task<CurrentEmployeeResponse> GetCurrentEmployeeAsync(long sessionId);
        Task<EmployeePermissionsResponse> GetPermissionsAsync(long employeeId);
    }
}
