using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Exports.Interfaces;
using Supermarket.Contracts.Exports;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/exports")]
    [RequireActiveSession]
    public class ExportsController : ControllerBase
    {
        private readonly IExportsService _exportsService;

        public ExportsController(IExportsService exportsService)
        {
            _exportsService = exportsService;
        }

        [HttpPost("products")]
        [RequireScreenPermission("Products")]
        public async Task<IActionResult> ExportProducts([FromBody] ExportProductsRequest request)
        {
            var result = await _exportsService.ExportProductsAsync(request);
            return File(result.Data, result.ContentType, result.FileName);
        }

        [HttpPost("invoices")]
        [RequireScreenPermission("Invoices")]
        public async Task<IActionResult> ExportInvoices([FromBody] ExportInvoicesRequest request)
        {
            var result = await _exportsService.ExportInvoicesAsync(request);
            return File(result.Data, result.ContentType, result.FileName);
        }

        [HttpPost("offers")]
        [RequireScreenPermission("Offers")]
        public async Task<IActionResult> ExportOffers([FromBody] ExportOffersRequest request)
        {
            var result = await _exportsService.ExportOffersAsync(request);
            return File(result.Data, result.ContentType, result.FileName);
        }

        [HttpPost("inventory")]
        [RequireScreenPermission("Inventory")]
        public async Task<IActionResult> ExportInventory([FromBody] ExportInventoryRequest request)
        {
            var result = await _exportsService.ExportInventoryAsync(request);
            return File(result.Data, result.ContentType, result.FileName);
        }

        [HttpPost("reports/{reportKey}")]
        [RequireScreenPermission("Reports")]
        public async Task<IActionResult> ExportReport(string reportKey, [FromBody] ExportReportRequest request)
        {
            var result = await _exportsService.ExportReportAsync(reportKey, request);
            return File(result.Data, result.ContentType, result.FileName);
        }
    }
}
