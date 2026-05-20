using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Devices.Interfaces;
using Supermarket.Contracts.Devices;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Devices.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepo;
        private readonly IAuditLogService _auditLogService;

        public DeviceService(IDeviceRepository deviceRepo, IAuditLogService auditLogService)
        {
            _deviceRepo = deviceRepo;
            _auditLogService = auditLogService;
        }

        public async Task<List<DeviceListItem>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepo.ListAllAsync();
            return devices.Select(d => new DeviceListItem
            {
                Id = d.Id,
                DeviceCode = d.DeviceCode,
                DeviceName = d.DeviceName,
                IsActive = d.IsActive,
                Notes = d.Notes,
                LastLoginAt = d.LastLoginAt,
                CreatedAt = d.CreatedAt
            }).ToList();
        }

        public async Task<DeviceDetailsResponse?> GetDeviceByIdAsync(long id)
        {
            var d = await _deviceRepo.GetByIdAsync(id);
            if (d == null) return null;

            return new DeviceDetailsResponse
            {
                Id = d.Id,
                DeviceCode = d.DeviceCode,
                DeviceName = d.DeviceName,
                IsActive = d.IsActive,
                Notes = d.Notes,
                LastLoginAt = d.LastLoginAt,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            };
        }

        public async Task<DeviceDetailsResponse> CreateDeviceAsync(CreateDeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceCode))
                throw new ArgumentException("DEVICE_CODE_REQUIRED");

            if (string.IsNullOrWhiteSpace(request.DeviceName))
                throw new ArgumentException("DEVICE_NAME_REQUIRED");

            var existing = await _deviceRepo.GetByCodeAsync(request.DeviceCode.Trim());
            if (existing != null)
                throw new InvalidOperationException("DEVICE_CODE_ALREADY_EXISTS");

            var now = DateTime.UtcNow;
            var device = new PosDevice
            {
                DeviceCode = request.DeviceCode.Trim(),
                DeviceName = request.DeviceName.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _deviceRepo.CreateAsync(device);

            // Audit logging
            try
            {
                await _auditLogService.RecordAsync(
                    "CREATE_DEVICE",
                    "PosDevice",
                    device.Id.ToString(),
                    device.DeviceName,
                    before: null,
                    after: new { device.DeviceCode, device.DeviceName, device.Notes, device.IsActive }
                );
            }
            catch { /* Audit log failure must not block the operation */ }

            return new DeviceDetailsResponse
            {
                Id = device.Id,
                DeviceCode = device.DeviceCode,
                DeviceName = device.DeviceName,
                IsActive = device.IsActive,
                Notes = device.Notes,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            };
        }

        public async Task<DeviceDetailsResponse> UpdateDeviceAsync(long id, UpdateDeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceName))
                throw new ArgumentException("DEVICE_NAME_REQUIRED");

            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null)
                throw new InvalidOperationException("DEVICE_NOT_FOUND");

            var oldName = device.DeviceName;
            var oldNotes = device.Notes;

            device.DeviceName = request.DeviceName.Trim();
            device.Notes = request.Notes?.Trim();
            device.UpdatedAt = DateTime.UtcNow;

            await _deviceRepo.UpdateAsync(device);

            // Audit logging
            try
            {
                await _auditLogService.RecordAsync(
                    "UPDATE_DEVICE",
                    "PosDevice",
                    device.Id.ToString(),
                    device.DeviceName,
                    before: new { DeviceName = oldName, Notes = oldNotes },
                    after: new { device.DeviceName, device.Notes }
                );
            }
            catch { /* Audit log failure must not block the operation */ }

            return new DeviceDetailsResponse
            {
                Id = device.Id,
                DeviceCode = device.DeviceCode,
                DeviceName = device.DeviceName,
                IsActive = device.IsActive,
                Notes = device.Notes,
                LastLoginAt = device.LastLoginAt,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            };
        }

        public async Task<EnableDisableDeviceResponse> EnableDeviceAsync(long id)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null)
                throw new InvalidOperationException("DEVICE_NOT_FOUND");

            if (!device.IsActive)
            {
                device.IsActive = true;
                device.UpdatedAt = DateTime.UtcNow;
                await _deviceRepo.UpdateAsync(device);

                // Audit logging
                try
                {
                    await _auditLogService.RecordAsync(
                        "ENABLE_DEVICE",
                        "PosDevice",
                        device.Id.ToString(),
                        device.DeviceName,
                        before: new { IsActive = false },
                        after: new { IsActive = true }
                    );
                }
                catch { /* Audit log failure must not block the operation */ }
            }

            return new EnableDisableDeviceResponse { Success = true, Message = "DEVICE_ENABLED" };
        }

        public async Task<EnableDisableDeviceResponse> DisableDeviceAsync(long id)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null)
                throw new InvalidOperationException("DEVICE_NOT_FOUND");

            // Prevent disabling the last active device
            var all = await _deviceRepo.ListAllAsync();
            if (all.Count(d => d.IsActive) <= 1 && device.IsActive)
            {
                throw new InvalidOperationException("CANNOT_DISABLE_LAST_ACTIVE_DEVICE");
            }

            if (device.IsActive)
            {
                device.IsActive = false;
                device.UpdatedAt = DateTime.UtcNow;
                await _deviceRepo.UpdateAsync(device);

                // Audit logging
                try
                {
                    await _auditLogService.RecordAsync(
                        "DISABLE_DEVICE",
                        "PosDevice",
                        device.Id.ToString(),
                        device.DeviceName,
                        before: new { IsActive = true },
                        after: new { IsActive = false }
                    );
                }
                catch { /* Audit log failure must not block the operation */ }
            }

            return new EnableDisableDeviceResponse { Success = true, Message = "DEVICE_DISABLED" };
        }

        public async Task<DeleteDeviceResponse> DeleteDeviceAsync(long id)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null)
                throw new InvalidOperationException("DEVICE_NOT_FOUND");

            if (device.DeviceCode == "DEFAULT_DEVICE")
                throw new InvalidOperationException("CANNOT_DELETE_DEFAULT_DEVICE");

            var hasSessions = await _deviceRepo.HasSessionsAsync(id);
            if (hasSessions)
            {
                // Prevent disabling the last active device even when deactivating via delete
                var all = await _deviceRepo.ListAllAsync();
                if (all.Count(d => d.IsActive) <= 1 && device.IsActive)
                {
                    throw new InvalidOperationException("CANNOT_DISABLE_LAST_ACTIVE_DEVICE");
                }

                // Instead of physical delete, deactivate
                if (device.IsActive)
                {
                    device.IsActive = false;
                    device.UpdatedAt = DateTime.UtcNow;
                    await _deviceRepo.UpdateAsync(device);

                    try
                    {
                        await _auditLogService.RecordAsync(
                            "DISABLE_DEVICE",
                            "PosDevice",
                            device.Id.ToString(),
                            device.DeviceName,
                            before: new { IsActive = true },
                            after: new { IsActive = false }
                        );
                    }
                    catch { /* Audit log failure must not block the operation */ }
                }

                return new DeleteDeviceResponse
                {
                    Success = false,
                    Message = "DEVICE_DEACTIVATED_INSTEAD_OF_DELETED"
                };
            }

            await _deviceRepo.DeleteAsync(id);

            // Audit logging
            try
            {
                await _auditLogService.RecordAsync(
                    "DELETE_DEVICE",
                    "PosDevice",
                    device.Id.ToString(),
                    device.DeviceName,
                    before: new { device.DeviceCode, device.DeviceName, device.IsActive }
                );
            }
            catch { /* Audit log failure must not block the operation */ }

            return new DeleteDeviceResponse { Success = true, Message = "DEVICE_DELETED" };
        }
    }
}
