using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.InventoryQueries.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    [RequireActiveSession]
    [RequireScreenPermission("Inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryQueryService _service;

        public InventoryController(IInventoryQueryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetInventoryList(
            [FromQuery] string? search,
            [FromQuery] long? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] bool? hasStock,
            [FromQuery] bool? hasExpiry,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var response = await _service.GetInventoryListAsync(search, categoryId, isActive, hasStock, hasExpiry, page, pageSize);
            return Ok(response);
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetInventoryDetails(long productId)
        {
            try
            {
                var response = await _service.GetInventoryDetailsAsync(productId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
