using System.Threading.Tasks;
using Supermarket.Contracts.Products;

namespace Supermarket.Application.Products.Interfaces
{
    public interface IProductService
    {
        Task<ProductListResponse> GetAllAsync();
        Task<ProductDetailResponse> GetByIdAsync(long id);
        Task<ProductDetailResponse> CreateAsync(CreateProductRequest request);
        Task<ProductDetailResponse> UpdateAsync(long id, UpdateProductRequest request);
        Task<DeleteProductResponse> DeleteAsync(long id);
    }
}
