using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.PurchaseInvoices.Interfaces;
using Supermarket.Contracts.PurchaseInvoices;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/purchase-invoices")]
    [RequireActiveSession]
    [RequireScreenPermission("Purchases")]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly IPurchaseInvoiceService _purchaseInvoiceService;

        public PurchaseInvoicesController(IPurchaseInvoiceService purchaseInvoiceService)
        {
            _purchaseInvoiceService = purchaseInvoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _purchaseInvoiceService.GetAllAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var response = await _purchaseInvoiceService.GetByIdAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceRequest request)
        {
            try
            {
                var response = await _purchaseInvoiceService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePurchaseInvoiceRequest request)
        {
            try
            {
                var response = await _purchaseInvoiceService.UpdateAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _purchaseInvoiceService.DeleteAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpPost("{id}/lines")]
        public async Task<IActionResult> AddLine(long id, [FromBody] CreatePurchaseInvoiceLineRequest request)
        {
            try
            {
                var response = await _purchaseInvoiceService.AddLineAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpPut("{id}/lines/{lineId}")]
        public async Task<IActionResult> UpdateLine(long id, long lineId, [FromBody] UpdatePurchaseInvoiceLineRequest request)
        {
            try
            {
                var response = await _purchaseInvoiceService.UpdateLineAsync(id, lineId, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpDelete("{id}/lines/{lineId}")]
        public async Task<IActionResult> DeleteLine(long id, long lineId)
        {
            try
            {
                var response = await _purchaseInvoiceService.DeleteLineAsync(id, lineId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return MapError(ex);
            }
        }

        [HttpGet("products-lookup")]
        public async Task<IActionResult> ProductsLookup([FromQuery] string? search)
        {
            var response = await _purchaseInvoiceService.LookupProductsAsync(search);
            return Ok(response);
        }

        private IActionResult MapError(InvalidOperationException ex)
        {
            return ex.Message switch
            {
                "PURCHASE_INVOICE_NOT_FOUND" => NotFound(new { error = ex.Message }),
                "PURCHASE_INVOICE_LINE_NOT_FOUND" => NotFound(new { error = ex.Message }),
                "SUPPLIER_NOT_FOUND" => NotFound(new { error = ex.Message }),
                "PRODUCT_NOT_FOUND" => NotFound(new { error = ex.Message }),
                "PURCHASE_INVOICE_NUMBER_ALREADY_EXISTS" => Conflict(new { error = ex.Message }),
                "PURCHASE_INVOICE_NOT_DRAFT" => Conflict(new { error = ex.Message }),
                "SUPPLIER_INACTIVE" => Conflict(new { error = ex.Message }),
                "PRODUCT_INACTIVE" => Conflict(new { error = ex.Message }),
                _ => BadRequest(new { error = ex.Message })
            };
        }
    }
}
