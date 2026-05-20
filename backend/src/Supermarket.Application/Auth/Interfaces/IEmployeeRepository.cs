using System;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Auth.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByUsernameAsync(string username);
        Task<Employee?> GetByIdAsync(long employeeId);
        Task UpdatePasswordHashAsync(long employeeId, string passwordHash, DateTime updatedAt);
    }
}
