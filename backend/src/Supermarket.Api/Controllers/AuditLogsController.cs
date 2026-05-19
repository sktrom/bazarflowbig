using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Contracts.AuditLogs;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [RequireActiveSession]
    [RequireScreenPermission("Settings")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogQueryService _queryService;

        public AuditLogsController(IAuditLogQueryService queryService)
        {
            _queryService = queryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] long? employeeId,
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var response = await _queryService.GetPagedAsync(
                employeeId,
                action,
                entityType,
                dateFrom,
                dateTo,
                page,
                pageSize);

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _queryService.GetByIdAsync(id);
            if (response == null)
            {
                return NotFound(new { error = "AUDIT_LOG_NOT_FOUND" });
            }

            return Ok(response);
        }
    }
}
