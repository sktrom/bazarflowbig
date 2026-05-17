using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Contracts.InventoryQueries;

namespace Supermarket.Application.InventoryQueries.Services
{
    public class InventoryQueryService : IInventoryQueryService
    {
        private readonly IInventoryQueryRepository _repository;
        private readonly IAppSettingsRepository _appSettingsRepository;

        public InventoryQueryService(IInventoryQueryRepository repository, IAppSettingsRepository appSettingsRepository)
        {
            _repository = repository;
            _appSettingsRepository = appSettingsRepository;
        }

        public async Task<InventoryListResponse> GetInventoryListAsync(
            string? search, long? categoryId, bool? isActive, bool? hasStock, bool? hasExpiry, int page, int pageSize)
        {
            var (items, totalCount) = await _repository.GetInventoryPaginatedAsync(
                search, categoryId, isActive, hasStock, hasExpiry, page, pageSize);

            var stockThreshold = await _appSettingsRepository.GetRequiredDecimalAsync("stock_alert_threshold");
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");

            var response = new InventoryListResponse
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            foreach (var item in items)
            {
                var p = item.Product;

                response.Items.Add(new InventoryListItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Barcode = p.Barcode,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    BaseUnit = p.BaseUnit,
                    PriceUsd = p.PriceUsd,
                    HasCarton = p.HasCarton,
                    CartonQuantity = p.CartonQuantity,
                    CartonPriceUsd = p.CartonPriceUsd,
                    HasExpiry = p.HasExpiry,
                    IsActive = p.IsActive,
                    TotalQuantityAvailable = item.TotalQuantityAvailable,
                    BatchCount = item.BatchCount,
                    NearestExpiryDate = item.NearestExpiryDate,
                    StockStatus = GetStockStatus(item.TotalQuantityAvailable, stockThreshold),
                    ExpiryStatus = GetExpiryStatus(p.HasExpiry, item.NearestExpiryDate, expiryAlertDays)
                });
            }

            return response;
        }

        public async Task<InventoryDetailsResponse> GetInventoryDetailsAsync(long productId)
        {
            var product = await _repository.GetProductByIdAsync(productId);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            var batches = await _repository.GetProductBatchesAsync(productId);

            var stockThreshold = await _appSettingsRepository.GetRequiredDecimalAsync("stock_alert_threshold");
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");

            decimal totalQuantity = batches.Sum(b => b.QuantityAvailable);
            DateTime? nearestExpiry = batches.Where(b => b.QuantityAvailable > 0 && b.ExpiryDate.HasValue)
                                             .Min(b => b.ExpiryDate);

            var response = new InventoryDetailsResponse
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                BaseUnit = product.BaseUnit,
                PriceUsd = product.PriceUsd,
                HasCarton = product.HasCarton,
                CartonQuantity = product.CartonQuantity,
                CartonPriceUsd = product.CartonPriceUsd,
                HasExpiry = product.HasExpiry,
                IsActive = product.IsActive,
                TotalQuantityAvailable = totalQuantity,
                StockStatus = GetStockStatus(totalQuantity, stockThreshold),
                ExpiryStatus = GetExpiryStatus(product.HasExpiry, nearestExpiry, expiryAlertDays)
            };

            var now = DateTime.UtcNow;

            foreach (var batch in batches)
            {
                int? daysUntilExpiry = null;
                if (batch.ExpiryDate.HasValue)
                {
                    daysUntilExpiry = (int)(batch.ExpiryDate.Value.Date - now.Date).TotalDays;
                }

                response.Batches.Add(new InventoryBatchDto
                {
                    BatchId = batch.Id,
                    QuantityReceived = batch.QuantityReceived,
                    QuantityAvailable = batch.QuantityAvailable,
                    EntryDate = batch.EntryDate,
                    ExpiryDate = batch.ExpiryDate,
                    EntryInvoiceNumber = batch.EntryInvoiceNumber,
                    EnteredByEmployeeId = batch.EnteredByEmployeeId,
                    DaysUntilExpiry = daysUntilExpiry,
                    ExpiryStatus = GetExpiryStatus(product.HasExpiry, batch.ExpiryDate, expiryAlertDays)
                });
            }

            return response;
        }

        private string GetStockStatus(decimal quantity, decimal threshold)
        {
            if (quantity == 0) return "OutOfStock";
            if (quantity <= threshold) return "LowStock";
            return "InStock";
        }

        private string? GetExpiryStatus(bool hasExpiry, DateTime? expiryDate, decimal expiryAlertDays)
        {
            if (!hasExpiry || !expiryDate.HasValue) return null;

            var now = DateTime.UtcNow;
            var thresholdDate = now.AddDays((double)expiryAlertDays);

            if (expiryDate.Value < now) return "Expired";
            if (expiryDate.Value <= thresholdDate) return "ExpiringSoon";
            return "Fresh";
        }
    }
}
