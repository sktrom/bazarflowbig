using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.AdjustmentRequests.Interfaces;
using Supermarket.Contracts.AdjustmentRequests;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/invoices/{invoiceId}/adjustment-requests")]
    [RequireActiveSession]
    [RequireScreenPermission("Invoices")]
    public class AdjustmentRequestsController : ControllerBase
    {
        private readonly IAdjustmentRequestService _service;

        public AdjustmentRequestsController(IAdjustmentRequestService service)
        {
            _service = service;
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> Get(long invoiceId, long requestId)
        {
            try
            {
                var response = await _service.GetAdjustmentRequestAsync(invoiceId, requestId);
                if (response == null) return NotFound(new { error = "ADJUSTMENT_NOT_FOUND" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(long invoiceId, [FromBody] CreateAdjustmentRequestDto requestDto)
        {
            try
            {
                var response = await _service.CreateAdjustmentRequestAsync(invoiceId, requestDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "INVOICE_NOT_FOUND" || ex.Message == "LINE_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "PENDING_REQUEST_EXISTS" || ex.Message == "NO_FURTHER_ADJUSTMENTS_ALLOWED") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{requestId}/approve")]
        public async Task<IActionResult> Approve(long invoiceId, long requestId)
        {
            try
            {
                var response = await _service.ApproveAdjustmentRequestAsync(invoiceId, requestId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "ADJUSTMENT_NOT_FOUND" || ex.Message == "INVOICE_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "ADJUSTMENT_INVOICE_MISMATCH") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{requestId}/reject")]
        public async Task<IActionResult> Reject(long invoiceId, long requestId)
        {
            try
            {
                var response = await _service.RejectAdjustmentRequestAsync(invoiceId, requestId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "ADJUSTMENT_NOT_FOUND" || ex.Message == "INVOICE_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "ADJUSTMENT_INVOICE_MISMATCH") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
