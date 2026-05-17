using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Reports.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [RequireActiveSession]
    [RequireScreenPermission("Reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _service;

        public ReportsController(IReportsService service)
        {
            _service = service;
        }

        // --- Sales Reports ---

        [HttpGet("sales/invoices")]
        public async Task<IActionResult> GetSalesInvoices([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? status)
        {
            return Ok(await _service.GetSalesInvoicesAsync(dateFrom, dateTo, status));
        }

        [HttpGet("sales/items")]
        public async Task<IActionResult> GetSalesItems([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetSalesItemsAsync(dateFrom, dateTo));
        }

        [HttpGet("sales/charts")]
        public async Task<IActionResult> GetSalesCharts([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetSalesChartsAsync(dateFrom, dateTo));
        }

        // --- Products Reports ---

        [HttpGet("products/summary")]
        public async Task<IActionResult> GetProductsSummary([FromQuery] long? categoryId)
        {
            return Ok(await _service.GetProductsSummaryAsync(categoryId));
        }

        [HttpGet("products/movements")]
        public async Task<IActionResult> GetProductsMovements([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] long? productId)
        {
            return Ok(await _service.GetProductsMovementsAsync(dateFrom, dateTo, productId));
        }

        [HttpGet("products/charts")]
        public async Task<IActionResult> GetProductsCharts([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetProductsChartsAsync(dateFrom, dateTo));
        }

        // --- Employees Reports ---

        [HttpGet("employees/summary")]
        public async Task<IActionResult> GetEmployeesSummary([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetEmployeesSummaryAsync(dateFrom, dateTo));
        }

        [HttpGet("employees/activity")]
        public async Task<IActionResult> GetEmployeesActivity([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] long? employeeId)
        {
            return Ok(await _service.GetEmployeesActivityAsync(dateFrom, dateTo, employeeId));
        }

        [HttpGet("employees/charts")]
        public async Task<IActionResult> GetEmployeesCharts([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetEmployeesChartsAsync(dateFrom, dateTo));
        }

        // --- Inventory Reports ---

        [HttpGet("inventory/summary")]
        public async Task<IActionResult> GetInventorySummary([FromQuery] long? categoryId)
        {
            return Ok(await _service.GetInventorySummaryAsync(categoryId));
        }

        [HttpGet("inventory/batches")]
        public async Task<IActionResult> GetInventoryBatches([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            return Ok(await _service.GetInventoryBatchesAsync(dateFrom, dateTo));
        }

        [HttpGet("inventory/charts")]
        public async Task<IActionResult> GetInventoryCharts()
        {
            return Ok(await _service.GetInventoryChartsAsync());
        }

        // --- Expiry Reports ---

        [HttpGet("expiry/summary")]
        public async Task<IActionResult> GetExpirySummary()
        {
            return Ok(await _service.GetExpirySummaryAsync());
        }

        [HttpGet("expiry/batches")]
        public async Task<IActionResult> GetExpiryBatches()
        {
            return Ok(await _service.GetExpiryBatchesAsync());
        }

        [HttpGet("expiry/charts")]
        public async Task<IActionResult> GetExpiryCharts()
        {
            return Ok(await _service.GetExpiryChartsAsync());
        }
    }
}
