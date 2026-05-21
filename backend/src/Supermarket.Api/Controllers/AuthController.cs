using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Auth;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISessionContext _sessionContext;

        public AuthController(IAuthService authService, ISessionContext sessionContext)
        {
            _authService = authService;
            _sessionContext = sessionContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "LOGIN_FAILED")
                    return Unauthorized(new { error = ex.Message });

                if (ex.Message == "LOGIN_THROTTLED")
                    return StatusCode(429, new { error = ex.Message });

                if (ex.Message == "SETUP_REQUIRED")
                    return StatusCode(403, new { error = ex.Message });

                if (ex.Message == "SETUP_STATE_INVALID")
                    return Conflict(new { error = ex.Message });

                if (ex.Message == "DEFAULT_DEVICE_NOT_ALLOWED")
                    return StatusCode(403, new { error = ex.Message });
                    
                if (ex.Message == "EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION")
                    return Conflict(new { error = ex.Message });

                if (ex.Message == "SESSION_START_FAILED")
                    return StatusCode(500, new { error = ex.Message });

                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "SESSION_START_FAILED" });
            }
        }

        [HttpPost("logout")]
        [RequireActiveSession]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var response = await _authService.LogoutAsync(_sessionContext.SessionId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "LOGOUT_BLOCKED_CART_NOT_EMPTY")
                    return Conflict(new { error = ex.Message });
                    
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("me")]
        [RequireActiveSession]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var response = await _authService.GetCurrentEmployeeAsync(_sessionContext.SessionId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("permissions")]
        [RequireActiveSession]
        public async Task<IActionResult> GetPermissions()
        {
            var response = await _authService.GetPermissionsAsync(_sessionContext.EmployeeId);
            return Ok(response);
        }
    }
}
