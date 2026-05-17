using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.InvoicesQuery.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    [RequireActiveSession]
    [RequireScreenPermission("Invoices")]
    public class InvoicesQueryController : ControllerBase
    {
        private readonly IInvoicesQueryService _service;

        public InvoicesQueryController(IInvoicesQueryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices(
            [FromQuery] string? status,
            [FromQuery] long? employeeId,
            [FromQuery] string? customerName,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] bool? hasAdjustmentRequest,
            [FromQuery] string? adjustmentRequestStatus,
            [FromQuery] bool? manualPriceEdited,
            [FromQuery] TimeSpan? timeFrom,
            [FromQuery] TimeSpan? timeTo,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var response = await _service.GetInvoicesAsync(
                    status, employeeId, customerName, dateFrom, dateTo,
                    hasAdjustmentRequest, adjustmentRequestStatus, manualPriceEdited, 
                    timeFrom, timeTo, sortBy, sortOrder, page, pageSize);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "INVALID_STATUS_FILTER" || ex.Message == "INVALID_ADJUSTMENT_STATUS_FILTER")
                {
                    return BadRequest(new { error = ex.Message });
                }
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetInvoiceSummary(long invoiceId)
        {
            try
            {
                var response = await _service.GetInvoiceSummaryAsync(invoiceId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "INVOICE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{invoiceId}/details")]
        public async Task<IActionResult> GetInvoiceDetails(long invoiceId)
        {
            try
            {
                var response = await _service.GetInvoiceDetailsAsync(invoiceId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "INVOICE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
