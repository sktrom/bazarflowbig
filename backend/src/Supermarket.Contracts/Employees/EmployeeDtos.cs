using System;
using System.Collections.Generic;

namespace Supermarket.Contracts.Employees
{
    public class EmployeeListItem
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmployeeListResponse
    {
        public List<EmployeeListItem> Items { get; set; } = new();
    }

    public class PermissionItem
    {
        public int ScreenId { get; set; }
        public string ScreenKey { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
    }

    public class EmployeeDetailResponse
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PermissionItem> Permissions { get; set; } = new();
    }

    public class PermissionEntry
    {
        public string ScreenKey { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
    }

    public class CreateEmployeeRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public List<PermissionEntry>? Permissions { get; set; }
    }

    public class UpdateEmployeeRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public List<PermissionEntry>? Permissions { get; set; }
    }

    public class DeleteEmployeeResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; } = string.Empty; // "DELETED" | "DEACTIVATED"
        public string Message { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
