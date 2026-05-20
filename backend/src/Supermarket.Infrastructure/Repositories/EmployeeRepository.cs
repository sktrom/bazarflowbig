using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly SupermarketDbContext _context;
        public EmployeeRepository(SupermarketDbContext context) => _context = context;

        public async Task<Employee?> GetByUsernameAsync(string username)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.Username == username);
        }

        public async Task<Employee?> GetByIdAsync(long employeeId)
        {
            return await _context.Employees.FindAsync(employeeId);
        }

        public async Task UpdatePasswordHashAsync(long employeeId, string passwordHash, DateTime updatedAt)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

            employee.PasswordHash = passwordHash;
            employee.UpdatedAt = updatedAt;
            await _context.SaveChangesAsync();
        }
    }
}
