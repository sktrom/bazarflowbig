using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Contracts.InventoryQueries;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.InventoryQueries.Services
{
    public class ActionCenterService : IActionCenterService
    {
        private readonly IInventoryQueryRepository _repository;
        private readonly IAppSettingsRepository _appSettingsRepository;

        public ActionCenterService(IInventoryQueryRepository repository, IAppSettingsRepository appSettingsRepository)
        {
            _repository = repository;
            _appSettingsRepository = appSettingsRepository;
        }

        public async Task<ActionCenterResponseDto> GetActionCenterSummaryAsync()
        {
            var response = new ActionCenterResponseDto();
            
            var stockThreshold = await _appSettingsRepository.GetRequiredDecimalAsync("stock_alert_threshold");
            var expiryAlertDays = await _appSettingsRepository.GetRequiredDecimalAsync("expiry_alert_days");

            var productsWithStock = await _repository.GetProductsWithStockLevelsAsync();
            var batchesWithExpiry = await _repository.GetBatchesWithExpiryAsync();
            var slowMovingProductIds = await _repository.GetProductsWithZeroSalesLast30DaysAsync();

            var today = DateTime.UtcNow.Date;
            var expiryThresholdDate = today.AddDays((double)expiryAlertDays);
            var soldQuantitiesLast30Days = await _repository.GetSoldQuantitiesLast30DaysAsync(DateTime.UtcNow.AddDays(-30));

            // Populate Details
            foreach (var item in productsWithStock)
            {
                var p = item.Product;
                var stock = item.TotalQuantityAvailable;

                if (p.IsActive)
                {
                    if (stock == 0)
                    {
                        response.OutOfStock.Add(new ProductActionItemDto
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Barcode = p.Barcode,
                            CurrentStock = stock
                        });
                    }
                    else if (stock > 0 && stock <= stockThreshold)
                    {
                        response.LowStock.Add(new ProductActionItemDto
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Barcode = p.Barcode,
                            CurrentStock = stock
                        });
                    }

                    var soldLast30Days = soldQuantitiesLast30Days.TryGetValue(p.Id, out var soldQty) ? soldQty : 0m;
                    var restockSuggestion = BuildRestockSuggestion(p, stock, soldLast30Days);
                    if (restockSuggestion != null)
                    {
                        response.RestockSuggestions.Add(restockSuggestion);
                    }
                }
                else
                {
                    if (stock > 0)
                    {
                        response.InactiveWithStock.Add(new ProductActionItemDto
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Barcode = p.Barcode,
                            CurrentStock = stock
                        });
                    }
                }
            }

            var expiringSoonProductIds = new HashSet<long>();

            foreach (var item in batchesWithExpiry)
            {
                var b = item.Batch;
                var p = item.Product;

                if (b.ExpiryDate.HasValue)
                {
                    var expiryDate = b.ExpiryDate.Value.Date;

                    if (expiryDate < today)
                    {
                        response.Expired.Add(new BatchActionItemDto
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Barcode = p.Barcode,
                            CurrentStock = b.QuantityAvailable,
                            BatchId = b.Id,
                            ExpiryDate = b.ExpiryDate
                        });
                    }
                    else if (expiryDate <= expiryThresholdDate)
                    {
                        response.ExpiringSoon.Add(new BatchActionItemDto
                        {
                            ProductId = p.Id,
                            ProductName = p.Name,
                            Barcode = p.Barcode,
                            CurrentStock = b.QuantityAvailable,
                            BatchId = b.Id,
                            ExpiryDate = b.ExpiryDate
                        });
                        expiringSoonProductIds.Add(p.Id);
                    }
                }
            }

            // Offer Candidates: Expiring Soon OR Slow Moving (with stock)
            var offerCandidateIds = new HashSet<long>(expiringSoonProductIds);
            foreach (var id in slowMovingProductIds)
            {
                offerCandidateIds.Add(id);
            }

            foreach (var id in offerCandidateIds)
            {
                var pInfo = productsWithStock.FirstOrDefault(x => x.Product.Id == id);
                if (pInfo.Product != null && pInfo.TotalQuantityAvailable > 0)
                {
                    response.OfferCandidates.Add(new ProductActionItemDto
                    {
                        ProductId = pInfo.Product.Id,
                        ProductName = pInfo.Product.Name,
                        Barcode = pInfo.Product.Barcode,
                        CurrentStock = pInfo.TotalQuantityAvailable
                    });
                }
            }

            // Update Summaries
            response.Summary.OutOfStockCount = response.OutOfStock.Count;
            response.Summary.LowStockCount = response.LowStock.Count;
            response.Summary.ExpiredBatchesCount = response.Expired.Count;
            response.Summary.ExpiringSoonBatchesCount = response.ExpiringSoon.Count;
            response.Summary.InactiveWithStockCount = response.InactiveWithStock.Count;
            response.Summary.OfferCandidatesCount = response.OfferCandidates.Count;
            response.Summary.RestockSuggestionsCount = response.RestockSuggestions.Count;

            // Generate Top Urgent Actions
            var allUrgentActions = new List<TopUrgentActionDto>();

            // HIGH: Expired
            foreach (var item in response.Expired)
            {
                allUrgentActions.Add(new TopUrgentActionDto
                {
                    Type = "EXPIRED",
                    Severity = "HIGH",
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Message = $"انتهت صلاحية تشغيلة (الكمية: {item.CurrentStock})",
                    RecommendedAction = "إعدام فوراً"
                });
            }

            // HIGH: OutOfStock
            foreach (var item in response.OutOfStock)
            {
                allUrgentActions.Add(new TopUrgentActionDto
                {
                    Type = "OUT_OF_STOCK",
                    Severity = "HIGH",
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Message = "نفذت الكمية بالكامل",
                    RecommendedAction = "إعادة طلب فورية"
                });
            }

            // HIGH: Restock Buy Now
            foreach (var item in response.RestockSuggestions.Where(x => x.RecommendationType == "BuyNow"))
            {
                allUrgentActions.Add(new TopUrgentActionDto
                {
                    Type = "RESTOCK_BUY_NOW",
                    Severity = "HIGH",
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Message = $"اقتراح إعادة طلب فورية (الكمية المقترحة: {item.SuggestedQty})",
                    RecommendedAction = "إعادة طلب فورية"
                });
            }

            // MEDIUM: Expiring Soon
            foreach (var item in response.ExpiringSoon)
            {
                allUrgentActions.Add(new TopUrgentActionDto
                {
                    Type = "EXPIRING_SOON",
                    Severity = "MEDIUM",
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Message = $"تشغيلة تنتهي قريباً (الكمية: {item.CurrentStock})",
                    RecommendedAction = "عمل عرض ترويجي"
                });
            }

            // MEDIUM: Low Stock
            foreach (var item in response.LowStock)
            {
                allUrgentActions.Add(new TopUrgentActionDto
                {
                    Type = "LOW_STOCK",
                    Severity = "MEDIUM",
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Barcode = item.Barcode,
                    Message = $"المخزون منخفض (الكمية: {item.CurrentStock})",
                    RecommendedAction = "إعادة طلب"
                });
            }

            // Sort HIGH then MEDIUM, and take 10
            response.TopUrgentActions = allUrgentActions
                .OrderBy(a => a.Severity == "HIGH" ? 0 : 1)
                .Take(10)
                .ToList();

            return response;
        }

        private static RestockSuggestionDto? BuildRestockSuggestion(Product product, decimal currentStock, decimal soldLast30Days)
        {
            var avgDailySales = soldLast30Days / 30m;
            decimal? daysRemaining = null;
            var suggestedQty = 0m;
            var confidence = soldLast30Days >= 10m ? "High" : soldLast30Days > 0m ? "Medium" : "Low";
            string? recommendationType = null;

            if (avgDailySales > 0m)
            {
                daysRemaining = currentStock / avgDailySales;
                suggestedQty = Math.Ceiling(Math.Max(0m, (avgDailySales * 14m) - currentStock));
            }

            if (currentStock == 0m && soldLast30Days > 0m)
            {
                recommendationType = "BuyNow";
            }
            else if (avgDailySales > 0m && daysRemaining.HasValue && daysRemaining.Value <= 3m)
            {
                recommendationType = "BuyNow";
            }
            else if (avgDailySales > 0m && daysRemaining.HasValue && daysRemaining.Value <= 7m)
            {
                recommendationType = "Watch";
            }
            else if (currentStock > 0m && soldLast30Days == 0m)
            {
                recommendationType = "SlowMoving";
            }
            else if (currentStock == 0m && soldLast30Days == 0m)
            {
                recommendationType = "LowConfidence";
            }

            if (recommendationType == null)
            {
                return null;
            }

            return new RestockSuggestionDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                CurrentStock = currentStock,
                SoldLast30Days = soldLast30Days,
                AvgDailySales = avgDailySales,
                DaysRemaining = daysRemaining,
                SuggestedQty = suggestedQty,
                Confidence = confidence,
                RecommendationType = recommendationType,
                RecommendedAction = recommendationType switch
                {
                    "BuyNow" => "شراء الآن",
                    "Watch" => "راقب المخزون",
                    "SlowMoving" => "بطيء الحركة",
                    _ => "بيانات غير كافية"
                }
            };
        }
    }
}
