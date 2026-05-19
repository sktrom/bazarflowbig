using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supermarket.Api.Filters;
using Supermarket.Application.SystemMaintenance.Interfaces;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/system")]
    [RequireActiveSession]
    [RequireScreenPermission("Settings")]
    public class SystemMaintenanceController : ControllerBase
    {
        private readonly IBackupService _backupService;

        public SystemMaintenanceController(IBackupService backupService)
        {
            _backupService = backupService;
        }

        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var result = await _backupService.CreateBackupAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message switch
                {
                    "BACKUP_DIRECTORY_NOT_CONFIGURED" => BadRequest(new { error = ex.Message }),
                    "BACKUP_PATH_NOT_ACCESSIBLE" => Conflict(new { error = ex.Message }),
                    "BACKUP_SQL_FAILED" => StatusCode(500, new { error = ex.Message }),
                    _ => BadRequest(new { error = ex.Message })
                };
            }
        }
    }
}
