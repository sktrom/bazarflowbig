using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Application.Products.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Contracts.WorkingCart;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.WorkingCart.Services
{
    public class CartService : ICartService
    {
        private readonly ICartManagementRepository _cartRepo;
        private readonly IProductManagementRepository _productRepo;
        private readonly IOfferManagementRepository _offerRepo;
        private readonly ISessionContext _sessionContext;

        public CartService(
            ICartManagementRepository cartRepo, 
            IProductManagementRepository productRepo, 
            IOfferManagementRepository offerRepo, 
            ISessionContext sessionContext)
        {
            _cartRepo = cartRepo;
            _productRepo = productRepo;
            _offerRepo = offerRepo;
            _sessionContext = sessionContext;
        }

        private async Task<Invoice> GetOrCreateWorkingInvoiceAsync(long employeeId)
        {
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null)
            {
                cart = await _cartRepo.CreateWorkingInvoiceAsync(new Invoice
                {
                    InvoiceNumber = "WRK-" + DateTime.UtcNow.Ticks,
                    OriginalEmployeeId = employeeId,
                    Status = InvoiceStatus.Working,
                    CreatedAt = DateTime.UtcNow,
                    SubtotalUsd = 0,
                    TotalUsd = 0
                });
            }
            return cart;
        }

        private async Task<List<InvoiceLine>> GetCartLinesAsync(long invoiceId)
        {
            var lines = await _cartRepo.GetCartLinesAsync(invoiceId);
            return lines ?? new List<InvoiceLine>();
        }

        private async Task<CartResponse> MapCartToResponseAsync(Invoice? invoice)
        {
            if (invoice == null) return new CartResponse(); // Empty/New Model

            var lines = await GetCartLinesAsync(invoice.Id);

            return new CartResponse
            {
                InvoiceId = invoice.Id,
                Status = "Working",
                CustomerName = invoice.CustomerName,
                InvoiceDiscountType = invoice.InvoiceDiscountType?.ToString(),
                InvoiceDiscountValue = invoice.InvoiceDiscountValue,
                SubtotalUsd = invoice.SubtotalUsd,
                TotalUsd = invoice.TotalUsd,
                Lines = lines.Select(l => new CartLineDto
                {
                    LineId = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.Product?.Name ?? string.Empty,
                    Quantity = l.Quantity,
                    UnitPriceUsdOriginal = l.UnitPriceUsdOriginal,
                    LineTotalUsdOriginal = l.LineTotalUsdOriginal,
                    LineTotalUsdEffective = l.LineTotalUsdEffective,
                    IsPriceOverridden = l.IsPriceOverridden,
                    OfferId = l.OfferId
                }).ToList()
            };
        }

        private void RecalculateCartTotals(Invoice invoice, List<InvoiceLine> lines)
        {
            invoice.SubtotalUsd = lines.Sum(l => l.LineTotalUsdEffective);

            decimal discountAmount = 0;
            if (invoice.InvoiceDiscountType == InvoiceDiscountType.Amount && invoice.InvoiceDiscountValue.HasValue)
            {
                discountAmount = invoice.InvoiceDiscountValue.Value;
            }
            else if (invoice.InvoiceDiscountType == InvoiceDiscountType.Percent && invoice.InvoiceDiscountValue.HasValue)
            {
                discountAmount = invoice.SubtotalUsd * (invoice.InvoiceDiscountValue.Value / 100m);
            }

            var calculatedTotal = invoice.SubtotalUsd - discountAmount;
            invoice.TotalUsd = calculatedTotal < 0 ? 0 : calculatedTotal;
        }

        private async Task ApplyPricingAndOfferAsync(InvoiceLine line)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");

            line.UnitPriceUsdOriginal = product.PriceUsd;
            line.LineTotalUsdOriginal = line.Quantity * line.UnitPriceUsdOriginal;

            if (line.IsPriceOverridden) return; // Skip offer logic if overridden

            // Natural compute
            var offer = await _offerRepo.GetActiveOfferForProductAsync(line.ProductId); // Auto throws MULTIPLE_ACTIVE_OFFERS_FOUND if ambiguous
            if (offer != null)
            {
                line.OfferId = offer.Id;
                if (offer.DiscountType == OfferDiscountType.Amount)
                {
                    line.LineTotalUsdEffective = line.LineTotalUsdOriginal - offer.DiscountValue;
                }
                else if (offer.DiscountType == OfferDiscountType.Percent)
                {
                    var discountAmt = line.LineTotalUsdOriginal * (offer.DiscountValue / 100m);
                    line.LineTotalUsdEffective = line.LineTotalUsdOriginal - discountAmt;
                }
            }
            else
            {
                line.OfferId = null;
                line.LineTotalUsdEffective = line.LineTotalUsdOriginal;
            }
        }

        public async Task<CartResponse> GetCurrentCartAsync()
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            return await MapCartToResponseAsync(cart);
        }

        private async Task<CartResponse> AddItemInternalAsync(Product product)
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await GetOrCreateWorkingInvoiceAsync(employeeId);
            var lines = await GetCartLinesAsync(cart.Id);

            var existingLine = lines.FirstOrDefault(l => l.ProductId == product.Id);
            if (existingLine != null)
            {
                existingLine.Quantity += 1m;
                existingLine.IsPriceOverridden = false; // Reset override on quantity change implicitly
                await ApplyPricingAndOfferAsync(existingLine);
            }
            else
            {
                var newLine = new InvoiceLine
                {
                    InvoiceId = cart.Id,
                    ProductId = product.Id,
                    Quantity = 1m,
                    IsPriceOverridden = false,
                    SortOrder = lines.Count > 0 ? lines.Max(l => l.SortOrder) + 1 : 1
                };
                await ApplyPricingAndOfferAsync(newLine);
                await _cartRepo.AddLineAsync(newLine);
                lines.Add(newLine);
            }

            RecalculateCartTotals(cart, lines);
            await _cartRepo.UpdateInvoiceAsync(cart);

            return await MapCartToResponseAsync(cart);
        }

        public async Task<CartResponse> AddByBarcodeAsync(AddByBarcodeRequest request)
        {
            var product = await _productRepo.GetByBarcodeAsync(request.Barcode);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");
            return await AddItemInternalAsync(product);
        }

        public async Task<CartResponse> AddByProductAsync(AddByProductRequest request)
        {
            var product = await _productRepo.GetByIdAsync(request.ProductId);
            if (product == null) throw new InvalidOperationException("PRODUCT_NOT_FOUND");
            return await AddItemInternalAsync(product);
        }

        public async Task<CartResponse> UpdateLineAsync(long lineId, UpdateLineRequest request)
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            var lines = await GetCartLinesAsync(cart.Id);
            var line = lines.FirstOrDefault(l => l.Id == lineId);
            if (line == null) throw new InvalidOperationException("LINE_NOT_FOUND");

            if (request.Quantity.HasValue)
            {
                if (request.Quantity.Value <= 0) throw new InvalidOperationException("INVALID_QUANTITY");
                
                // If it really changes quantity, clear override
                if (line.Quantity != request.Quantity.Value)
                {
                    line.Quantity = request.Quantity.Value;
                    line.IsPriceOverridden = false;
                }
            }

            if (request.OverrideLineTotalUsd.HasValue && !request.Quantity.HasValue)
            {
                // Applying manual override (must happen without changing quantity natively, or after)
                line.IsPriceOverridden = true;
                line.LineTotalUsdEffective = request.OverrideLineTotalUsd.Value;
            }

            // Always re-apply pricing if it's not overridden, or if we just changed quantity.
            if (!line.IsPriceOverridden)
            {
                await ApplyPricingAndOfferAsync(line);
            }

            RecalculateCartTotals(cart, lines);
            await _cartRepo.UpdateInvoiceAsync(cart);
            await _cartRepo.SaveChangesAsync();

            return await MapCartToResponseAsync(cart);
        }

        public async Task<CartResponse> DeleteLineAsync(long lineId)
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            var lines = await GetCartLinesAsync(cart.Id);
            var line = lines.FirstOrDefault(l => l.Id == lineId);
            if (line == null) throw new InvalidOperationException("LINE_NOT_FOUND");

            await _cartRepo.RemoveLineAsync(line);
            lines.Remove(line);

            if (lines.Count == 0)
            {
                // Last line deleted, reset cart to empty
                await _cartRepo.DeleteInvoiceAsync(cart);
                return new CartResponse();
            }

            RecalculateCartTotals(cart, lines);
            await _cartRepo.UpdateInvoiceAsync(cart);

            return await MapCartToResponseAsync(cart);
        }

        public async Task<CartResponse> UpdateDiscountAsync(UpdateDiscountRequest request)
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            if (!Enum.TryParse<InvoiceDiscountType>(request.DiscountType, out var dType))
                throw new InvalidOperationException("INVALID_DISCOUNT_TYPE");

            cart.InvoiceDiscountType = dType;
            cart.InvoiceDiscountValue = request.DiscountValue;

            var lines = await GetCartLinesAsync(cart.Id);
            RecalculateCartTotals(cart, lines);
            await _cartRepo.UpdateInvoiceAsync(cart);

            return await MapCartToResponseAsync(cart);
        }

        public async Task<CartResponse> UpdateCustomerAsync(UpdateCustomerRequest request)
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            cart.CustomerName = request.CustomerName;
            await _cartRepo.UpdateInvoiceAsync(cart);

            return await MapCartToResponseAsync(cart);
        }

        public async Task<CartResponse> DeleteCustomerAsync()
        {
            var employeeId = _sessionContext.EmployeeId;
            var cart = await _cartRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (cart == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            cart.CustomerName = null;
            await _cartRepo.UpdateInvoiceAsync(cart);

            return await MapCartToResponseAsync(cart);
        }
    }
}
