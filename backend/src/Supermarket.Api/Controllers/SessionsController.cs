using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Sessions.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [RequireActiveSession]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ISessionContext _sessionContext;

        public SessionsController(ISessionService sessionService, ISessionContext sessionContext)
        {
            _sessionService = sessionService;
            _sessionContext = sessionContext;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var response = await _sessionService.GetHistoryAsync(pageIndex, pageSize);
            return Ok(response);
        }

        [HttpGet("active")]
        [RequireScreenPermission("Settings")]
        public async Task<IActionResult> GetActive()
        {
            var sessions = await _sessionService.GetActiveSessionsAsync();
            return Ok(sessions);
        }

        [HttpPost("{sessionId}/force-close")]
        [RequireScreenPermission("Settings")]
        public async Task<IActionResult> ForceClose(long sessionId)
        {
            try
            {
                await _sessionService.ForceCloseSessionAsync(sessionId, _sessionContext.EmployeeId, _sessionContext.SessionId);
                return Ok(new { success = true });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "SESSION_NOT_FOUND" });
            }
            catch (InvalidOperationException ex) when (ex.Message == "SESSION_NOT_ACTIVE")
            {
                return Conflict(new { error = "SESSION_NOT_ACTIVE" });
            }
        }
    }
}
