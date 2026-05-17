using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.ProductBatches.Interfaces;
using Supermarket.Contracts.ProductBatches;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/batches")]
    [RequireActiveSession]
    [RequireScreenPermission("Products")]
    public class ProductBatchesController : ControllerBase
    {
        private readonly IProductBatchService _batchService;

        public ProductBatchesController(IProductBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllByProductId(long productId)
        {
            try
            {
                var response = await _batchService.GetAllByProductIdAsync(productId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(long productId, [FromBody] CreateBatchRequest request)
        {
            try
            {
                var response = await _batchService.CreateAsync(productId, request);
                // The URL for "GetById" is not defined purely in scope, returning 201 Created with the object.
                return Created($"/api/products/{productId}/batches/{response.Id}", response); 
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "NO_ACTIVE_SESSION") return Unauthorized(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
