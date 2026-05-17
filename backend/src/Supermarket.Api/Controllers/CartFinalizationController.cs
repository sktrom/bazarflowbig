using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.CartFinalization.Interfaces;
using Supermarket.Contracts.CartFinalization;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/cashier/cart")]
    [RequireActiveSession]
    [RequireScreenPermission("Sales")]
    public class CartFinalizationController : ControllerBase
    {
        private readonly ICartFinalizationService _finalizationService;

        public CartFinalizationController(ICartFinalizationService finalizationService)
        {
            _finalizationService = finalizationService;
        }

        [HttpPost("suspend")]
        public async Task<IActionResult> Suspend([FromBody] SuspendCartRequest request)
        {
            try
            {
                var response = await _finalizationService.SuspendAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message switch
                {
                    "NO_WORKING_CART_EXISTS" => NotFound(new { error = ex.Message }),
                    "CUSTOMER_NAME_REQUIRED" => BadRequest(new { error = ex.Message }),
                    "INVALID_SUSPENSION_REASON" => BadRequest(new { error = ex.Message }),
                    "INSUFFICIENT_INVENTORY" => Conflict(new { error = ex.Message }),
                    _ => BadRequest(new { error = ex.Message })
                };
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete()
        {
            try
            {
                var response = await _finalizationService.CompleteAsync();
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message switch
                {
                    "NO_WORKING_CART_EXISTS" => NotFound(new { error = ex.Message }),
                    "INSUFFICIENT_INVENTORY" => Conflict(new { error = ex.Message }),
                    "EXCHANGE_RATE_NOT_CONFIGURED" => UnprocessableEntity(new { error = ex.Message }),
                    _ => BadRequest(new { error = ex.Message })
                };
            }
        }

        [HttpDelete("current")]
        public async Task<IActionResult> CancelCurrent()
        {
            try
            {
                var response = await _finalizationService.CancelCurrentAsync();
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message switch
                {
                    "NO_WORKING_CART_EXISTS" => NotFound(new { error = ex.Message }),
                    _ => BadRequest(new { error = ex.Message })
                };
            }
        }

        [HttpPost("load-suspended/{invoiceId:long}")]
        public async Task<IActionResult> LoadSuspended(long invoiceId)
        {
            try
            {
                var response = await _finalizationService.LoadSuspendedAsync(invoiceId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message switch
                {
                    "INVOICE_NOT_FOUND" => NotFound(new { error = ex.Message }),
                    "INVOICE_NOT_SUSPENDED" => BadRequest(new { error = ex.Message }),
                    "WORKING_CART_NOT_EMPTY" => Conflict(new { error = ex.Message }),
                    _ => BadRequest(new { error = ex.Message })
                };
            }
        }
    }
}
