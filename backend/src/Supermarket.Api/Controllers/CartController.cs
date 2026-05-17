using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.WorkingCart.Interfaces;
using Supermarket.Contracts.WorkingCart;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/cashier/cart")]
    [RequireActiveSession]
    [RequireScreenPermission("Sales")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentCart()
        {
            var response = await _cartService.GetCurrentCartAsync();
            return Ok(response);
        }

        [HttpPost("items/by-barcode")]
        public async Task<IActionResult> AddByBarcode([FromBody] AddByBarcodeRequest request)
        {
            try
            {
                var response = await _cartService.AddByBarcodeAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "MULTIPLE_ACTIVE_OFFERS_FOUND") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("items/by-product")]
        public async Task<IActionResult> AddByProduct([FromBody] AddByProductRequest request)
        {
            try
            {
                var response = await _cartService.AddByProductAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "PRODUCT_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "MULTIPLE_ACTIVE_OFFERS_FOUND") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPatch("items/{lineId}")]
        public async Task<IActionResult> UpdateLine(long lineId, [FromBody] UpdateLineRequest request)
        {
            try
            {
                var response = await _cartService.UpdateLineAsync(lineId, request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "NO_WORKING_CART_EXISTS" || ex.Message == "LINE_NOT_FOUND") return NotFound(new { error = ex.Message });
                if (ex.Message == "INVALID_QUANTITY") return BadRequest(new { error = ex.Message });
                if (ex.Message == "MULTIPLE_ACTIVE_OFFERS_FOUND") return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("items/{lineId}")]
        public async Task<IActionResult> DeleteLine(long lineId)
        {
            try
            {
                var response = await _cartService.DeleteLineAsync(lineId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "NO_WORKING_CART_EXISTS" || ex.Message == "LINE_NOT_FOUND") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPatch("discount")]
        public async Task<IActionResult> UpdateDiscount([FromBody] UpdateDiscountRequest request)
        {
            try
            {
                var response = await _cartService.UpdateDiscountAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "NO_WORKING_CART_EXISTS") return NotFound(new { error = ex.Message });
                if (ex.Message == "INVALID_DISCOUNT_TYPE") return BadRequest(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("customer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerRequest request)
        {
            try
            {
                var response = await _cartService.UpdateCustomerAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "NO_WORKING_CART_EXISTS") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("customer")]
        public async Task<IActionResult> DeleteCustomer()
        {
            try
            {
                var response = await _cartService.DeleteCustomerAsync();
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "NO_WORKING_CART_EXISTS") return NotFound(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
