using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Application.AdjustmentRequests.Services;
using Supermarket.Application.AuditLogs.Interfaces;
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
        private readonly Mock<IAuditLogService> _auditLogMock = new();

        private AdjustmentRequestService CreateService()
        {
            _sessionMock.Setup(s => s.EmployeeId).Returns(1);
            return new AdjustmentRequestService(_repoMock.Object, _sessionMock.Object, _auditLogMock.Object);
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
        public async Task CreateAdjustmentRequestAsync_ShouldRecordCreateAdjustment()
        {
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1))
                .ReturnsAsync(new Invoice { Id = 1, Status = InvoiceStatus.Completed });
            _repoMock.Setup(r => r.CreateRequestAsync(It.IsAny<AdjustmentRequest>(), It.IsAny<List<AdjustmentRequestLine>>()))
                .ReturnsAsync((AdjustmentRequest request, List<AdjustmentRequestLine> _) =>
                {
                    request.Id = 10;
                    return request;
                });

            var service = CreateService();
            await service.CreateAdjustmentRequestAsync(1, new CreateAdjustmentRequestDto { RequestType = "CancelInvoice" });

            _auditLogMock.Verify(a => a.RecordAsync(
                "CREATE_ADJUSTMENT",
                "AdjustmentRequest",
                "10",
                "CancelInvoice",
                null,
                null,
                It.Is<object>(metadata =>
                    JsonContains(metadata, "\"adjustmentRequestId\":10") &&
                    JsonContains(metadata, "\"InvoiceId\":1") &&
                    JsonContains(metadata, "\"requestType\":\"CancelInvoice\"") &&
                    JsonContains(metadata, "\"status\":\"Pending\"") &&
                    JsonContains(metadata, "\"RequestedByEmployeeId\":1") &&
                    !JsonContains(metadata, "reviewedByEmployeeId"))),
                Times.Once);
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
        public async Task ApproveAdjustmentRequestAsync_ShouldRecordApproveAdjustment()
        {
            var invoice = new Invoice { Id = 1, Status = InvoiceStatus.Completed, TotalUsd = 100 };
            var request = new AdjustmentRequest
            {
                Id = 10,
                InvoiceId = 1,
                RequestedByEmployeeId = 2,
                Status = AdjustmentRequestStatus.Pending,
                RequestType = AdjustmentRequestType.CancelInvoice
            };

            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetRequestLinesAsync(10)).ReturnsAsync(new List<AdjustmentRequestLine>());
            _repoMock.Setup(r => r.GetInvoiceLinesAsync(1)).ReturnsAsync(new List<InvoiceLine>());

            var service = CreateService();
            await service.ApproveAdjustmentRequestAsync(1, 10);

            _auditLogMock.Verify(a => a.RecordAsync(
                "APPROVE_ADJUSTMENT",
                "AdjustmentRequest",
                "10",
                "CancelInvoice",
                null,
                null,
                It.Is<object>(metadata =>
                    JsonContains(metadata, "\"adjustmentRequestId\":10") &&
                    JsonContains(metadata, "\"InvoiceId\":1") &&
                    JsonContains(metadata, "\"requestType\":\"CancelInvoice\"") &&
                    JsonContains(metadata, "\"status\":\"Approved\"") &&
                    JsonContains(metadata, "\"RequestedByEmployeeId\":2") &&
                    JsonContains(metadata, "\"ReviewedByEmployeeId\":1"))),
                Times.Once);
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

        [Fact]
        public async Task RejectAdjustmentRequestAsync_ShouldRecordRejectAdjustment()
        {
            var request = new AdjustmentRequest
            {
                Id = 10,
                InvoiceId = 1,
                RequestedByEmployeeId = 2,
                Status = AdjustmentRequestStatus.Pending,
                RequestType = AdjustmentRequestType.DeleteLine
            };
            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetRequestLinesAsync(10)).ReturnsAsync(new List<AdjustmentRequestLine>());

            var service = CreateService();
            await service.RejectAdjustmentRequestAsync(1, 10);

            _auditLogMock.Verify(a => a.RecordAsync(
                "REJECT_ADJUSTMENT",
                "AdjustmentRequest",
                "10",
                "DeleteLine",
                null,
                null,
                It.Is<object>(metadata =>
                    JsonContains(metadata, "\"adjustmentRequestId\":10") &&
                    JsonContains(metadata, "\"InvoiceId\":1") &&
                    JsonContains(metadata, "\"requestType\":\"DeleteLine\"") &&
                    JsonContains(metadata, "\"status\":\"Rejected\"") &&
                    JsonContains(metadata, "\"RequestedByEmployeeId\":2") &&
                    JsonContains(metadata, "\"ReviewedByEmployeeId\":1"))),
                Times.Once);
        }

        [Fact]
        public async Task AdjustmentOperation_ShouldSucceed_WhenAuditThrows()
        {
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1))
                .ReturnsAsync(new Invoice { Id = 1, Status = InvoiceStatus.Completed });
            _repoMock.Setup(r => r.CreateRequestAsync(It.IsAny<AdjustmentRequest>(), It.IsAny<List<AdjustmentRequestLine>>()))
                .ReturnsAsync((AdjustmentRequest request, List<AdjustmentRequestLine> _) =>
                {
                    request.Id = 10;
                    return request;
                });
            _auditLogMock
                .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<object?>()))
                .ThrowsAsync(new Exception("audit failed"));

            var service = CreateService();
            var result = await service.CreateAdjustmentRequestAsync(1, new CreateAdjustmentRequestDto { RequestType = "CancelInvoice" });

            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task AuditMetadata_ShouldNotContainInvoiceLinesOrAllocations()
        {
            var invoice = new Invoice { Id = 1, Status = InvoiceStatus.Completed, TotalUsd = 100 };
            var request = new AdjustmentRequest
            {
                Id = 10,
                InvoiceId = 1,
                RequestedByEmployeeId = 2,
                Status = AdjustmentRequestStatus.Pending,
                RequestType = AdjustmentRequestType.CancelInvoice
            };
            object? metadata = null;
            _repoMock.Setup(r => r.GetRequestByIdAsync(10)).ReturnsAsync(request);
            _repoMock.Setup(r => r.GetInvoiceByIdAsync(1)).ReturnsAsync(invoice);
            _repoMock.Setup(r => r.GetRequestLinesAsync(10)).ReturnsAsync(new List<AdjustmentRequestLine>
            {
                new AdjustmentRequestLine { InvoiceLineId = 50, ActionType = AdjustmentLineActionType.DeleteLine }
            });
            _repoMock.Setup(r => r.GetInvoiceLinesAsync(1)).ReturnsAsync(new List<InvoiceLine>
            {
                new InvoiceLine { Id = 50, ProductId = 5, Quantity = 2 }
            });
            _auditLogMock
                .Setup(a => a.RecordAsync("APPROVE_ADJUSTMENT", "AdjustmentRequest", "10", "CancelInvoice", null, null, It.IsAny<object?>()))
                .Callback<string, string, string?, string?, object?, object?, object?>((_, _, _, _, _, _, capturedMetadata) => metadata = capturedMetadata);

            var service = CreateService();
            await service.ApproveAdjustmentRequestAsync(1, 10);

            var serialized = JsonSerializer.Serialize(metadata);
            Assert.DoesNotContain("InvoiceLines", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Lines", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Allocations", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Batch", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Product", serialized, StringComparison.OrdinalIgnoreCase);
        }

        private static bool JsonContains(object value, string expected)
            => JsonSerializer.Serialize(value).Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
