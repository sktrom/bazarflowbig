using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.Reports.Interfaces;
using Supermarket.Contracts.Reports;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Infrastructure.Repositories
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly SupermarketDbContext _context;

        public ReportsRepository(SupermarketDbContext context)
        {
            _context = context;
        }

        // Sales

        public async Task<List<SalesInvoiceReportDto>> GetSalesInvoicesAsync(DateTime? dateFrom, DateTime? dateTo, string? status)
        {
            var query = _context.Invoices.Include(i => i.OriginalEmployee).AsNoTracking().AsQueryable();

            if (dateFrom.HasValue) query = query.Where(i => i.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(i => i.CreatedAt <= dateTo.Value);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<InvoiceStatus>(status, out var invoiceStatus))
                {
                    query = query.Where(i => i.Status == invoiceStatus);
                }
                else
                {
                    // Invalid status filter requested, returns empty naturally
                    return new List<SalesInvoiceReportDto>();
                }
            }

            return await query.Select(i => new SalesInvoiceReportDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CreatedAt = i.CreatedAt,
                Status = i.Status.ToString(),
                TotalUsd = i.TotalUsd,
                TotalSyp = i.TotalSyp,
                EmployeeName = i.OriginalEmployee != null ? i.OriginalEmployee.FullName : string.Empty
            }).ToListAsync();
        }

        public async Task<List<SalesItemReportDto>> GetSalesItemsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.InvoiceLines
                .Include(il => il.Invoice)
                .Include(il => il.Product)
                .Where(il => il.Invoice != null && il.Invoice.Status != InvoiceStatus.Working && il.Invoice.Status != InvoiceStatus.Suspended && il.Invoice.Status != InvoiceStatus.Cancelled)
                .AsNoTracking()
                .AsQueryable();

            if (dateFrom.HasValue) query = query.Where(il => il.Invoice!.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(il => il.Invoice!.CreatedAt <= dateTo.Value);

            var grouped = await query.GroupBy(il => new { il.ProductId, il.Product!.Name })
                .Select(g => new SalesItemReportDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenueUsd = g.Sum(x => x.LineTotalUsdEffective)
                }).ToListAsync();

            return grouped;
        }

        public async Task<List<SalesChartDto>> GetSalesChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.Invoices
                .Where(i => i.Status != InvoiceStatus.Working && i.Status != InvoiceStatus.Suspended && i.Status != InvoiceStatus.Cancelled)
                .AsNoTracking()
                .AsQueryable();

            if (dateFrom.HasValue) query = query.Where(i => i.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(i => i.CreatedAt <= dateTo.Value);

            // Group by Date part (simplified for typical charts)
            var list = await query.ToListAsync();
            
            var grouped = list.GroupBy(i => i.CreatedAt.Date)
                .Select(g => new SalesChartDto
                {
                    DateLabel = g.Key.ToString("yyyy-MM-dd"),
                    RevenueUsd = g.Sum(x => x.TotalUsd)
                })
                .OrderBy(x => x.DateLabel)
                .ToList();

            return grouped;
        }

        // Products

        public async Task<List<ProductSummaryReportDto>> GetProductsSummaryAsync(long? categoryId)
        {
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = await query.ToListAsync();
            var productIds = products.Select(p => p.Id).ToList();

            var batchTotals = await _context.ProductBatches
                .Where(b => productIds.Contains(b.ProductId))
                .GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty);

            return products.Select(p =>
            {
                var qty = batchTotals.TryGetValue(p.Id, out var val) ? val : 0;
                return new ProductSummaryReportDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    IsActive = p.IsActive,
                    TotalStockQuantity = qty,
                    TotalStockValueUsd = qty * p.PriceUsd
                };
            }).ToList();
        }

        public async Task<List<ProductMovementReportDto>> GetProductsMovementsAsync(DateTime? dateFrom, DateTime? dateTo, long? productId)
        {
            var query = _context.InvoiceLines
                .Include(il => il.Invoice)
                .Include(il => il.Product)
                .Where(il => il.Invoice != null && il.Invoice.Status != InvoiceStatus.Working && il.Invoice.Status != InvoiceStatus.Suspended && il.Invoice.Status != InvoiceStatus.Cancelled)
                .AsNoTracking()
                .AsQueryable();

            if (productId.HasValue) query = query.Where(il => il.ProductId == productId.Value);
            if (dateFrom.HasValue) query = query.Where(il => il.Invoice!.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(il => il.Invoice!.CreatedAt <= dateTo.Value);

            var lines = await query.ToListAsync();

            return lines.Select(il => new ProductMovementReportDto
            {
                ProductId = il.ProductId,
                ProductName = il.Product?.Name ?? string.Empty,
                MovementDate = il.Invoice!.CreatedAt,
                MovementType = "Sale",
                Quantity = il.Quantity,
                ReferenceNumber = il.Invoice.InvoiceNumber
            }).OrderByDescending(m => m.MovementDate).ToList();
        }

        public async Task<List<ProductChartDto>> GetProductsChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.InvoiceLines
                .Include(il => il.Invoice)
                .Include(il => il.Product)
                .Where(il => il.Invoice != null && il.Invoice.Status != InvoiceStatus.Working && il.Invoice.Status != InvoiceStatus.Suspended && il.Invoice.Status != InvoiceStatus.Cancelled)
                .AsNoTracking()
                .AsQueryable();

            if (dateFrom.HasValue) query = query.Where(il => il.Invoice!.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(il => il.Invoice!.CreatedAt <= dateTo.Value);

            var grouped = await query.GroupBy(il => new { il.ProductId, il.Product!.Name })
                .Select(g => new ProductChartDto
                {
                    ProductName = g.Key.Name,
                    TotalSalesRevenueUsd = g.Sum(x => x.LineTotalUsdEffective)
                })
                .OrderByDescending(x => x.TotalSalesRevenueUsd)
                .Take(10) // Top 10 for chart
                .ToListAsync();

            return grouped;
        }

        // Employees

        public async Task<List<EmployeeSummaryReportDto>> GetEmployeesSummaryAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var empQuery = _context.Employees.AsNoTracking();
            var invQuery = _context.Invoices
                .Where(i => i.Status != InvoiceStatus.Working && i.Status != InvoiceStatus.Suspended && i.Status != InvoiceStatus.Cancelled)
                .AsNoTracking()
                .AsQueryable();

            if (dateFrom.HasValue) invQuery = invQuery.Where(i => i.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) invQuery = invQuery.Where(i => i.CreatedAt <= dateTo.Value);

            var employees = await empQuery.ToListAsync();
            var salesData = await invQuery.GroupBy(i => i.OriginalEmployeeId)
                .Select(g => new { EmployeeId = g.Key, InvoiceCount = g.Count(), TotalUsd = g.Sum(x => x.TotalUsd) })
                .ToDictionaryAsync(x => x.EmployeeId);

            return employees.Select(e =>
            {
                var data = salesData.TryGetValue(e.Id, out var val) ? val : null;
                return new EmployeeSummaryReportDto
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.FullName,
                    TotalInvoicesHandled = data?.InvoiceCount ?? 0,
                    TotalSalesRevenueUsd = data?.TotalUsd ?? 0
                };
            }).ToList();
        }

        public async Task<List<EmployeeActivityReportDto>> GetEmployeesActivityAsync(DateTime? dateFrom, DateTime? dateTo, long? employeeId)
        {
            var sessionsQuery = _context.CashSessions.Include(s => s.Employee).AsNoTracking().AsQueryable();
            var invoicesQuery = _context.Invoices.Include(i => i.OriginalEmployee).Where(i => i.Status != InvoiceStatus.Working && i.Status != InvoiceStatus.Suspended).AsNoTracking().AsQueryable();

            if (employeeId.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.EmployeeId == employeeId.Value);
                invoicesQuery = invoicesQuery.Where(i => i.OriginalEmployeeId == employeeId.Value);
            }

            if (dateFrom.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.StartedAt >= dateFrom.Value);
                invoicesQuery = invoicesQuery.Where(i => i.CreatedAt >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.StartedAt <= dateTo.Value);
                invoicesQuery = invoicesQuery.Where(i => i.CreatedAt <= dateTo.Value);
            }

            var activities = new List<EmployeeActivityReportDto>();

            var sessions = await sessionsQuery.ToListAsync();
            foreach (var s in sessions)
            {
                activities.Add(new EmployeeActivityReportDto
                {
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.Employee?.FullName ?? string.Empty,
                    ActivityDate = s.StartedAt,
                    ActivityType = "SessionOpened",
                    Details = $"Session {s.Id} opened."
                });
                if (s.EndedAt.HasValue)
                {
                    activities.Add(new EmployeeActivityReportDto
                    {
                        EmployeeId = s.EmployeeId,
                        EmployeeName = s.Employee?.FullName ?? string.Empty,
                        ActivityDate = s.EndedAt.Value,
                        ActivityType = "SessionClosed",
                        Details = $"Session {s.Id} closed."
                    });
                }
            }

            var invoices = await invoicesQuery.ToListAsync();
            foreach (var i in invoices)
            {
                activities.Add(new EmployeeActivityReportDto
                {
                    EmployeeId = i.OriginalEmployeeId,
                    EmployeeName = i.OriginalEmployee?.FullName ?? string.Empty,
                    ActivityDate = i.CreatedAt,
                    ActivityType = "InvoiceProcessed",
                    Details = $"Invoice {i.InvoiceNumber} processed. Status: {i.Status}"
                });
            }

            return activities.OrderByDescending(a => a.ActivityDate).ToList();
        }

        public async Task<List<EmployeeChartDto>> GetEmployeesChartsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var summary = await GetEmployeesSummaryAsync(dateFrom, dateTo);
            return summary.Select(s => new EmployeeChartDto
            {
                EmployeeName = s.EmployeeName,
                TotalSalesRevenueUsd = s.TotalSalesRevenueUsd
            }).OrderByDescending(x => x.TotalSalesRevenueUsd).ToList();
        }

        // Inventory

        public async Task<List<InventorySummaryReportDto>> GetInventorySummaryAsync(long? categoryId, decimal stockAlertThreshold)
        {
            var query = _context.Products.Include(p => p.Category).AsNoTracking().AsQueryable();
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = await query.ToListAsync();
            var productIds = products.Select(p => p.Id).ToList();

            var batchTotals = await _context.ProductBatches
                .Where(b => productIds.Contains(b.ProductId))
                .GroupBy(b => b.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty);

            return products.Select(p =>
            {
                var qty = batchTotals.TryGetValue(p.Id, out var val) ? val : 0;
                string status = qty == 0 ? "OutOfStock" : (qty <= stockAlertThreshold ? "LowStock" : "InStock");
                
                return new InventorySummaryReportDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    TotalQuantityAvailable = qty,
                    TotalStockValueUsd = qty * p.PriceUsd,
                    StockStatus = status
                };
            }).ToList();
        }

        public async Task<List<InventoryBatchReportDto>> GetInventoryBatchesAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.ProductBatches.Include(b => b.Product).AsNoTracking().AsQueryable();

            if (dateFrom.HasValue) query = query.Where(b => b.EntryDate >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(b => b.EntryDate <= dateTo.Value);

            var batches = await query.ToListAsync();

            return batches.Select(b => new InventoryBatchReportDto
            {
                BatchId = b.Id,
                ProductName = b.Product?.Name ?? string.Empty,
                QuantityReceived = b.QuantityReceived,
                QuantityAvailable = b.QuantityAvailable,
                EntryDate = b.EntryDate,
                EntryInvoiceNumber = b.EntryInvoiceNumber ?? string.Empty
            }).ToList();
        }

        public async Task<List<InventoryChartDto>> GetInventoryChartsAsync()
        {
            var summary = await GetInventorySummaryAsync(null, 0); // threshold doesn't matter for total value
            return summary.GroupBy(s => s.CategoryName)
                .Select(g => new InventoryChartDto
                {
                    CategoryName = g.Key,
                    TotalStockValueUsd = g.Sum(x => x.TotalStockValueUsd)
                }).ToList();
        }

        // Expiry

        public async Task<List<ExpirySummaryReportDto>> GetExpirySummaryAsync(decimal expiryAlertDays)
        {
            var now = DateTime.UtcNow;
            var alertDate = now.AddDays((double)expiryAlertDays);

            var batches = await _context.ProductBatches
                .Include(b => b.Product)
                .Where(b => b.Product!.HasExpiry && b.ExpiryDate != null && b.QuantityAvailable > 0)
                .AsNoTracking()
                .ToListAsync();

            var summary = batches.GroupBy(b => new { b.ProductId, b.Product!.Name })
                .Select(g =>
                {
                    var expired = g.Where(b => b.ExpiryDate < now).ToList();
                    var expiringSoon = g.Where(b => b.ExpiryDate >= now && b.ExpiryDate <= alertDate).ToList();
                    var pPrice = g.First().Product?.PriceUsd ?? 0;

                    return new ExpirySummaryReportDto
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        ExpiredBatchesCount = expired.Count,
                        ExpiringSoonBatchesCount = expiringSoon.Count,
                        TotalExpiredValueUsd = expired.Sum(b => b.QuantityAvailable) * pPrice
                    };
                }).ToList();

            return summary;
        }

        public async Task<List<ExpiryBatchReportDto>> GetExpiryBatchesAsync(decimal expiryAlertDays)
        {
            var now = DateTime.UtcNow;
            var alertDate = now.AddDays((double)expiryAlertDays);

            var batches = await _context.ProductBatches
                .Include(b => b.Product)
                .Where(b => b.Product!.HasExpiry && b.ExpiryDate != null && b.QuantityAvailable > 0)
                .AsNoTracking()
                .ToListAsync();

            return batches.Select(b =>
            {
                string status = b.ExpiryDate < now ? "Expired" : (b.ExpiryDate <= alertDate ? "ExpiringSoon" : "Fresh");
                return new ExpiryBatchReportDto
                {
                    BatchId = b.Id,
                    ProductName = b.Product?.Name ?? string.Empty,
                    QuantityAvailable = b.QuantityAvailable,
                    ExpiryDate = b.ExpiryDate,
                    ExpiryStatus = status,
                    DaysUntilExpiry = (int)(b.ExpiryDate!.Value.Date - now.Date).TotalDays
                };
            }).ToList();
        }

        public async Task<List<ExpiryChartDto>> GetExpiryChartsAsync(decimal expiryAlertDays)
        {
            var batches = await GetExpiryBatchesAsync(expiryAlertDays);
            return batches.GroupBy(b => b.ExpiryStatus)
                .Select(g => new ExpiryChartDto
                {
                    ExpiryStatus = g.Key,
                    BatchCount = g.Count()
                }).ToList();
        }
    }
}
