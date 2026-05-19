using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Offers.Interfaces;
using Supermarket.Contracts.Offers;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/offers")]
    [RequireActiveSession]
    [RequireScreenPermission("Offers")]
    public class OffersController : ControllerBase
    {
        private readonly IOfferService _offerService;

        public OffersController(IOfferService offerService)
        {
            _offerService = offerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _offerService.GetAllAsync();
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOfferRequest request)
        {
            try
            {
                var response = await _offerService.CreateAsync(request);
                return Created(string.Empty, response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return BadRequest(new { error = ex.Message });
                if (ex.Message == "INVALID_DISCOUNT_TYPE") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOfferRequest request)
        {
            try
            {
                var response = await _offerService.UpdateAsync(id, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "OFFER_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "PRODUCT_NOT_FOUND") return BadRequest(new { error = ex.Message });
                if (ex.Message == "INVALID_DISCOUNT_TYPE") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            try
            {
                var response = await _offerService.CancelAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "OFFER_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var response = await _offerService.DeleteAsync(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "OFFER_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "CANNOT_DELETE_LEGACY_OFFER") return Conflict(new { error = ex.Message });
                if (ex.Message == "CANNOT_DELETE_USED_OFFER") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("products-lookup")]
        public async Task<IActionResult> ProductsLookup([FromQuery] string? search)
        {
            var response = await _offerService.ProductsLookupAsync(search);
            return Ok(response);
        }
    }
}
