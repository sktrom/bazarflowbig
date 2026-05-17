using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Products
{
    public class ProductListItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductListResponse
    {
        public List<ProductListItem> Items { get; set; } = new();
    }

    public class ProductDetailResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool HasCarton { get; set; }
        public int? CartonQuantity { get; set; }
        public decimal? CartonPriceUsd { get; set; }
        public bool HasExpiry { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool HasCarton { get; set; }
        public int? CartonQuantity { get; set; }
        public decimal? CartonPriceUsd { get; set; }
        public bool HasExpiry { get; set; }
    }

    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public bool HasCarton { get; set; }
        public int? CartonQuantity { get; set; }
        public decimal? CartonPriceUsd { get; set; }
        public bool HasExpiry { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeleteProductResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; } = string.Empty; // "DELETED" | "DISABLED"
        public string Message { get; set; } = string.Empty;
    }
}
