using System.Collections.Generic;

namespace Supermarket.Contracts.Categories
{
    public class CategoryItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CategoryListResponse
    {
        public List<CategoryItem> Items { get; set; } = new();
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DeleteCategoryResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; } = string.Empty; // "DELETED" | "DISABLED"
        public string Message { get; set; } = string.Empty;
    }
}
