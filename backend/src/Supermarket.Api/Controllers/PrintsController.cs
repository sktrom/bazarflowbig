using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Exports.Interfaces;
using Supermarket.Contracts.Exports;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/prints")]
    [RequireActiveSession]
    public class PrintsController : ControllerBase
    {
        private readonly IExportsService _exportsService;

        public PrintsController(IExportsService exportsService)
        {
            _exportsService = exportsService;
        }

        [HttpPost("reports/{reportKey}")]
        [RequireScreenPermission("Reports")]
        public async Task<IActionResult> PrintReport(string reportKey, [FromBody] PrintReportRequest request)
        {
            var html = await _exportsService.PrintReportAsync(reportKey, request);
            return Content(html, "text/html");
        }
    }
}
