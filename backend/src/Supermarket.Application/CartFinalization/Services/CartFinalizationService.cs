using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Contracts.CartFinalization;
using Supermarket.Contracts.WorkingCart;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.CartFinalization.Services
{
    public class CartFinalizationService : ICartFinalizationService
    {
        private readonly ICartFinalizationRepository _finalizationRepo;
        private readonly IInventoryAllocationRepository _inventoryRepo;
        private readonly IAppSettingsRepository _settingsRepo;
        private readonly ICartManagementRepository _cartRepo;
        private readonly ISessionContext _sessionContext;
        private readonly IAuditLogService _auditLogService;

        public CartFinalizationService(
            ICartFinalizationRepository finalizationRepo,
            IInventoryAllocationRepository inventoryRepo,
            IAppSettingsRepository settingsRepo,
            ICartManagementRepository cartRepo,
            ISessionContext sessionContext,
            IAuditLogService auditLogService)
        {
            _finalizationRepo = finalizationRepo;
            _inventoryRepo = inventoryRepo;
            _settingsRepo = settingsRepo;
            _cartRepo = cartRepo;
            _sessionContext = sessionContext;
            _auditLogService = auditLogService;
        }

        public async Task<CartResponse> SuspendAsync(SuspendCartRequest request)
        {
            var employeeId = _sessionContext.EmployeeId;

            if (!Enum.TryParse<InvoiceSuspensionReason>(request.SuspensionReason, out var reason))
                throw new InvalidOperationException("INVALID_SUSPENSION_REASON");

            var invoice = await _finalizationRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (invoice == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            if ((reason == InvoiceSuspensionReason.Financial || reason == InvoiceSuspensionReason.Both)
                && string.IsNullOrWhiteSpace(invoice.CustomerName))
                throw new InvalidOperationException("CUSTOMER_NAME_REQUIRED");

            // Reserve inventory (FEFO handled in repo)
            await ReserveInventoryAsync(invoice.Id);

            invoice.Status = InvoiceStatus.Suspended;
            invoice.SuspensionReason = reason;
            await _finalizationRepo.UpdateInvoiceAsync(invoice);
            await _finalizationRepo.SaveChangesAsync();

            await RecordAuditAsync(
                "SUSPEND_INVOICE",
                invoice,
                new
                {
                    invoiceId = invoice.Id,
                    invoice.InvoiceNumber,
                    status = invoice.Status.ToString(),
                    invoice.TotalUsd,
                    invoice.TotalSyp,
                    suspensionReason = invoice.SuspensionReason?.ToString()
                });

            return MapToCartResponse(invoice);
        }

        public async Task<CartResponse> CompleteAsync()
        {
            var employeeId = _sessionContext.EmployeeId;

            var invoice = await _finalizationRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (invoice == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            // Read exchange rate - fails loudly if not configured
            var rate = await _settingsRepo.GetRequiredDecimalAsync("exchange_rate_syp");

            // Snapshot exchange rate and compute SYP total
            invoice.ExchangeRateSypSnapshot = rate;
            invoice.TotalSyp = invoice.TotalUsd * rate;

            // Ensure inventory is reserved before consuming (handles direct Working -> Complete)
            var reservedAllocations = await _inventoryRepo.GetReservedByInvoiceAsync(invoice.Id);
            var lineCount = reservedAllocations.Select(a => a.InvoiceLineId).Distinct().Count();
            if (!reservedAllocations.Any())
            {
                lineCount = await ReserveInventoryAsync(invoice.Id);
            }

            // Consume inventory (FEFO handled in repo)
            await ConsumeInventoryAsync(invoice.Id);

            invoice.Status = InvoiceStatus.Completed;
            invoice.CompletedAt = DateTime.UtcNow;
            await _finalizationRepo.UpdateInvoiceAsync(invoice);
            await _finalizationRepo.SaveChangesAsync();

            await _auditLogService.RecordAsync(
                "COMPLETE_INVOICE",
                "Invoice",
                invoice.Id.ToString(),
                invoice.InvoiceNumber,
                metadata: new
                {
                    invoice.TotalUsd,
                    invoice.TotalSyp,
                    lineCount
                });

            return MapToCartResponse(invoice);
        }

        public async Task<CartResponse> CancelCurrentAsync()
        {
            var employeeId = _sessionContext.EmployeeId;

            var invoice = await _finalizationRepo.GetWorkingInvoiceByEmployeeAsync(employeeId);
            if (invoice == null) throw new InvalidOperationException("NO_WORKING_CART_EXISTS");

            var cancelMetadata = new
            {
                invoiceId = invoice.Id,
                invoice.InvoiceNumber,
                status = invoice.Status.ToString(),
                invoice.TotalUsd,
                invoice.TotalSyp
            };

            // Release any reserved allocations first
            await ReleaseInventoryAsync(invoice.Id);

            // Physical delete inside same unit-of-work (SaveChanges called by repo)
            await _finalizationRepo.DeleteInvoiceWithLinesAsync(invoice.Id);

            await RecordAuditAsync("CANCEL_INVOICE", invoice, cancelMetadata);

            return new CartResponse(); // Empty model
        }

        public async Task<CartResponse> LoadSuspendedAsync(long invoiceId)
        {
            var employeeId = _sessionContext.EmployeeId;

            var invoice = await _finalizationRepo.GetSuspendedInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("INVOICE_NOT_FOUND");
            if (invoice.Status != InvoiceStatus.Suspended) throw new InvalidOperationException("INVOICE_NOT_SUSPENDED");

            // Block if current cashier already has a non-empty Working cart
            var hasNonEmptyCart = await _finalizationRepo.EmployeeHasNonEmptyWorkingCartAsync(employeeId);
            if (hasNonEmptyCart) throw new InvalidOperationException("WORKING_CART_NOT_EMPTY");

            invoice.Status = InvoiceStatus.Working;
            invoice.SuspensionReason = null;
            await _finalizationRepo.UpdateInvoiceAsync(invoice);
            await _finalizationRepo.SaveChangesAsync();

            return MapToCartResponse(invoice);
        }

        // ─── Private Inventory Helpers ────────────────────────────────────────────

        private async Task<int> ReserveInventoryAsync(long invoiceId)
        {
            var lines = await _cartRepo.GetCartLinesAsync(invoiceId);
            foreach (var line in lines)
            {
                var remaining = line.Quantity;
                var batches = await _inventoryRepo.GetAvailableBatchesFEFOAsync(line.ProductId);

                foreach (var batch in batches)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(batch.QuantityAvailable, remaining);

                    var allocation = new InvoiceLineBatchAllocation
                    {
                        InvoiceLineId = line.Id,
                        BatchId = batch.Id,
                        Quantity = take,
                        AllocationStatus = AllocationStatus.Reserved
                    };
                    await _inventoryRepo.AddAllocationAsync(allocation);

                    // Reduce available on batch
                    batch.QuantityAvailable -= take;
                    await _inventoryRepo.UpdateBatchAsync(batch);

                    remaining -= take;
                }

                if (remaining > 0)
                    throw new InvalidOperationException("INSUFFICIENT_INVENTORY");
            }

            await _inventoryRepo.SaveChangesAsync();
            return lines.Count;
        }

        private async Task ConsumeInventoryAsync(long invoiceId)
        {
            var allocations = await _inventoryRepo.GetReservedByInvoiceAsync(invoiceId);
            foreach (var alloc in allocations)
            {
                alloc.AllocationStatus = AllocationStatus.Consumed;
                await _inventoryRepo.UpdateAllocationAsync(alloc);
                // QuantityAvailable was already decremented at Reserve time; no further batch change needed.
            }

            await _inventoryRepo.SaveChangesAsync();
        }

        private async Task ReleaseInventoryAsync(long invoiceId)
        {
            var allocations = await _inventoryRepo.GetReservedByInvoiceAsync(invoiceId);
            foreach (var alloc in allocations)
            {
                // Restore batch quantity
                if (alloc.Batch != null)
                {
                    alloc.Batch.QuantityAvailable += alloc.Quantity;
                    await _inventoryRepo.UpdateBatchAsync(alloc.Batch);
                }

                alloc.AllocationStatus = AllocationStatus.Released;
                await _inventoryRepo.UpdateAllocationAsync(alloc);
            }

            await _inventoryRepo.SaveChangesAsync();
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private async Task RecordAuditAsync(string action, Invoice invoice, object metadata)
        {
            try
            {
                await _auditLogService.RecordAsync(
                    action,
                    "Invoice",
                    invoice.Id.ToString(),
                    invoice.InvoiceNumber,
                    metadata: metadata);
            }
            catch
            {
                // Audit logging is best-effort and must not break cart finalization.
            }
        }

        private static CartResponse MapToCartResponse(Invoice invoice)
        {
            return new CartResponse
            {
                InvoiceId = invoice.Id,
                Status = invoice.Status.ToString(),
                CustomerName = invoice.CustomerName,
                InvoiceDiscountType = invoice.InvoiceDiscountType?.ToString(),
                InvoiceDiscountValue = invoice.InvoiceDiscountValue,
                SubtotalUsd = invoice.SubtotalUsd,
                TotalUsd = invoice.TotalUsd,
                Lines = new System.Collections.Generic.List<CartLineDto>()
            };
        }
    }
}
