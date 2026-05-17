using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Sessions.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [RequireActiveSession]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var response = await _sessionService.GetHistoryAsync(pageIndex, pageSize);
            return Ok(response);
        }
    }
}
