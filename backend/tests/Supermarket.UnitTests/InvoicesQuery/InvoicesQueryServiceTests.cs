using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.InvoicesQuery.Interfaces;
using Supermarket.Application.InvoicesQuery.Services;
using Supermarket.Contracts.InvoicesQuery;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.InvoicesQuery
{
    public class InvoicesQueryServiceTests
    {
        private readonly Mock<IInvoicesQueryRepository> _repoMock = new();

        private InvoicesQueryService CreateService() => new InvoicesQueryService(_repoMock.Object);

        [Fact]
        public async Task GetInvoices_ShouldReturnPaginatedList()
        {
            var invoices = new List<(Invoice Invoice, AdjustmentRequest? LatestAdjustment)>
            {
                (new Invoice { Id = 1, Status = InvoiceStatus.Working }, null),
                (new Invoice { Id = 2, Status = InvoiceStatus.Completed }, null)
            };

            _repoMock.Setup(r => r.GetInvoicesPaginatedAsync(
                null, null, null, null, null, null, null, null, null, null, 1, 20))
                .ReturnsAsync((invoices, 2));

            var service = CreateService();
            var result = await service.GetInvoicesAsync();

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetInvoices_ShouldThrow_WhenStatusFilterIsInvalid()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetInvoicesAsync(status: "INVALID"));

            Assert.Equal("INVALID_STATUS_FILTER", ex.Message);
        }

        [Fact]
        public async Task GetInvoiceSummary_ShouldThrow_WhenInvoiceNotFound()
        {
            _repoMock.Setup(r => r.GetInvoiceSummaryByIdAsync(99))
                .ReturnsAsync((null, null));

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetInvoiceSummaryAsync(99));

            Assert.Equal("INVOICE_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task GetInvoiceDetails_ShouldReturnAdjustmentStatus_WhenExists()
        {
            var invoice = new Invoice
            {
                Id = 1,
                Status = InvoiceStatus.Completed,
                HasAdjustmentRequest = true
            };
            
            var latestAdj = new AdjustmentRequest { Status = AdjustmentRequestStatus.Pending, RequestType = AdjustmentRequestType.ChangeQuantity, CreatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.GetInvoiceDetailsByIdAsync(1))
                .ReturnsAsync((invoice, new List<InvoiceLine>(), latestAdj));

            var service = CreateService();
            var result = await service.GetInvoiceDetailsAsync(1);

            Assert.True(result.HasAdjustmentRequest);
            Assert.Equal("Pending", result.AdjustmentRequestStatus);
            Assert.Equal("ChangeQuantity", result.AdjustmentRequestType);
        }
    }
}
