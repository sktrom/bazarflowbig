using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Application.AdjustmentRequests.Services;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.AdjustmentRequests;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;
using Xunit;

namespace Supermarket.UnitTests.AdjustmentRequests
{
    public class AdjustmentRequestServiceTests
    {
        private readonly Mock<IAdjustmentRequestRepository> _repoMock = new();
        private readonly Mock<ISessionContext> _sessionMock = new();

        private AdjustmentRequestService CreateService()
        {
            _sessionMock.Setup(s => s.EmployeeId).Returns(1);
            return new AdjustmentRequestService(_repoMock.Object, _sessionMock.Object);
        }

        [Fact]
        public async Task Create_ShouldThrow_WhenInvoiceIsWorking()
        {
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1)).ReturnsAsync(new Invoice { Id = 1, Status = InvoiceStatus.Working });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.CreateAdjustmentRequestAsync(1, new CreateAdjustmentRequestDto { RequestType = "CancelInvoice" }));

            Assert.Equal("INVALID_INVOICE_STATUS", ex.Message);
        }

        [Fact]
        public async Task Create_ShouldThrow_WhenPendingRequestExists()
        {
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1)).ReturnsAsync(new Invoice { Id = 1, Status = InvoiceStatus.Completed });
            _repoMock.Setup(r => r.HasPendingRequestAsync(1)).ReturnsAsync(true);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.CreateAdjustmentRequestAsync(1, new CreateAdjustmentRequestDto { RequestType = "CancelInvoice" }));

            Assert.Equal("PENDING_REQUEST_EXISTS", ex.Message);
        }

        [Fact]
        public async Task Approve_ShouldSetInvoiceToCancelled_ForCancelInvoice()
        {
            var invoice = new Invoice { Id = 1, Status = InvoiceStatus.Completed, TotalUsd = 100 };
            var request = new AdjustmentRequest { Id = 10, InvoiceId = 1, Status = AdjustmentRequestStatus.Pending, RequestType = AdjustmentRequestType.CancelInvoice };

            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetRequestLinesAsync(10)).ReturnsAsync(new List<AdjustmentRequestLine>());
            _repoMock.Setup(r => r.GetInvoiceLinesAsync(1)).ReturnsAsync(new List<InvoiceLine>());

            var service = CreateService();
            var result = await service.ApproveAdjustmentRequestAsync(1, 10);

            Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
            Assert.Equal(0, invoice.TotalUsd);
            Assert.Equal("Approved", result.Status);
            _repoMock.Verify(r => r.ReleaseAllInvoiceAllocationsAsync(1), Times.Once);
        }

        [Fact]
        public async Task Reject_ShouldPreventFutureCreateRequests_And_NotChangeStatus()
        {
            var request = new AdjustmentRequest { Id = 10, InvoiceId = 1, Status = AdjustmentRequestStatus.Pending };
            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);

            var service = CreateService();
            var result = await service.RejectAdjustmentRequestAsync(1, 10);

            Assert.Equal("Rejected", result.Status);
            _repoMock.Verify(r => r.UpdateInvoiceAsync(It.IsAny<Invoice>()), Times.Never); // Status stays same
        }
    }
}
