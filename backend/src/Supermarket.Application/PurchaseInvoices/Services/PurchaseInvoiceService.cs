using System;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.PurchaseInvoices.Interfaces;
using Supermarket.Contracts.PurchaseInvoices;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.PurchaseInvoices.Services
{
    public class PurchaseInvoiceService : IPurchaseInvoiceService
    {
        private const int ProductLookupLimit = 20;
        private readonly IPurchaseInvoiceRepository _repository;
        private readonly ISessionContext _sessionContext;

        public PurchaseInvoiceService(IPurchaseInvoiceRepository repository, ISessionContext sessionContext)
        {
            _repository = repository;
            _sessionContext = sessionContext;
        }

        public async Task<PurchaseInvoiceListResponse> GetAllAsync()
        {
            var invoices = await _repository.GetAllAsync();
            return new PurchaseInvoiceListResponse
            {
                Items = invoices.Select(i => new PurchaseInvoiceListItem
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierId = i.SupplierId,
                    SupplierName = i.Supplier?.Name ?? string.Empty,
                    Status = i.Status.ToString(),
                    ExternalInvoiceNumber = i.ExternalInvoiceNumber,
                    SubtotalUsd = i.SubtotalUsd,
                    TotalUsd = i.TotalUsd,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToList()
            };
        }

        public async Task<PurchaseInvoiceDetailResponse> GetByIdAsync(long id)
        {
            var invoice = await _repository.GetByIdWithDetailsAsync(id);
            if (invoice == null) throw new InvalidOperationException("PURCHASE_INVOICE_NOT_FOUND");

            return MapToDetail(invoice);
        }

        public async Task<PurchaseInvoiceDetailResponse> CreateAsync(CreatePurchaseInvoiceRequest request)
        {
            if (_sessionContext.EmployeeId <= 0)
                throw new InvalidOperationException("NO_ACTIVE_SESSION");

            var supplier = await RequireActiveSupplierAsync(request.SupplierId);
            var now = DateTime.UtcNow;
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(now),
                SupplierId = supplier.Id,
                CreatedByEmployeeId = _sessionContext.EmployeeId,
                Status = PurchaseInvoiceStatus.Draft,
                ExternalInvoiceNumber = NormalizeOptional(request.ExternalInvoiceNumber),
                Notes = NormalizeOptional(request.Notes),
                SubtotalUsd = 0m,
                TotalUsd = 0m,
                CreatedAt = now,
                UpdatedAt = now
            };

            var created = await _repository.CreateAsync(invoice);
            return await GetByIdAsync(created.Id);
        }

        public async Task<PurchaseInvoiceDetailResponse> UpdateAsync(long id, UpdatePurchaseInvoiceRequest request)
        {
            var invoice = await RequireInvoiceForUpdateAsync(id);
            var supplier = await RequireActiveSupplierAsync(request.SupplierId);

            invoice.SupplierId = supplier.Id;
            invoice.ExternalInvoiceNumber = NormalizeOptional(request.ExternalInvoiceNumber);
            invoice.Notes = NormalizeOptional(request.Notes);
            invoice.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(invoice);
            return await GetByIdAsync(id);
        }

        public async Task<DeletePurchaseInvoiceResponse> DeleteAsync(long id)
        {
            var invoice = await RequireInvoiceForUpdateAsync(id);

            await _repository.ExecuteInTransactionAsync(async () =>
            {
                await _repository.DeleteLinesAsync(invoice.Id);
                await _repository.DeleteAsync(invoice);
            });

            return new DeletePurchaseInvoiceResponse
            {
                Success = true,
                Action = "DELETED",
                Message = "Purchase invoice deleted successfully."
            };
        }

        public async Task<PurchaseInvoiceDetailResponse> AddLineAsync(long invoiceId, CreatePurchaseInvoiceLineRequest request)
        {
            await RequireInvoiceForUpdateAsync(invoiceId);
            var product = await RequireActiveProductAsync(request.ProductId);
            ValidateLine(request.Quantity, request.UnitCostUsd, request.ExpiryDate, product);

            await _repository.ExecuteInTransactionAsync(async () =>
            {
                var sortOrder = await _repository.GetNextLineSortOrderAsync(invoiceId);
                var line = new PurchaseInvoiceLine
                {
                    PurchaseInvoiceId = invoiceId,
                    ProductId = product.Id,
                    Quantity = request.Quantity,
                    UnitCostUsd = request.UnitCostUsd,
                    LineTotalUsd = request.Quantity * request.UnitCostUsd,
                    ExpiryDate = request.ExpiryDate,
                    Notes = NormalizeOptional(request.Notes),
                    SortOrder = sortOrder
                };

                await _repository.AddLineAsync(line);
                await _repository.RecalculateTotalsAsync(invoiceId);
            });

            return await GetByIdAsync(invoiceId);
        }

        public async Task<PurchaseInvoiceDetailResponse> UpdateLineAsync(long invoiceId, long lineId, UpdatePurchaseInvoiceLineRequest request)
        {
            await RequireInvoiceForUpdateAsync(invoiceId);
            var line = await _repository.GetLineAsync(invoiceId, lineId);
            if (line == null) throw new InvalidOperationException("PURCHASE_INVOICE_LINE_NOT_FOUND");

            var product = line.Product ?? await RequireActiveProductAsync(line.ProductId);
            ValidateLine(request.Quantity, request.UnitCostUsd, request.ExpiryDate, product);

            await _repository.ExecuteInTransactionAsync(async () =>
            {
                line.Quantity = request.Quantity;
                line.UnitCostUsd = request.UnitCostUsd;
                line.LineTotalUsd = request.Quantity * request.UnitCostUsd;
                line.ExpiryDate = request.ExpiryDate;
                line.Notes = NormalizeOptional(request.Notes);
                await _repository.SaveChangesAsync();
                await _repository.RecalculateTotalsAsync(invoiceId);
            });

            return await GetByIdAsync(invoiceId);
        }

        public async Task<DeletePurchaseInvoiceLineResponse> DeleteLineAsync(long invoiceId, long lineId)
        {
            await RequireInvoiceForUpdateAsync(invoiceId);
            var line = await _repository.GetLineAsync(invoiceId, lineId);
            if (line == null) throw new InvalidOperationException("PURCHASE_INVOICE_LINE_NOT_FOUND");

            await _repository.ExecuteInTransactionAsync(async () =>
            {
                await _repository.DeleteLineAsync(line);
                await _repository.RecalculateTotalsAsync(invoiceId);
            });

            return new DeletePurchaseInvoiceLineResponse
            {
                Success = true,
                Message = "Purchase invoice line deleted successfully."
            };
        }

        public async Task<PurchaseProductLookupResponse> LookupProductsAsync(string? search)
        {
            var products = await _repository.LookupProductsAsync(search, ProductLookupLimit);
            return new PurchaseProductLookupResponse
            {
                Items = products.Select(p => new PurchaseProductLookupItem
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    PriceUsd = p.PriceUsd,
                    HasExpiry = p.HasExpiry,
                    BaseUnit = p.BaseUnit
                }).ToList()
            };
        }

        private async Task<PurchaseInvoice> RequireInvoiceForUpdateAsync(long id)
        {
            var invoice = await _repository.GetByIdForUpdateAsync(id);
            if (invoice == null) throw new InvalidOperationException("PURCHASE_INVOICE_NOT_FOUND");
            if (invoice.Status != PurchaseInvoiceStatus.Draft)
                throw new InvalidOperationException("PURCHASE_INVOICE_NOT_DRAFT");

            return invoice;
        }

        private async Task<Supplier> RequireActiveSupplierAsync(long supplierId)
        {
            var supplier = await _repository.GetSupplierAsync(supplierId);
            if (supplier == null) throw new InvalidOperationException("SUPPLIER_NOT_FOUND");
            if (!supplier.IsActive) throw new InvalidOperationException("SUPPLIER_INACTIVE");
            return supplier;
        }

        private async Task<Product> RequireActiveProductAsync(long productId)
        {
            var product = await _repository.GetProductAsync(productId);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");
            if (!product.IsActive) throw new InvalidOperationException("PRODUCT_INACTIVE");
            return product;
        }

        private static void ValidateLine(decimal quantity, decimal unitCostUsd, DateTime? expiryDate, Product product)
        {
            if (quantity <= 0) throw new InvalidOperationException("INVALID_QUANTITY");
            if (unitCostUsd < 0) throw new InvalidOperationException("INVALID_UNIT_COST");
            if (product.HasExpiry && !expiryDate.HasValue) throw new InvalidOperationException("EXPIRY_DATE_REQUIRED");
            if (!product.HasExpiry && expiryDate.HasValue) throw new InvalidOperationException("EXPIRY_DATE_NOT_ALLOWED");
        }

        private async Task<string> GenerateInvoiceNumberAsync(DateTime now)
        {
            var sequence = await _repository.GetInvoiceCountForDateAsync(now.Date) + 1;
            return $"PI-{now:yyyyMMdd}-{sequence:000000}";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static PurchaseInvoiceDetailResponse MapToDetail(PurchaseInvoice invoice)
        {
            return new PurchaseInvoiceDetailResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                SupplierId = invoice.SupplierId,
                SupplierName = invoice.Supplier?.Name ?? string.Empty,
                CreatedByEmployeeId = invoice.CreatedByEmployeeId,
                CreatedByEmployeeName = invoice.CreatedByEmployee?.FullName ?? string.Empty,
                Status = invoice.Status.ToString(),
                ExternalInvoiceNumber = invoice.ExternalInvoiceNumber,
                Notes = invoice.Notes,
                SubtotalUsd = invoice.SubtotalUsd,
                TotalUsd = invoice.TotalUsd,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                Lines = invoice.Lines
                    .OrderBy(line => line.SortOrder)
                    .Select(line => new PurchaseInvoiceLineDto
                    {
                        Id = line.Id,
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name ?? string.Empty,
                        Barcode = line.Product?.Barcode ?? string.Empty,
                        Quantity = line.Quantity,
                        UnitCostUsd = line.UnitCostUsd,
                        LineTotalUsd = line.LineTotalUsd,
                        ExpiryDate = line.ExpiryDate,
                        Notes = line.Notes,
                        SortOrder = line.SortOrder
                    }).ToList()
            };
        }
    }
}
