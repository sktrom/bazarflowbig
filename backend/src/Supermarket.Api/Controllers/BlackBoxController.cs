using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.BlackBox.Interfaces;
using Supermarket.Contracts.BlackBox;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/black-box/events")]
    [RequireActiveSession]
    public class BlackBoxController : ControllerBase
    {
        private readonly IBlackBoxEventService _service;

        public BlackBoxController(IBlackBoxEventService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBlackBoxEventRequest request)
        {
            try
            {
                var response = await _service.CreateAsync(
                    request,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].FirstOrDefault());

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "BLACK_BOX_ACTION_TYPE_REQUIRED" || ex.Message == "BLACK_BOX_RESULT_REQUIRED")
                {
                    return BadRequest(new { error = ex.Message });
                }

                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [RequireScreenPermission("BlackBox")]
        public async Task<IActionResult> GetPaged([FromQuery] BlackBoxEventQuery query)
        {
            var response = await _service.GetPagedAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [RequireScreenPermission("BlackBox")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetByIdAsync(id);
            if (response == null)
            {
                return NotFound(new { error = "BLACK_BOX_EVENT_NOT_FOUND" });
            }

            return Ok(response);
        }
    }
}
