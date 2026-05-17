using System.Threading.Tasks;
using Supermarket.Contracts.Categories;

namespace Supermarket.Application.Categories.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryListResponse> GetAllAsync();
        Task<CategoryItem> CreateAsync(CreateCategoryRequest request);
        Task<CategoryItem> UpdateAsync(long id, UpdateCategoryRequest request);
        Task<DeleteCategoryResponse> DeleteAsync(long id);
    }
}
