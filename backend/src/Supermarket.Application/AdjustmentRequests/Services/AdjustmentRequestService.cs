using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.AdjustmentRequests;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.AdjustmentRequests.Services
{
    public class AdjustmentRequestService : IAdjustmentRequestService
    {
        private readonly IAdjustmentRequestRepository _repository;
        private readonly ISessionContext _sessionContext;

        public AdjustmentRequestService(IAdjustmentRequestRepository repository, ISessionContext sessionContext)
        {
            _repository = repository;
            _sessionContext = sessionContext;
        }

        public async Task<AdjustmentRequestResponseDto> CreateAdjustmentRequestAsync(long invoiceId, CreateAdjustmentRequestDto requestDto)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var invoice = await _repository.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null) throw new InvalidOperationException("INVOICE_NOT_FOUND");

                if (invoice.Status != InvoiceStatus.Completed && invoice.Status != InvoiceStatus.Modified)
                    throw new InvalidOperationException("INVALID_INVOICE_STATUS");

                if (await _repository.HasPendingRequestAsync(invoiceId))
                    throw new InvalidOperationException("PENDING_REQUEST_EXISTS");

                if (await _repository.HasRejectedRequestAsync(invoiceId))
                    throw new InvalidOperationException("NO_FURTHER_ADJUSTMENTS_ALLOWED");

                if (!Enum.TryParse<AdjustmentRequestType>(requestDto.RequestType, out var requestType))
                    throw new InvalidOperationException("INVALID_REQUEST_TYPE");

                if (requestType == AdjustmentRequestType.CancelInvoice)
                {
                    if (requestDto.Lines != null && requestDto.Lines.Count > 0)
                        throw new InvalidOperationException("LINES_NOT_ALLOWED");
                }
                else
                {
                    if (requestDto.Lines == null || requestDto.Lines.Count == 0)
                        throw new InvalidOperationException("LINES_REQUIRED");
                }

                var request = new AdjustmentRequest
                {
                    InvoiceId = invoiceId,
                    RequestedByEmployeeId = _sessionContext.EmployeeId,
                    RequestType = requestType,
                    Reason = requestDto.Reason ?? string.Empty,
                    Status = AdjustmentRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                var lines = new List<AdjustmentRequestLine>();

                if (requestType != AdjustmentRequestType.CancelInvoice && requestDto.Lines != null)
                {
                    var invoiceLines = await _repository.GetInvoiceLinesAsync(invoiceId);
                    
                    if (!Enum.TryParse<AdjustmentLineActionType>(requestDto.RequestType, out var actionType))
                    {
                         // Should not happen as RequestType is already validated and names match for line types
                         throw new InvalidOperationException("INVALID_REQUEST_TYPE");
                    }

                    foreach (var lineDto in requestDto.Lines)
                    {
                        var invLine = invoiceLines.FirstOrDefault(l => l.Id == lineDto.InvoiceLineId);
                        if (invLine == null) throw new InvalidOperationException("LINE_NOT_FOUND");

                        lines.Add(new AdjustmentRequestLine
                        {
                            InvoiceLineId = lineDto.InvoiceLineId,
                            ActionType = actionType,
                            RequestedQuantity = lineDto.RequestedQuantity,
                            RequestedLineTotalUsd = lineDto.RequestedLineTotalUsd
                        });
                    }
                }

                invoice.HasAdjustmentRequest = true;
                await _repository.UpdateInvoiceAsync(invoice);

                var createdRequest = await _repository.CreateRequestAsync(request, lines);

                await _repository.CommitTransactionAsync();
                
                return MapToDto(createdRequest, lines);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<AdjustmentRequestResponseDto> ApproveAdjustmentRequestAsync(long invoiceId, long requestId)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var request = await _repository.GetRequestByIdAsync(requestId);
                if (request == null) throw new InvalidOperationException("ADJUSTMENT_NOT_FOUND");
                if (request.InvoiceId != invoiceId) throw new InvalidOperationException("ADJUSTMENT_INVOICE_MISMATCH");
                if (request.Status != AdjustmentRequestStatus.Pending) throw new InvalidOperationException("INVALID_ADJUSTMENT_STATUS");

                var invoice = await _repository.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null) throw new InvalidOperationException("INVOICE_NOT_FOUND");

                var lines = await _repository.GetRequestLinesAsync(requestId);
                var invoiceLines = await _repository.GetInvoiceLinesAsync(invoiceId);

                request.ReviewedByEmployeeId = _sessionContext.EmployeeId;
                request.ReviewedAt = DateTime.UtcNow;
                request.Status = AdjustmentRequestStatus.Approved;

                bool recalculateTotal = false;

                switch (request.RequestType)
                {
                    case AdjustmentRequestType.CancelInvoice:
                        await _repository.ReleaseAllInvoiceAllocationsAsync(invoiceId);
                        invoice.Status = InvoiceStatus.Cancelled;
                        invoice.TotalUsd = 0;
                        if (invoice.ExchangeRateSypSnapshot.HasValue)
                            invoice.TotalSyp = 0;
                        break;

                    case AdjustmentRequestType.DeleteLine:
                        foreach (var rLine in lines)
                        {
                            var invLine = invoiceLines.First(l => l.Id == rLine.InvoiceLineId);
                            await _repository.ReleaseInvoiceLineAllocationsAsync(invLine.Id);
                            invLine.LineTotalUsdEffective = 0;
                            await _repository.UpdateInvoiceLineAsync(invLine);
                        }
                        invoice.Status = InvoiceStatus.Modified;
                        recalculateTotal = true;
                        break;

                    case AdjustmentRequestType.ChangeQuantity:
                        foreach (var rLine in lines)
                        {
                            var invLine = invoiceLines.First(l => l.Id == rLine.InvoiceLineId);
                            decimal diff = invLine.Quantity - (rLine.RequestedQuantity ?? 0);
                            if (diff > 0)
                            {
                                await _repository.PartiallyReleaseInvoiceLineAllocationsLifoAsync(invLine.Id, diff);
                            }
                            invLine.Quantity = rLine.RequestedQuantity ?? 0;
                            invLine.LineTotalUsdEffective = invLine.Quantity * invLine.UnitPriceUsdOriginal;
                            await _repository.UpdateInvoiceLineAsync(invLine);
                        }
                        invoice.Status = InvoiceStatus.Modified;
                        recalculateTotal = true;
                        break;

                    case AdjustmentRequestType.ChangeLineTotal:
                        foreach (var rLine in lines)
                        {
                            var invLine = invoiceLines.First(l => l.Id == rLine.InvoiceLineId);
                            invLine.LineTotalUsdEffective = rLine.RequestedLineTotalUsd ?? 0;
                            invLine.IsPriceOverridden = true;
                            await _repository.UpdateInvoiceLineAsync(invLine);
                        }
                        invoice.Status = InvoiceStatus.Modified;
                        recalculateTotal = true;
                        break;
                }

                if (recalculateTotal)
                {
                    invoice.TotalUsd = invoiceLines.Sum(l => l.LineTotalUsdEffective);
                    if (invoice.ExchangeRateSypSnapshot.HasValue)
                    {
                        invoice.TotalSyp = invoice.TotalUsd * invoice.ExchangeRateSypSnapshot.Value;
                    }
                }

                await _repository.UpdateRequestAsync(request);
                await _repository.UpdateInvoiceAsync(invoice);

                await _repository.CommitTransactionAsync();
                
                return MapToDto(request, lines);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<AdjustmentRequestResponseDto> RejectAdjustmentRequestAsync(long invoiceId, long requestId)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var request = await _repository.GetRequestByIdAsync(requestId);
                if (request == null) throw new InvalidOperationException("ADJUSTMENT_NOT_FOUND");
                if (request.InvoiceId != invoiceId) throw new InvalidOperationException("ADJUSTMENT_INVOICE_MISMATCH");
                if (request.Status != AdjustmentRequestStatus.Pending) throw new InvalidOperationException("INVALID_ADJUSTMENT_STATUS");

                request.ReviewedByEmployeeId = _sessionContext.EmployeeId;
                request.ReviewedAt = DateTime.UtcNow;
                request.Status = AdjustmentRequestStatus.Rejected;

                await _repository.UpdateRequestAsync(request);

                var lines = await _repository.GetRequestLinesAsync(requestId);

                await _repository.CommitTransactionAsync();

                return MapToDto(request, lines);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<AdjustmentRequestResponseDto?> GetAdjustmentRequestAsync(long invoiceId, long requestId)
        {
            var request = await _repository.GetRequestByIdAsync(requestId);
            if (request == null || request.InvoiceId != invoiceId) return null;

            var lines = await _repository.GetRequestLinesAsync(requestId);
            return MapToDto(request, lines);
        }

        private AdjustmentRequestResponseDto MapToDto(AdjustmentRequest request, List<AdjustmentRequestLine> lines)
        {
            return new AdjustmentRequestResponseDto
            {
                RequestId = request.Id,
                InvoiceId = request.InvoiceId,
                Status = request.Status.ToString(),
                RequestType = request.RequestType.ToString(),
                Reason = request.Reason,
                RequestedByEmployeeId = request.RequestedByEmployeeId,
                ReviewedByEmployeeId = request.ReviewedByEmployeeId,
                CreatedAt = request.CreatedAt,
                ReviewedAt = request.ReviewedAt,
                Lines = (lines ?? new List<AdjustmentRequestLine>()).Select(l => new AdjustmentRequestLineResponseDto
                {
                    InvoiceLineId = l.InvoiceLineId,
                    ActionType = l.ActionType.ToString(),
                    RequestedQuantity = l.RequestedQuantity,
                    RequestedLineTotalUsd = l.RequestedLineTotalUsd
                }).ToList()
            };
        }
    }
}
