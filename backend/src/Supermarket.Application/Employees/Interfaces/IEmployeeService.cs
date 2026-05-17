using System.Threading.Tasks;
using Supermarket.Contracts.Employees;

namespace Supermarket.Application.Employees.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeListResponse> GetAllAsync();
        Task<EmployeeDetailResponse> GetByIdAsync(long id);
        Task<EmployeeDetailResponse> CreateAsync(CreateEmployeeRequest request);
        Task<EmployeeDetailResponse> UpdateAsync(long id, UpdateEmployeeRequest request);
        Task<DeleteEmployeeResponse> DeleteAsync(long id, long currentEmployeeId);
        Task<ResetPasswordResponse> ResetPasswordAsync(long id, ResetPasswordRequest request);
    }
}
