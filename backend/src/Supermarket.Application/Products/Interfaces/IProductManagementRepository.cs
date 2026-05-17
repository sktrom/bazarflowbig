using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Products.Interfaces
{
    public interface IProductManagementRepository
    {
        Task<IReadOnlyList<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(long id);
        Task<Product?> GetByBarcodeAsync(string barcode);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(long id);
        Task<bool> HasRelatedRecordsAsync(long id);
        Task<bool> CategoryExistsAsync(long categoryId);
    }
}
