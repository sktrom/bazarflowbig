using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.PurchaseInvoices.Interfaces;
using Supermarket.Application.PurchaseInvoices.Services;
using Supermarket.Contracts.PurchaseInvoices;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.PurchaseInvoices
{
    public class PurchaseInvoiceServiceTests
    {
        private readonly Mock<IPurchaseInvoiceRepository> _repoMock;
        private readonly Mock<ISessionContext> _sessionMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly PurchaseInvoiceService _service;

        public PurchaseInvoiceServiceTests()
        {
            _repoMock = new Mock<IPurchaseInvoiceRepository>();
            _sessionMock = new Mock<ISessionContext>();
            _auditLogMock = new Mock<IAuditLogService>();
            _sessionMock.Setup(s => s.EmployeeId).Returns(42);
            _repoMock.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<System.Data.IsolationLevel>()))
                .Returns<Func<Task>, System.Data.IsolationLevel>((operation, _) => operation());
            _service = new PurchaseInvoiceService(_repoMock.Object, _sessionMock.Object, _auditLogMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateDraftInvoice()
        {
            var supplier = ActiveSupplier();
            _repoMock.Setup(r => r.GetSupplierAsync(1)).ReturnsAsync(supplier);
            _repoMock.Setup(r => r.GetInvoiceCountForDateAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseInvoice>())).ReturnsAsync((PurchaseInvoice invoice) =>
            {
                invoice.Id = 10;
                invoice.Supplier = supplier;
                invoice.CreatedByEmployee = new Employee { Id = 42, FullName = "Cashier" };
                return invoice;
            });
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync((long _) => new PurchaseInvoice
            {
                Id = 10,
                InvoiceNumber = "PI-20260519-000001",
                SupplierId = 1,
                Supplier = supplier,
                CreatedByEmployeeId = 42,
                CreatedByEmployee = new Employee { Id = 42, FullName = "Cashier" },
                Status = PurchaseInvoiceStatus.Draft,
                Lines = new List<PurchaseInvoiceLine>()
            });

            var result = await _service.CreateAsync(new CreatePurchaseInvoiceRequest { SupplierId = 1, Notes = "  note  " });

            Assert.Equal(10, result.Id);
            Assert.Equal("Draft", result.Status);
            _repoMock.Verify(r => r.CreateAsync(It.Is<PurchaseInvoice>(invoice =>
                invoice.SupplierId == 1 &&
                invoice.CreatedByEmployeeId == 42 &&
                invoice.Status == PurchaseInvoiceStatus.Draft &&
                invoice.Notes == "note" &&
                invoice.SubtotalUsd == 0m &&
                invoice.TotalUsd == 0m)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldRecordPurchaseCreate()
        {
            SetupCreateDraftInvoice();

            await _service.CreateAsync(new CreatePurchaseInvoiceRequest { SupplierId = 1 });

            VerifyAudit("PURCHASE_CREATE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"InvoiceNumber\":\"PI-20260519-000001\"") &&
                JsonContains(metadata, "\"SupplierId\":1") &&
                JsonContains(metadata, "\"Status\":\"Draft\"") &&
                JsonContains(metadata, "\"TotalUsd\":0") &&
                JsonContains(metadata, "\"lineCount\":0"));
        }

        [Fact]
        public async Task CreateAsync_ShouldRejectMissingSupplier()
        {
            _repoMock.Setup(r => r.GetSupplierAsync(99)).ReturnsAsync((Supplier?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(new CreatePurchaseInvoiceRequest { SupplierId = 99 }));

            Assert.Equal("SUPPLIER_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldRejectInactiveSupplier()
        {
            _repoMock.Setup(r => r.GetSupplierAsync(1)).ReturnsAsync(new Supplier { Id = 1, IsActive = false });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(new CreatePurchaseInvoiceRequest { SupplierId = 1 }));

            Assert.Equal("SUPPLIER_INACTIVE", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateDraftInvoice()
        {
            var invoice = DraftInvoice();
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetSupplierAsync(2)).ReturnsAsync(new Supplier { Id = 2, Name = "New", IsActive = true });
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoice);

            await _service.UpdateAsync(10, new UpdatePurchaseInvoiceRequest
            {
                SupplierId = 2,
                ExternalInvoiceNumber = " EXT ",
                Notes = " Notes "
            });

            Assert.Equal(2, invoice.SupplierId);
            Assert.Equal("EXT", invoice.ExternalInvoiceNumber);
            Assert.Equal("Notes", invoice.Notes);
            _repoMock.Verify(r => r.UpdateAsync(invoice), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldRecordPurchaseUpdate()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.TotalUsd = 6m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetSupplierAsync(2)).ReturnsAsync(new Supplier { Id = 2, Name = "New", IsActive = true });
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoice);

            await _service.UpdateAsync(10, new UpdatePurchaseInvoiceRequest { SupplierId = 2 });

            VerifyAudit("PURCHASE_UPDATE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"SupplierId\":2") &&
                JsonContains(metadata, "\"Status\":\"Draft\"") &&
                JsonContains(metadata, "\"TotalUsd\":6") &&
                JsonContains(metadata, "\"lineCount\":1"));
        }

        [Fact]
        public async Task UpdateAsync_ShouldRejectNonDraftInvoice()
        {
            var invoice = DraftInvoice();
            invoice.Status = PurchaseInvoiceStatus.Completed;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateAsync(10, new UpdatePurchaseInvoiceRequest { SupplierId = 1 }));

            Assert.Equal("PURCHASE_INVOICE_NOT_DRAFT", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteDraftInvoiceAndLines()
        {
            var invoice = DraftInvoice();
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);

            var result = await _service.DeleteAsync(10);

            Assert.Equal("DELETED", result.Action);
            _repoMock.Verify(r => r.DeleteLinesAsync(10), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(invoice), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRecordPurchaseDelete()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.TotalUsd = 6m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);

            await _service.DeleteAsync(10);

            VerifyAudit("PURCHASE_DELETE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"InvoiceNumber\":\"PI-20260519-000001\"") &&
                JsonContains(metadata, "\"SupplierId\":1") &&
                JsonContains(metadata, "\"status\":\"Draft\"") &&
                JsonContains(metadata, "\"TotalUsd\":6") &&
                JsonContains(metadata, "\"lineCount\":1"));
        }

        [Fact]
        public async Task AddLineAsync_ShouldAddLineAndRecalculateTotals()
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: true));
            _repoMock.Setup(r => r.GetNextLineSortOrderAsync(10)).ReturnsAsync(1);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(DraftInvoice());

            await _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest
            {
                ProductId = 5,
                Quantity = 3,
                UnitCostUsd = 2,
                ExpiryDate = DateTime.UtcNow.Date.AddDays(30),
                Notes = " line "
            });

            _repoMock.Verify(r => r.AddLineAsync(It.Is<PurchaseInvoiceLine>(line =>
                line.PurchaseInvoiceId == 10 &&
                line.ProductId == 5 &&
                line.Quantity == 3m &&
                line.UnitCostUsd == 2m &&
                line.LineTotalUsd == 6m &&
                line.SortOrder == 1 &&
                line.Notes == "line")), Times.Once);
            _repoMock.Verify(r => r.RecalculateTotalsAsync(10), Times.Once);
        }

        [Fact]
        public async Task AddLineAsync_ShouldRecordPurchaseLineCreate()
        {
            var invoiceAfter = DraftInvoiceWithLine();
            invoiceAfter.TotalUsd = 6m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: true));
            _repoMock.Setup(r => r.GetNextLineSortOrderAsync(10)).ReturnsAsync(1);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoiceAfter);

            await _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest
            {
                ProductId = 5,
                Quantity = 3,
                UnitCostUsd = 2,
                ExpiryDate = DateTime.UtcNow.Date.AddDays(30)
            });

            VerifyAudit("PURCHASE_LINE_CREATE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"productId\":5") &&
                JsonContains(metadata, "\"quantity\":3") &&
                JsonContains(metadata, "\"unitCostUsd\":2") &&
                JsonContains(metadata, "\"lineCount\":1") &&
                JsonContains(metadata, "\"TotalUsd\":6"));
        }

        [Fact]
        public async Task UpdateLineAsync_ShouldUpdateLineAndRecalculateTotals()
        {
            var line = new PurchaseInvoiceLine
            {
                Id = 7,
                PurchaseInvoiceId = 10,
                ProductId = 5,
                Product = ActiveProduct(hasExpiry: true)
            };
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetLineAsync(10, 7)).ReturnsAsync(line);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(DraftInvoice());

            await _service.UpdateLineAsync(10, 7, new UpdatePurchaseInvoiceLineRequest
            {
                Quantity = 4,
                UnitCostUsd = 1.5m,
                ExpiryDate = DateTime.UtcNow.Date.AddDays(30)
            });

            Assert.Equal(4m, line.Quantity);
            Assert.Equal(1.5m, line.UnitCostUsd);
            Assert.Equal(6m, line.LineTotalUsd);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _repoMock.Verify(r => r.RecalculateTotalsAsync(10), Times.Once);
        }

        [Fact]
        public async Task UpdateLineAsync_ShouldRecordPurchaseLineUpdate()
        {
            var line = new PurchaseInvoiceLine
            {
                Id = 7,
                PurchaseInvoiceId = 10,
                ProductId = 5,
                Product = ActiveProduct(hasExpiry: true)
            };
            var invoiceAfter = DraftInvoiceWithLine();
            invoiceAfter.TotalUsd = 6m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetLineAsync(10, 7)).ReturnsAsync(line);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoiceAfter);

            await _service.UpdateLineAsync(10, 7, new UpdatePurchaseInvoiceLineRequest
            {
                Quantity = 4,
                UnitCostUsd = 1.5m,
                ExpiryDate = DateTime.UtcNow.Date.AddDays(30)
            });

            VerifyAudit("PURCHASE_LINE_UPDATE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"productId\":5") &&
                JsonContains(metadata, "\"quantity\":4") &&
                JsonContains(metadata, "\"unitCostUsd\":1.5") &&
                JsonContains(metadata, "\"lineCount\":1") &&
                JsonContains(metadata, "\"TotalUsd\":6"));
        }

        [Fact]
        public async Task DeleteLineAsync_ShouldDeleteLineAndRecalculateTotals()
        {
            var line = new PurchaseInvoiceLine { Id = 7, PurchaseInvoiceId = 10 };
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetLineAsync(10, 7)).ReturnsAsync(line);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(DraftInvoice());

            var result = await _service.DeleteLineAsync(10, 7);

            Assert.True(result.Success);
            _repoMock.Verify(r => r.DeleteLineAsync(line), Times.Once);
            _repoMock.Verify(r => r.RecalculateTotalsAsync(10), Times.Once);
        }

        [Fact]
        public async Task DeleteLineAsync_ShouldRecordPurchaseLineDelete()
        {
            var line = new PurchaseInvoiceLine { Id = 7, PurchaseInvoiceId = 10, ProductId = 5 };
            var invoiceAfter = DraftInvoice();
            invoiceAfter.TotalUsd = 0m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetLineAsync(10, 7)).ReturnsAsync(line);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoiceAfter);

            await _service.DeleteLineAsync(10, 7);

            VerifyAudit("PURCHASE_LINE_DELETE", metadata =>
                JsonContains(metadata, "\"purchaseInvoiceId\":10") &&
                JsonContains(metadata, "\"productId\":5") &&
                JsonContains(metadata, "\"lineId\":7") &&
                JsonContains(metadata, "\"lineCount\":0") &&
                JsonContains(metadata, "\"TotalUsd\":0"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AddLineAsync_ShouldRejectInvalidQuantity(decimal quantity)
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: false));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest { ProductId = 5, Quantity = quantity, UnitCostUsd = 1 }));

            Assert.Equal("INVALID_QUANTITY", exception.Message);
        }

        [Fact]
        public async Task AddLineAsync_ShouldRejectInvalidUnitCost()
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: false));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest { ProductId = 5, Quantity = 1, UnitCostUsd = -1 }));

            Assert.Equal("INVALID_UNIT_COST", exception.Message);
        }

        [Fact]
        public async Task AddLineAsync_ShouldRequireExpiryForExpiringProduct()
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: true));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest { ProductId = 5, Quantity = 1, UnitCostUsd = 1 }));

            Assert.Equal("EXPIRY_DATE_REQUIRED", exception.Message);
        }

        [Fact]
        public async Task AddLineAsync_ShouldRejectExpiryForNonExpiringProduct()
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync(ActiveProduct(hasExpiry: false));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest
                {
                    ProductId = 5,
                    Quantity = 1,
                    UnitCostUsd = 1,
                    ExpiryDate = DateTime.UtcNow.Date.AddDays(30)
                }));

            Assert.Equal("EXPIRY_DATE_NOT_ALLOWED", exception.Message);
        }

        [Fact]
        public async Task AddLineAsync_ShouldRejectMissingProduct()
        {
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(DraftInvoice());
            _repoMock.Setup(r => r.GetProductAsync(5)).ReturnsAsync((Product?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddLineAsync(10, new CreatePurchaseInvoiceLineRequest { ProductId = 5, Quantity = 1, UnitCostUsd = 1 }));

            Assert.Equal("PRODUCT_NOT_FOUND", exception.Message);
        }

        [Fact]
        public async Task LookupProductsAsync_ShouldReturnLookupItems()
        {
            _repoMock.Setup(r => r.LookupProductsAsync("milk", 20)).ReturnsAsync(new List<Product>
            {
                new Product { Id = 1, Name = "Milk", Barcode = "111", PriceUsd = 2, HasExpiry = true, BaseUnit = "pcs", IsActive = true }
            });

            var result = await _service.LookupProductsAsync("milk");

            var item = Assert.Single(result.Items);
            Assert.Equal(1, item.ProductId);
            Assert.Equal("Milk", item.Name);
            Assert.True(item.HasExpiry);
        }

        [Fact]
        public async Task CompleteAsync_ShouldCreateBatchesAndMarkInvoiceCompleted()
        {
            var invoice = DraftInvoiceWithLine();
            PurchaseInvoice? savedInvoice = null;
            List<ProductBatch>? createdBatches = null;

            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.HasAnyBatchForPurchaseInvoiceLinesAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.AddProductBatchesAsync(It.IsAny<IEnumerable<ProductBatch>>()))
                .Callback<IEnumerable<ProductBatch>>(batches => createdBatches = batches.ToList())
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.SaveChangesAsync())
                .Callback(() => savedInvoice = invoice)
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(() =>
            {
                invoice.CompletedByEmployee = new Employee { Id = 42, FullName = "Cashier" };
                return invoice;
            });

            var result = await _service.CompleteAsync(10);

            Assert.Equal("Completed", result.Status);
            Assert.NotNull(result.CompletedAt);
            Assert.Equal(42, result.CompletedByEmployeeId);
            Assert.Equal(PurchaseInvoiceStatus.Completed, savedInvoice!.Status);
            Assert.Equal(6m, savedInvoice.SubtotalUsd);
            Assert.Equal(6m, savedInvoice.TotalUsd);

            var batch = Assert.Single(createdBatches!);
            Assert.Equal(5, batch.ProductId);
            Assert.Equal(3m, batch.QuantityReceived);
            Assert.Equal(3m, batch.QuantityAvailable);
            Assert.Equal(2m, batch.UnitCostUsd);
            Assert.Equal(77, batch.PurchaseInvoiceLineId);
            Assert.Equal(42, batch.EnteredByEmployeeId);
            Assert.Equal("EXT-1", batch.EntryInvoiceNumber);
            Assert.NotNull(batch.EntryDate);
            _auditLogMock.Verify(a => a.RecordAsync(
                "COMPLETE_PURCHASE",
                "PurchaseInvoice",
                "10",
                "PI-20260519-000001",
                null,
                null,
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CompleteAsync_ShouldNotDuplicateCompletePurchaseAudit()
        {
            var invoice = DraftInvoiceWithLine();
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.HasAnyBatchForPurchaseInvoiceLinesAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoice);

            await _service.CompleteAsync(10);

            _auditLogMock.Verify(a => a.RecordAsync(
                "COMPLETE_PURCHASE",
                "PurchaseInvoice",
                "10",
                "PI-20260519-000001",
                null,
                null,
                It.IsAny<object>()), Times.Once);
            VerifyAuditNever("PURCHASE_CREATE");
            VerifyAuditNever("PURCHASE_UPDATE");
            VerifyAuditNever("PURCHASE_DELETE");
            VerifyAuditNever("PURCHASE_LINE_CREATE");
            VerifyAuditNever("PURCHASE_LINE_UPDATE");
            VerifyAuditNever("PURCHASE_LINE_DELETE");
        }

        [Fact]
        public async Task PurchaseOperation_ShouldSucceed_WhenAuditThrows()
        {
            SetupCreateDraftInvoice();
            _auditLogMock
                .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<object?>()))
                .ThrowsAsync(new Exception("audit failed"));

            var result = await _service.CreateAsync(new CreatePurchaseInvoiceRequest { SupplierId = 1 });

            Assert.Equal(10, result.Id);
        }

        [Fact]
        public async Task AuditMetadata_ShouldNotContainFullLinesCollection()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.TotalUsd = 6m;
            _repoMock.Setup(r => r.GetByIdForUpdateAsync(10)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetSupplierAsync(2)).ReturnsAsync(new Supplier { Id = 2, Name = "New", IsActive = true });
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(invoice);
            object? metadata = null;
            _auditLogMock
                .Setup(a => a.RecordAsync("PURCHASE_UPDATE", "PurchaseInvoice", "10", "PI-20260519-000001", null, null, It.IsAny<object?>()))
                .Callback<string, string, string?, string?, object?, object?, object?>((_, _, _, _, _, _, capturedMetadata) => metadata = capturedMetadata);

            await _service.UpdateAsync(10, new UpdatePurchaseInvoiceRequest { SupplierId = 2 });

            var serialized = JsonSerializer.Serialize(metadata);
            Assert.DoesNotContain("Lines", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ProductName", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Barcode", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectEmptyInvoice()
        {
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(DraftInvoice());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("PURCHASE_INVOICE_HAS_NO_LINES", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectAlreadyCompletedInvoice()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.Status = PurchaseInvoiceStatus.Completed;
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("PURCHASE_INVOICE_ALREADY_COMPLETED", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectNonDraftInvoice()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.Status = PurchaseInvoiceStatus.Cancelled;
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("PURCHASE_INVOICE_NOT_DRAFT", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectSecondCompletionWhenBatchAlreadyExists()
        {
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(DraftInvoiceWithLine());
            _repoMock.Setup(r => r.HasAnyBatchForPurchaseInvoiceLinesAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("PURCHASE_INVOICE_ALREADY_COMPLETED", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectInactiveProduct()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.Lines[0].Product!.IsActive = false;
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("PRODUCT_INACTIVE", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRequireExpiryForExpiringProduct()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.Lines[0].Product!.HasExpiry = true;
            invoice.Lines[0].ExpiryDate = null;
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("EXPIRY_DATE_REQUIRED", exception.Message);
        }

        [Fact]
        public async Task CompleteAsync_ShouldRejectExpiryForNonExpiringProduct()
        {
            var invoice = DraftInvoiceWithLine();
            invoice.Lines[0].Product!.HasExpiry = false;
            invoice.Lines[0].ExpiryDate = DateTime.UtcNow.Date.AddDays(30);
            _repoMock.Setup(r => r.GetByIdWithDetailsForCompletionAsync(10)).ReturnsAsync(invoice);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(10));

            Assert.Equal("EXPIRY_DATE_NOT_ALLOWED", exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowWhenInvoiceMissing()
        {
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((PurchaseInvoice?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetByIdAsync(99));

            Assert.Equal("PURCHASE_INVOICE_NOT_FOUND", exception.Message);
        }

        private static Supplier ActiveSupplier()
        {
            return new Supplier { Id = 1, Name = "Supplier", IsActive = true };
        }

        private static Product ActiveProduct(bool hasExpiry)
        {
            return new Product
            {
                Id = 5,
                Name = "Product",
                Barcode = "P5",
                BaseUnit = "pcs",
                PriceUsd = 3,
                HasExpiry = hasExpiry,
                IsActive = true
            };
        }

        private static PurchaseInvoice DraftInvoice()
        {
            return new PurchaseInvoice
            {
                Id = 10,
                InvoiceNumber = "PI-20260519-000001",
                SupplierId = 1,
                Supplier = ActiveSupplier(),
                CreatedByEmployeeId = 42,
                CreatedByEmployee = new Employee { Id = 42, FullName = "Cashier" },
                Status = PurchaseInvoiceStatus.Draft,
                Lines = new List<PurchaseInvoiceLine>()
            };
        }

        private static PurchaseInvoice DraftInvoiceWithLine()
        {
            var invoice = DraftInvoice();
            invoice.ExternalInvoiceNumber = "EXT-1";
            invoice.Lines = new List<PurchaseInvoiceLine>
            {
                new PurchaseInvoiceLine
                {
                    Id = 77,
                    PurchaseInvoiceId = 10,
                    ProductId = 5,
                    Product = ActiveProduct(hasExpiry: true),
                    Quantity = 3m,
                    UnitCostUsd = 2m,
                    LineTotalUsd = 5m,
                    ExpiryDate = DateTime.UtcNow.Date.AddDays(30),
                    SortOrder = 1
                }
            };
            return invoice;
        }

        private void SetupCreateDraftInvoice()
        {
            var supplier = ActiveSupplier();
            _repoMock.Setup(r => r.GetSupplierAsync(1)).ReturnsAsync(supplier);
            _repoMock.Setup(r => r.GetInvoiceCountForDateAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseInvoice>())).ReturnsAsync((PurchaseInvoice invoice) =>
            {
                invoice.Id = 10;
                invoice.Supplier = supplier;
                invoice.CreatedByEmployee = new Employee { Id = 42, FullName = "Cashier" };
                return invoice;
            });
            _repoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync((long _) => DraftInvoice());
        }

        private void VerifyAudit(string action, Func<object, bool> metadata)
        {
            _auditLogMock.Verify(a => a.RecordAsync(
                action,
                "PurchaseInvoice",
                "10",
                "PI-20260519-000001",
                null,
                null,
                It.Is<object>(value => metadata(value))), Times.Once);
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
