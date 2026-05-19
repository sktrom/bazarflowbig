using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Suppliers.Interfaces
{
    public interface ISupplierRepository
    {
        Task<IReadOnlyList<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(long id);
        Task<bool> ActiveNameExistsAsync(string name, long? excludeId = null);
        Task<Supplier> CreateAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(Supplier supplier);
        Task<bool> IsSupplierUsedAsync(long id);
    }
}
