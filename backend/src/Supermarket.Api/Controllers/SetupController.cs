using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Setup;
using Supermarket.Domain.Entities;
using Supermarket.Infrastructure.Persistence;

namespace Supermarket.Api.Controllers
{
    [ApiController]
    [Route("api/setup")]
    public class SetupController : ControllerBase
    {
        private readonly SupermarketDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public SetupController(
            SupermarketDbContext db,
            IPasswordHasher passwordHasher,
            IAuditLogService auditLogService)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            var setupSetting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "setup_completed");
            
            bool completed = setupSetting != null && setupSetting.SettingValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            return Ok(new SetupStatusResponse(completed));
        }

        [HttpPost("complete")]
        [AllowAnonymous]
        public async Task<IActionResult> Complete([FromBody] SetupCompleteRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Request body is required." });
            }

            // check if setup already completed in database
            var setupSetting = await _db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "setup_completed");
            
            bool completed = setupSetting != null && setupSetting.SettingValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (completed)
            {
                return BadRequest(new { error = "SETUP_ALREADY_COMPLETED", message = "System setup has already been completed." });
            }

            // Validation
            if (string.IsNullOrWhiteSpace(request.AdminFullName))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Admin full name is required." });
            
            if (string.IsNullOrWhiteSpace(request.AdminUsername))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Admin username is required." });
            
            if (string.IsNullOrWhiteSpace(request.AdminPassword))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Admin password is required." });
            
            if (request.AdminPassword.Length < 6)
                return BadRequest(new { error = "INVALID_ADMIN_PASSWORD", message = "Password must be at least 6 characters long." });
            
            if (request.AdminPassword.Equals("admin123", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "INVALID_ADMIN_PASSWORD", message = "The password 'admin123' is not secure." });
            
            if (request.AdminPassword.Equals(request.AdminUsername, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "INVALID_ADMIN_PASSWORD", message = "Password cannot be equal to username." });
            
            if (string.IsNullOrWhiteSpace(request.StoreName))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Store name is required." });
            
            if (request.ExchangeRate <= 0)
                return BadRequest(new { error = "INVALID_EXCHANGE_RATE", message = "Exchange rate must be greater than zero." });
            
            if (string.IsNullOrWhiteSpace(request.DeviceCode))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Device code is required." });
            
            if (string.IsNullOrWhiteSpace(request.DeviceName))
                return BadRequest(new { error = "SETUP_VALIDATION_ERROR", message = "Device name is required." });

            var trimmedDeviceCode = request.DeviceCode.Trim();
            var trimmedUsername = request.AdminUsername.Trim();

            // Device code uniqueness check
            var deviceExists = await _db.PosDevices
                .AnyAsync(d => d.DeviceCode.ToLower() == trimmedDeviceCode.ToLower());
            if (deviceExists)
            {
                return BadRequest(new { error = "DEVICE_CODE_ALREADY_EXISTS", message = $"A device with code '{trimmedDeviceCode}' already exists." });
            }

            // Username uniqueness check for other employees
            var usernameExists = await _db.Employees
                .AnyAsync(e => e.Username.ToLower() == trimmedUsername.ToLower() && e.Id != 1);
            if (usernameExists)
            {
                return BadRequest(new { error = "ADMIN_USERNAME_ALREADY_EXISTS", message = $"Username '{trimmedUsername}' is already taken." });
            }

            // Admin strategy: check employees list
            var employees = await _db.Employees.ToListAsync();
            
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                if (employees.Count == 0)
                {
                    // Create first admin
                    var newAdmin = new Employee
                    {
                        FullName = request.AdminFullName.Trim(),
                        Username = trimmedUsername,
                        PasswordHash = _passwordHasher.Hash(request.AdminPassword),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Employees.Add(newAdmin);
                }
                else if (employees.Count == 1 && employees[0].Id == 1)
                {
                    // Update seed admin (Id = 1)
                    var seedAdmin = employees[0];
                    seedAdmin.FullName = request.AdminFullName.Trim();
                    seedAdmin.Username = trimmedUsername;
                    seedAdmin.PasswordHash = _passwordHasher.Hash(request.AdminPassword);
                    seedAdmin.IsActive = true;
                    seedAdmin.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Ambiguous state: multiple employees exist
                    return BadRequest(new { error = "SETUP_STATE_AMBIGUOUS", message = "System database contains existing employees, setup cannot proceed automatically." });
                }

                // Create first custom device
                var newDevice = new PosDevice
                {
                    DeviceCode = trimmedDeviceCode,
                    DeviceName = request.DeviceName.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.PosDevices.Add(newDevice);

                // Update settings keys
                var storeSetting = await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "store_name")
                    ?? await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "StoreName");
                if (storeSetting == null)
                {
                    storeSetting = new AppSetting { SettingKey = "store_name", SettingValue = request.StoreName.Trim(), UpdatedAt = DateTime.UtcNow };
                    _db.AppSettings.Add(storeSetting);
                }
                else
                {
                    storeSetting.SettingValue = request.StoreName.Trim();
                    storeSetting.UpdatedAt = DateTime.UtcNow;
                }

                var rateSetting = await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "exchange_rate_syp")
                    ?? await _db.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == "ExchangeRateSyp");
                if (rateSetting == null)
                {
                    rateSetting = new AppSetting { SettingKey = "exchange_rate_syp", SettingValue = request.ExchangeRate.ToString(System.Globalization.CultureInfo.InvariantCulture), UpdatedAt = DateTime.UtcNow };
                    _db.AppSettings.Add(rateSetting);
                }
                else
                {
                    rateSetting.SettingValue = request.ExchangeRate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    rateSetting.UpdatedAt = DateTime.UtcNow;
                }

                if (setupSetting == null)
                {
                    setupSetting = new AppSetting { SettingKey = "setup_completed", SettingValue = "true", UpdatedAt = DateTime.UtcNow };
                    _db.AppSettings.Add(setupSetting);
                }
                else
                {
                    setupSetting.SettingValue = "true";
                    setupSetting.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                // Audit log registration (best effort)
                try
                {
                    await _auditLogService.RecordAsync(
                        action: "SETUP_COMPLETED",
                        entityType: "System",
                        entityId: "1",
                        entityDisplayName: "System Setup",
                        before: null,
                        after: null,
                        metadata: new
                        {
                            adminUsername = trimmedUsername,
                            storeName = request.StoreName.Trim(),
                            deviceCode = trimmedDeviceCode,
                            exchangeRate = request.ExchangeRate
                        });
                }
                catch
                {
                    // Ignore audit failures
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new SetupCompleteResponse(true, "Setup completed successfully."));
        }
    }
}
