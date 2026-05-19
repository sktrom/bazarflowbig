using System;

namespace Supermarket.Contracts.Devices
{
    public class DeviceListItem
    {
        public long Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeviceDetailsResponse
    {
        public long Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateDeviceRequest
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class UpdateDeviceRequest
    {
        public string DeviceName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class DeleteDeviceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EnableDisableDeviceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
