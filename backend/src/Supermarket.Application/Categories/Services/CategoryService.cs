using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Categories.Interfaces;
using Supermarket.Contracts.Categories;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Categories.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<CategoryListResponse> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            return new CategoryListResponse
            {
                Items = categories.Select(c => new CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive
                }).ToList()
            };
        }

        public async Task<CategoryItem> CreateAsync(CreateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("VALIDATION_ERROR");

            var existing = await _repository.GetByNameAsync(request.Name.Trim());
            if (existing != null)
                throw new InvalidOperationException("CATEGORY_NAME_ALREADY_EXISTS");

            var category = new Category
            {
                Name = request.Name.Trim(),
                IsActive = true
            };

            var created = await _repository.CreateAsync(category);

            return new CategoryItem
            {
                Id = created.Id,
                Name = created.Name,
                IsActive = created.IsActive
            };
        }

        public async Task<CategoryItem> UpdateAsync(long id, UpdateCategoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("VALIDATION_ERROR");

            var category = await _repository.GetByIdAsync(id);
            if (category == null) throw new InvalidOperationException("CATEGORY_NOT_FOUND");

            var newNameTrimmed = request.Name.Trim();
            
            // Check if name has changed and if it conflicts
            if (!category.Name.Equals(newNameTrimmed, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _repository.GetByNameAsync(newNameTrimmed);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException("CATEGORY_NAME_ALREADY_EXISTS");
                    
                category.Name = newNameTrimmed;
            }

            category.IsActive = request.IsActive;

            await _repository.UpdateAsync(category);

            return new CategoryItem
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            };
        }

        public async Task<DeleteCategoryResponse> DeleteAsync(long id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) throw new InvalidOperationException("CATEGORY_NOT_FOUND");

            bool hasProducts = await _repository.HasProductsAsync(id);

            if (hasProducts)
            {
                category.IsActive = false;
                await _repository.UpdateAsync(category);
                return new DeleteCategoryResponse 
                { 
                    Success = true, 
                    Action = "DISABLED", 
                    Message = "Category has linked products and was disabled instead of deleted." 
                };
            }
            else
            {
                await _repository.DeleteAsync(id);
                return new DeleteCategoryResponse 
                { 
                    Success = true, 
                    Action = "DELETED", 
                    Message = "Category deleted successfully." 
                };
            }
        }
    }
}
