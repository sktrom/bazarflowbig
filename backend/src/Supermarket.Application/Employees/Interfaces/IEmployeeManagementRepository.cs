using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Employees.Interfaces
{
    public interface IEmployeeManagementRepository
    {
        Task<IReadOnlyList<Employee>> GetAllAsync();
        Task<Employee?> GetByIdAsync(long id);
        Task<Employee?> GetByUsernameAsync(string username);
        Task<Employee> CreateAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(long id);
        Task<bool> HasRelatedRecordsAsync(long id);
    }
}
