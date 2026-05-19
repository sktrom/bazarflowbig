using System.Threading.Tasks;
using Supermarket.Contracts.Suppliers;

namespace Supermarket.Application.Suppliers.Interfaces
{
    public interface ISupplierService
    {
        Task<SupplierListResponse> GetAllAsync();
        Task<SupplierDetailResponse> GetByIdAsync(long id);
        Task<SupplierDetailResponse> CreateAsync(CreateSupplierRequest request);
        Task<SupplierDetailResponse> UpdateAsync(long id, UpdateSupplierRequest request);
        Task<DeleteSupplierResponse> DeleteAsync(long id);
    }
}
