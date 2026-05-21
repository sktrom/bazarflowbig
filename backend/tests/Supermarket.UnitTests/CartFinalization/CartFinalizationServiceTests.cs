using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Application.CartFinalization.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Contracts.CartFinalization;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.CartFinalization
{
    public class CartFinalizationServiceTests
    {
        private readonly Mock<ICartFinalizationRepository> _finalizationRepoMock = new();
        private readonly Mock<IInventoryAllocationRepository> _inventoryRepoMock = new();
        private readonly Mock<IAppSettingsRepository> _settingsRepoMock = new();
        private readonly Mock<ICartManagementRepository> _cartRepoMock = new();
        private readonly Mock<ISessionContext> _sessionContextMock = new();
        private readonly Mock<IAuditLogService> _auditLogMock = new();

        private CartFinalizationService CreateService() =>
            new CartFinalizationService(
                _finalizationRepoMock.Object,
                _inventoryRepoMock.Object,
                _settingsRepoMock.Object,
                _cartRepoMock.Object,
                _sessionContextMock.Object,
                _auditLogMock.Object);

        // ─── Suspend Tests ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Suspend_ShouldThrow_WhenReasonIsFinancialAndCustomerNameIsNull()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working, CustomerName = null });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Financial" }));

            Assert.Equal("CUSTOMER_NAME_REQUIRED", ex.Message);
        }

        [Fact]
        public async Task Suspend_ShouldThrow_WhenReasonIsBothAndCustomerNameIsNull()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working, CustomerName = null });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Both" }));

            Assert.Equal("CUSTOMER_NAME_REQUIRED", ex.Message);
        }

        [Fact]
        public async Task Suspend_ShouldThrow_WhenNoWorkingCart()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync((Invoice?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Incomplete" }));

            Assert.Equal("NO_WORKING_CART_EXISTS", ex.Message);
        }

        [Fact]
        public async Task Suspend_ShouldThrow_WhenInvalidReason()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "INVALID" }));

            Assert.Equal("INVALID_SUSPENSION_REASON", ex.Message);
        }

        [Fact]
        public async Task SuspendAsync_ShouldRecordSuspendInvoice()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice
            {
                Id = 10,
                InvoiceNumber = "INV-10",
                Status = InvoiceStatus.Working,
                TotalUsd = 25m,
                TotalSyp = 375000m
            };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            SetupReservation(invoice.Id);

            var service = CreateService();
            await service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Incomplete" });

            _auditLogMock.Verify(a => a.RecordAsync(
                "SUSPEND_INVOICE",
                "Invoice",
                "10",
                "INV-10",
                null,
                null,
                It.Is<object>(metadata =>
                    JsonContains(metadata, "\"invoiceId\":10") &&
                    JsonContains(metadata, "\"InvoiceNumber\":\"INV-10\"") &&
                    JsonContains(metadata, "\"status\":\"Suspended\"") &&
                    JsonContains(metadata, "\"TotalUsd\":25") &&
                    JsonContains(metadata, "\"TotalSyp\":375000") &&
                    JsonContains(metadata, "\"suspensionReason\":\"Incomplete\""))),
                Times.Once);
        }

        // ─── Complete Tests ────────────────────────────────────────────────────────

        [Fact]
        public async Task Complete_ShouldThrow_WhenExchangeRateNotConfigured()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working, TotalUsd = 100 });

            _settingsRepoMock
                .Setup(r => r.GetRequiredDecimalAsync("exchange_rate_syp"))
                .ThrowsAsync(new InvalidOperationException("EXCHANGE_RATE_NOT_CONFIGURED"));

            _cartRepoMock.Setup(r => r.GetCartLinesAsync(10)).ReturnsAsync(new List<InvoiceLine>());

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync());

            Assert.Equal("EXCHANGE_RATE_NOT_CONFIGURED", ex.Message);
        }

        [Fact]
        public async Task Complete_ShouldSnapshotExchangeRateAndComputeTotalSyp()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice { Id = 10, Status = InvoiceStatus.Working, TotalUsd = 100m };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            _settingsRepoMock.Setup(r => r.GetRequiredDecimalAsync("exchange_rate_syp")).ReturnsAsync(15000m);
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(10)).ReturnsAsync(new List<InvoiceLine>());
            _inventoryRepoMock.Setup(r => r.GetReservedByInvoiceAsync(10)).ReturnsAsync(new List<InvoiceLineBatchAllocation>());

            var service = CreateService();
            var result = await service.CompleteAsync();

            Assert.Equal(15000m, invoice.ExchangeRateSypSnapshot);
            Assert.Equal(1_500_000m, invoice.TotalSyp);
            Assert.Equal(InvoiceStatus.Completed, invoice.Status);
            Assert.NotNull(invoice.CompletedAt);
            _auditLogMock.Verify(a => a.RecordAsync(
                "COMPLETE_INVOICE",
                "Invoice",
                "10",
                invoice.InvoiceNumber,
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CompleteAsync_ShouldNotDuplicateCompleteInvoiceAudit()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice { Id = 10, InvoiceNumber = "INV-10", Status = InvoiceStatus.Working, TotalUsd = 100m };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            _settingsRepoMock.Setup(r => r.GetRequiredDecimalAsync("exchange_rate_syp")).ReturnsAsync(15000m);
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(10)).ReturnsAsync(new List<InvoiceLine>());
            _inventoryRepoMock.Setup(r => r.GetReservedByInvoiceAsync(10)).ReturnsAsync(new List<InvoiceLineBatchAllocation>());

            var service = CreateService();
            await service.CompleteAsync();

            _auditLogMock.Verify(a => a.RecordAsync(
                "COMPLETE_INVOICE",
                "Invoice",
                "10",
                "INV-10",
                null,
                null,
                It.IsAny<object>()), Times.Once);
            VerifyAuditNever("SUSPEND_INVOICE");
            VerifyAuditNever("CANCEL_INVOICE");
        }

        [Fact]
        public async Task Complete_ShouldThrow_WhenNoWorkingCart()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync((Invoice?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync());

            Assert.Equal("NO_WORKING_CART_EXISTS", ex.Message);
        }

        [Fact]
        public async Task Complete_FromWorkingCart_ShouldReserveAndConsumeInventory_WhenNoAllocationsExist()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice { Id = 10, Status = InvoiceStatus.Working, TotalUsd = 100m };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            _settingsRepoMock.Setup(r => r.GetRequiredDecimalAsync("exchange_rate_syp")).ReturnsAsync(15000m);
            
            // Setup lines requiring reservation
            var lines = new List<InvoiceLine> 
            { 
                new InvoiceLine { Id = 100, ProductId = 5, Quantity = 2 }
            };
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(10)).ReturnsAsync(lines);
            
            // Setup batches for reservation
            var batch = new ProductBatch { Id = 50, ProductId = 5, QuantityAvailable = 10 };
            _inventoryRepoMock.Setup(r => r.GetAvailableBatchesFEFOAsync(5)).ReturnsAsync(new List<ProductBatch> { batch });
            
            // Setup no prior reservations, but after reservation it should return the newly created ones
            var newlyCreatedAlloc = new InvoiceLineBatchAllocation { Id = 99, AllocationStatus = AllocationStatus.Reserved, Batch = batch, Quantity = 2 };
            _inventoryRepoMock.SetupSequence(r => r.GetReservedByInvoiceAsync(10))
                .ReturnsAsync(new List<InvoiceLineBatchAllocation>()) // First call: check existing
                .ReturnsAsync(new List<InvoiceLineBatchAllocation> { newlyCreatedAlloc }); // Second call: consume

            var service = CreateService();
            await service.CompleteAsync();

            // Verify Reservation occurred
            _inventoryRepoMock.Verify(r => r.AddAllocationAsync(It.Is<InvoiceLineBatchAllocation>(a => a.AllocationStatus == AllocationStatus.Reserved && a.Quantity == 2)), Times.Once);
            Assert.Equal(8, batch.QuantityAvailable); // Deduced from batch

            // Verify Consumption occurred
            Assert.Equal(AllocationStatus.Consumed, newlyCreatedAlloc.AllocationStatus);
            _inventoryRepoMock.Verify(r => r.UpdateAllocationAsync(newlyCreatedAlloc), Times.Once);
        }

        [Fact]
        public async Task Complete_ShouldOnlyConsumeInventory_WhenAllocationsAlreadyExist_ToPreventDoubleDeduction()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice { Id = 10, Status = InvoiceStatus.Working, TotalUsd = 100m };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            _settingsRepoMock.Setup(r => r.GetRequiredDecimalAsync("exchange_rate_syp")).ReturnsAsync(15000m);
            
            // Allocations already exist (e.g. was Suspended before)
            var existingAlloc = new InvoiceLineBatchAllocation { Id = 99, AllocationStatus = AllocationStatus.Reserved, Quantity = 2 };
            _inventoryRepoMock.Setup(r => r.GetReservedByInvoiceAsync(10)).ReturnsAsync(new List<InvoiceLineBatchAllocation> { existingAlloc });

            var service = CreateService();
            await service.CompleteAsync();

            // Should NOT call ReserveInventoryAsync (GetCartLinesAsync wouldn't be called)
            _cartRepoMock.Verify(r => r.GetCartLinesAsync(10), Times.Never);
            _inventoryRepoMock.Verify(r => r.GetAvailableBatchesFEFOAsync(It.IsAny<int>()), Times.Never);
            _inventoryRepoMock.Verify(r => r.AddAllocationAsync(It.IsAny<InvoiceLineBatchAllocation>()), Times.Never);

            // Verify Consumption occurred
            Assert.Equal(AllocationStatus.Consumed, existingAlloc.AllocationStatus);
            _inventoryRepoMock.Verify(r => r.UpdateAllocationAsync(existingAlloc), Times.Once);
        }

        // ─── Cancel Tests ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Cancel_ShouldCallRelease_ThenPhysicalDelete()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice { Id = 10, Status = InvoiceStatus.Working });

            _inventoryRepoMock
                .Setup(r => r.GetReservedByInvoiceAsync(10))
                .ReturnsAsync(new List<InvoiceLineBatchAllocation>());

            var service = CreateService();
            var result = await service.CancelCurrentAsync();

            _finalizationRepoMock.Verify(r => r.DeleteInvoiceWithLinesAsync(10), Times.Once);
            Assert.Equal(0m, result.TotalUsd);  // Empty CartResponse
        }

        [Fact]
        public async Task CancelCurrentAsync_ShouldRecordCancelInvoice()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1))
                .ReturnsAsync(new Invoice
                {
                    Id = 10,
                    InvoiceNumber = "INV-10",
                    Status = InvoiceStatus.Working,
                    TotalUsd = 25m,
                    TotalSyp = 375000m
                });

            _inventoryRepoMock
                .Setup(r => r.GetReservedByInvoiceAsync(10))
                .ReturnsAsync(new List<InvoiceLineBatchAllocation>());

            var service = CreateService();
            await service.CancelCurrentAsync();

            _auditLogMock.Verify(a => a.RecordAsync(
                "CANCEL_INVOICE",
                "Invoice",
                "10",
                "INV-10",
                null,
                null,
                It.Is<object>(metadata =>
                    JsonContains(metadata, "\"invoiceId\":10") &&
                    JsonContains(metadata, "\"InvoiceNumber\":\"INV-10\"") &&
                    JsonContains(metadata, "\"status\":\"Working\"") &&
                    JsonContains(metadata, "\"TotalUsd\":25") &&
                    JsonContains(metadata, "\"TotalSyp\":375000"))),
                Times.Once);
        }

        [Fact]
        public async Task CartOperation_ShouldSucceed_WhenAuditThrows()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice { Id = 10, InvoiceNumber = "INV-10", Status = InvoiceStatus.Working };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            SetupReservation(invoice.Id);
            _auditLogMock
                .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<object?>()))
                .ThrowsAsync(new Exception("audit failed"));

            var service = CreateService();
            var result = await service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Incomplete" });

            Assert.Equal("Suspended", result.Status);
        }

        [Fact]
        public async Task AuditMetadata_ShouldNotContainLinesOrAllocations()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice
            {
                Id = 10,
                InvoiceNumber = "INV-10",
                Status = InvoiceStatus.Working,
                TotalUsd = 25m
            };
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync(invoice);
            SetupReservation(invoice.Id);
            object? metadata = null;
            _auditLogMock
                .Setup(a => a.RecordAsync("SUSPEND_INVOICE", "Invoice", "10", "INV-10", null, null, It.IsAny<object?>()))
                .Callback<string, string, string?, string?, object?, object?, object?>((_, _, _, _, _, _, capturedMetadata) => metadata = capturedMetadata);

            var service = CreateService();
            await service.SuspendAsync(new SuspendCartRequest { SuspensionReason = "Incomplete" });

            var serialized = JsonSerializer.Serialize(metadata);
            Assert.DoesNotContain("Lines", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Allocations", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Batch", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Product", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cancel_ShouldThrow_WhenNoWorkingCart()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock.Setup(r => r.GetWorkingInvoiceByEmployeeAsync(1)).ReturnsAsync((Invoice?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelCurrentAsync());

            Assert.Equal("NO_WORKING_CART_EXISTS", ex.Message);
        }

        // ─── Load Suspended Tests ──────────────────────────────────────────────────

        [Fact]
        public async Task LoadSuspended_ShouldFail_WhenWorkingCartHasLines()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock
                .Setup(r => r.GetSuspendedInvoiceByIdAsync(55))
                .ReturnsAsync(new Invoice { Id = 55, Status = InvoiceStatus.Suspended });
            _finalizationRepoMock
                .Setup(r => r.EmployeeHasNonEmptyWorkingCartAsync(1))
                .ReturnsAsync(true);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadSuspendedAsync(55));

            Assert.Equal("WORKING_CART_NOT_EMPTY", ex.Message);
        }

        [Fact]
        public async Task LoadSuspended_ShouldFail_WhenInvoiceNotFound()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            _finalizationRepoMock.Setup(r => r.GetSuspendedInvoiceByIdAsync(99)).ReturnsAsync((Invoice?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoadSuspendedAsync(99));

            Assert.Equal("INVOICE_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task LoadSuspended_ShouldRestoreWorkingStatus_AndClearReason()
        {
            _sessionContextMock.Setup(s => s.EmployeeId).Returns(1);
            var invoice = new Invoice
            {
                Id = 55, Status = InvoiceStatus.Suspended,
                SuspensionReason = InvoiceSuspensionReason.Incomplete
            };
            _finalizationRepoMock.Setup(r => r.GetSuspendedInvoiceByIdAsync(55)).ReturnsAsync(invoice);
            _finalizationRepoMock.Setup(r => r.EmployeeHasNonEmptyWorkingCartAsync(1)).ReturnsAsync(false);

            var service = CreateService();
            var result = await service.LoadSuspendedAsync(55);

            Assert.Equal(InvoiceStatus.Working, invoice.Status);
            Assert.Null(invoice.SuspensionReason);
            Assert.Equal("Working", result.Status);
        }

        private void SetupReservation(long invoiceId)
        {
            _cartRepoMock.Setup(r => r.GetCartLinesAsync(invoiceId)).ReturnsAsync(new List<InvoiceLine>
            {
                new InvoiceLine { Id = 100, ProductId = 5, Quantity = 2 }
            });

            _inventoryRepoMock.Setup(r => r.GetAvailableBatchesFEFOAsync(5)).ReturnsAsync(new List<ProductBatch>
            {
                new ProductBatch { Id = 50, ProductId = 5, QuantityAvailable = 10 }
            });
        }

        private void VerifyAuditNever(string action)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>()), Times.Never);
        }

        private static bool JsonContains(object value, string expected)
            => JsonSerializer.Serialize(value).Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
