using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class EmployeeManagementRepository : IEmployeeManagementRepository
    {
        private readonly SupermarketDbContext _context;

        public EmployeeManagementRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Employee>> GetAllAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(long id)
        {
            return await _context.Employees.FindAsync(id);
        }

        public async Task<Employee?> GetByUsernameAsync(string username)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.Username == username);
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return employee;
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasRelatedRecordsAsync(long id)
        {
            // Check established tables from the plan
            bool hasSessions = await _context.CashSessions.AnyAsync(s => s.EmployeeId == id);
            bool hasInvoices = await _context.Invoices.AnyAsync(i => i.OriginalEmployeeId == id);
            bool hasAdjustmentRequests = await _context.AdjustmentRequests.AnyAsync(r => r.RequestedByEmployeeId == id || r.ReviewedByEmployeeId == id);
            bool hasProductBatches = await _context.ProductBatches.AnyAsync(b => b.EnteredByEmployeeId == id);

            return hasSessions || hasInvoices || hasAdjustmentRequests || hasProductBatches;
        }
    }
}
