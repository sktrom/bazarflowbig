using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.InventoryQueries.Interfaces;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class InventoryQueryRepository : IInventoryQueryRepository
    {
        private readonly SupermarketDbContext _context;

        public InventoryQueryRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        public async Task<(List<(Product Product, decimal TotalQuantityAvailable, int BatchCount, DateTime? NearestExpiryDate)> Items, int TotalCount)> GetInventoryPaginatedAsync(
            string? search, long? categoryId, bool? isActive, bool? hasStock, bool? hasExpiry, int page, int pageSize)
        {
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || p.Barcode.Contains(s));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            if (hasExpiry.HasValue)
                query = query.Where(p => p.HasExpiry == hasExpiry.Value);

            // Create the projection
            var projectedQuery = query.Select(p => new
            {
                Product = p,
                TotalQty = _context.ProductBatches.Where(b => b.ProductId == p.Id).Sum(b => (decimal?)b.QuantityAvailable) ?? 0m,
                BatchCount = _context.ProductBatches.Count(b => b.ProductId == p.Id && b.QuantityAvailable > 0),
                NearestExpiry = _context.ProductBatches.Where(b => b.ProductId == p.Id && b.QuantityAvailable > 0 && b.ExpiryDate != null).Min(b => b.ExpiryDate)
            });

            if (hasStock.HasValue)
            {
                if (hasStock.Value)
                    projectedQuery = projectedQuery.Where(p => p.TotalQty > 0);
                else
                    projectedQuery = projectedQuery.Where(p => p.TotalQty == 0);
            }

            var totalCount = await projectedQuery.CountAsync();

            var rawItems = await projectedQuery
                .OrderBy(p => p.Product.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawItems.Select(p => (p.Product, p.TotalQty, p.BatchCount, p.NearestExpiry)).ToList();

            return (items, totalCount);
        }

        public async Task<Product?> GetProductByIdAsync(long productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<List<ProductBatch>> GetProductBatchesAsync(long productId)
        {
            // FEFO ordering: nearest ExpiryDate first, nulls last
            return await _context.ProductBatches
                .Where(b => b.ProductId == productId)
                .OrderByDescending(b => b.ExpiryDate.HasValue) // true first, false last. In C#, false is 0, true is 1. So OrderByDescending puts true (has value) first
                .ThenBy(b => b.ExpiryDate) // nearest expiry first
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<(Product Product, decimal TotalQuantityAvailable)>> GetProductsWithStockLevelsAsync()
        {
            var query = _context.Products
                .AsNoTracking()
                .Select(p => new
                {
                    Product = p,
                    TotalQty = _context.ProductBatches.Where(b => b.ProductId == p.Id).Sum(b => (decimal?)b.QuantityAvailable) ?? 0m
                });

            var result = await query.ToListAsync();
            return result.Select(x => (x.Product, x.TotalQty)).ToList();
        }

        public async Task<List<(ProductBatch Batch, Product Product)>> GetBatchesWithExpiryAsync()
        {
            var query = _context.ProductBatches
                .Include(b => b.Product)
                .Where(b => b.QuantityAvailable > 0 && b.ExpiryDate != null)
                .AsNoTracking();

            var result = await query.ToListAsync();
            return result.Select(b => (b, b.Product!)).ToList();
        }

        public async Task<List<long>> GetProductsWithZeroSalesLast30DaysAsync()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            // Products with stock > 0 but NO sales in the last 30 days
            var query = _context.Products
                .Where(p => _context.ProductBatches.Where(b => b.ProductId == p.Id).Sum(b => (decimal?)b.QuantityAvailable) > 0)
                .Where(p => !_context.InvoiceLines.Any(il => il.ProductId == p.Id && il.Invoice != null && il.Invoice.CreatedAt >= thirtyDaysAgo))
                .Select(p => p.Id);

            return await query.ToListAsync();
        }

        public async Task<Dictionary<long, decimal>> GetSoldQuantitiesLast30DaysAsync(DateTime fromUtc)
        {
            return await _context.InvoiceLines
                .Where(il => il.Invoice != null &&
                    il.Invoice.CreatedAt >= fromUtc &&
                    il.Invoice.Status != InvoiceStatus.Working &&
                    il.Invoice.Status != InvoiceStatus.Suspended &&
                    il.Invoice.Status != InvoiceStatus.Cancelled)
                .GroupBy(il => il.ProductId)
                .Select(g => new { ProductId = g.Key, SoldQuantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.SoldQuantity);
        }
    }
}
