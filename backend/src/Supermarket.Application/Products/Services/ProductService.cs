using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Contracts.Products;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Products.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductManagementRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public ProductService(IProductManagementRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<ProductListResponse> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();
            return new ProductListResponse
            {
                Items = products.Select(p => new ProductListItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    PriceUsd = p.PriceUsd,
                    IsActive = p.IsActive
                }).ToList()
            };
        }

        public async Task<ProductDetailResponse> GetByIdAsync(long id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            return MapToDetailResponse(product);
        }

        public async Task<ProductDetailResponse> CreateAsync(CreateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Barcode) || string.IsNullOrWhiteSpace(request.BaseUnit))
                throw new InvalidOperationException("VALIDATION_ERROR");

            if (!await _repository.CategoryExistsAsync(request.CategoryId))
                throw new InvalidOperationException("CATEGORY_NOT_FOUND");

            var existing = await _repository.GetByBarcodeAsync(request.Barcode.Trim());
            if (existing != null)
                throw new InvalidOperationException("BARCODE_ALREADY_EXISTS");

            var product = new Product
            {
                Name = request.Name.Trim(),
                Barcode = request.Barcode.Trim(),
                CategoryId = request.CategoryId,
                BaseUnit = request.BaseUnit.Trim(),
                PriceUsd = request.PriceUsd,
                HasCarton = request.HasCarton,
                CartonQuantity = request.CartonQuantity,
                CartonPriceUsd = request.CartonPriceUsd,
                HasExpiry = request.HasExpiry,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(product);

            // Re-fetch to get Category navigation property populated if needed, or simply map
            var fetched = await _repository.GetByIdAsync(created.Id);
            await RecordAuditAsync("PRODUCT_CREATE", fetched ?? created);
            return MapToDetailResponse(fetched ?? created);
        }

        public async Task<ProductDetailResponse> UpdateAsync(long id, UpdateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Barcode) || string.IsNullOrWhiteSpace(request.BaseUnit))
                throw new InvalidOperationException("VALIDATION_ERROR");

            var product = await _repository.GetByIdAsync(id);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            var newBarcodeTrimmed = request.Barcode.Trim();
            
            if (!product.Barcode.Equals(newBarcodeTrimmed, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _repository.GetByBarcodeAsync(newBarcodeTrimmed);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException("BARCODE_ALREADY_EXISTS");
                    
                product.Barcode = newBarcodeTrimmed;
            }

            if (product.CategoryId != request.CategoryId)
            {
                if (!await _repository.CategoryExistsAsync(request.CategoryId))
                    throw new InvalidOperationException("CATEGORY_NOT_FOUND");
                    
                product.CategoryId = request.CategoryId;
            }

            product.Name = request.Name.Trim();
            product.BaseUnit = request.BaseUnit.Trim();
            product.PriceUsd = request.PriceUsd;
            product.HasCarton = request.HasCarton;
            product.CartonQuantity = request.CartonQuantity;
            product.CartonPriceUsd = request.CartonPriceUsd;
            product.HasExpiry = request.HasExpiry;
            product.IsActive = request.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(product);

            var fetched = await _repository.GetByIdAsync(product.Id);
            await RecordAuditAsync("PRODUCT_UPDATE", fetched ?? product);
            return MapToDetailResponse(fetched ?? product);
        }

        public async Task<DeleteProductResponse> DeleteAsync(long id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            bool hasRelated = await _repository.HasRelatedRecordsAsync(id);

            if (hasRelated)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(product);
                await RecordAuditAsync("PRODUCT_DEACTIVATE", product);
                return new DeleteProductResponse 
                { 
                    Success = true, 
                    Action = "DISABLED", 
                    Message = "Product has related records and was disabled instead of deleted." 
                };
            }
            else
            {
                await _repository.DeleteAsync(id);
                await RecordAuditAsync("PRODUCT_DELETE", product);
                return new DeleteProductResponse 
                { 
                    Success = true, 
                    Action = "DELETED", 
                    Message = "Product deleted successfully." 
                };
            }
        }

        private static ProductDetailResponse MapToDetailResponse(Product p)
        {
            return new ProductDetailResponse
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                CategoryId = p.CategoryId,
                BaseUnit = p.BaseUnit,
                PriceUsd = p.PriceUsd,
                HasCarton = p.HasCarton,
                CartonQuantity = p.CartonQuantity,
                CartonPriceUsd = p.CartonPriceUsd,
                HasExpiry = p.HasExpiry,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

        private async Task RecordAuditAsync(string action, Product product)
        {
            try
            {
                await _auditLogService.RecordAsync(
                    action,
                    "Product",
                    product.Id.ToString(),
                    product.Name,
                    metadata: new
                    {
                        productId = product.Id,
                        product.Name,
                        product.Barcode,
                        product.CategoryId,
                        product.PriceUsd,
                        product.IsActive
                    });
            }
            catch
            {
                // Audit logging is best-effort and must not break product operations.
            }
        }
    }
}
