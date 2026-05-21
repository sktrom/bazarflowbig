using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Application.Employees.Interfaces;
using Supermarket.Contracts.Employees;
using Supermarket.Domain.Entities;

namespace Supermarket.Application.Employees.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeManagementRepository _employeeRepo;
        private readonly IPermissionManagementRepository _permissionRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public EmployeeService(
            IEmployeeManagementRepository employeeRepo,
            IPermissionManagementRepository permissionRepo,
            IPasswordHasher passwordHasher,
            IAuditLogService auditLogService)
        {
            _employeeRepo = employeeRepo;
            _permissionRepo = permissionRepo;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        public async Task<EmployeeListResponse> GetAllAsync()
        {
            var employees = await _employeeRepo.GetAllAsync();
            return new EmployeeListResponse
            {
                Items = employees.Select(e => new EmployeeListItem
                {
                    Id = e.Id,
                    FullName = e.FullName,
                    Username = e.Username,
                    Phone = e.Phone,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };
        }

        public async Task<EmployeeDetailResponse> GetByIdAsync(long id)
        {
            var employee = await _employeeRepo.GetByIdAsync(id);
            if (employee == null) throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

            var permissions = await _permissionRepo.GetFullPermissionsAsync(id);

            return MapToDetailResponse(employee, permissions);
        }

        public async Task<EmployeeDetailResponse> CreateAsync(CreateEmployeeRequest request)
        {
            var existing = await _employeeRepo.GetByUsernameAsync(request.Username);
            if (existing != null) throw new InvalidOperationException("USERNAME_ALREADY_EXISTS");

            var employee = new Employee
            {
                FullName = request.FullName,
                Username = request.Username,
                Phone = request.Phone,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _employeeRepo.CreateAsync(employee);

            if (request.Permissions != null && request.Permissions.Any())
            {
                var permissionList = new List<(int ScreenId, bool CanAccess)>();
                foreach (var p in request.Permissions)
                {
                    var screenId = await _permissionRepo.GetScreenIdByKeyAsync(p.ScreenKey);
                    if (screenId.HasValue)
                        permissionList.Add((screenId.Value, p.CanAccess));
                    else
                        throw new InvalidOperationException("INVALID_SCREEN_KEY");
                }
                await _permissionRepo.ReplacePermissionsAsync(employee.Id, permissionList);
            }

            var fullPermissions = await _permissionRepo.GetFullPermissionsAsync(employee.Id);
            await RecordEmployeeCreateAsync(employee, fullPermissions);
            return MapToDetailResponse(employee, fullPermissions);
        }

        public async Task<EmployeeDetailResponse> UpdateAsync(long id, UpdateEmployeeRequest request)
        {
            var employee = await _employeeRepo.GetByIdAsync(id);
            if (employee == null) throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

            var beforeEmployee = new
            {
                employee.FullName,
                employee.Phone,
                employee.IsActive
            };
            var permissionsBefore = request.Permissions == null
                ? Array.Empty<string>()
                : OrderedEnabledScreens(await _permissionRepo.GetFullPermissionsAsync(id));
            var permissionList = new List<(int ScreenId, bool CanAccess)>();

            if (request.Permissions != null)
            {
                foreach (var p in request.Permissions)
                {
                    var screenId = await _permissionRepo.GetScreenIdByKeyAsync(p.ScreenKey);
                    if (screenId.HasValue)
                        permissionList.Add((screenId.Value, p.CanAccess));
                    else
                        throw new InvalidOperationException("INVALID_SCREEN_KEY");
                }
            }

            employee.FullName = request.FullName;
            employee.Phone = request.Phone;
            employee.IsActive = request.IsActive;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepo.UpdateAsync(employee);

            if (request.Permissions != null)
            {
                await _permissionRepo.ReplacePermissionsAsync(employee.Id, permissionList);

                var permissionsAfter = OrderedEnabledScreens(request.Permissions);
                if (!permissionsBefore.SequenceEqual(permissionsAfter, StringComparer.Ordinal))
                {
                    await RecordPermissionsUpdateAsync(employee, permissionsBefore, permissionsAfter);
                }
            }

            await RecordEmployeeUpdateAsync(employee, beforeEmployee);

            var fullPermissions = await _permissionRepo.GetFullPermissionsAsync(employee.Id);
            return MapToDetailResponse(employee, fullPermissions);
        }

        public async Task<DeleteEmployeeResponse> DeleteAsync(long id, long currentEmployeeId)
        {
            if (id == currentEmployeeId)
                throw new InvalidOperationException("CANNOT_DELETE_SELF");

            var employee = await _employeeRepo.GetByIdAsync(id);
            if (employee == null) throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

            bool hasRefs = await _employeeRepo.HasRelatedRecordsAsync(id);

            if (hasRefs)
            {
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;
                await _employeeRepo.UpdateAsync(employee);
                await RecordEmployeeDeactivateAsync(employee);
                return new DeleteEmployeeResponse { Success = true, Action = "DEACTIVATED", Message = "Employee has related records and was deactivated instead of deleted." };
            }
            else
            {
                await _permissionRepo.DeleteAllForEmployeeAsync(id);
                await _employeeRepo.DeleteAsync(id);
                await RecordEmployeeDeleteAsync(employee);
                return new DeleteEmployeeResponse { Success = true, Action = "DELETED", Message = "Employee deleted successfully." };
            }
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(long id, ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                throw new InvalidOperationException("INVALID_PASSWORD");

            var employee = await _employeeRepo.GetByIdAsync(id);
            if (employee == null) throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

            employee.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepo.UpdateAsync(employee);

            await _auditLogService.RecordAsync(
                "RESET_PASSWORD",
                "Employee",
                employee.Id.ToString(),
                string.IsNullOrWhiteSpace(employee.Username) ? employee.FullName : employee.Username,
                metadata: new { targetEmployeeId = employee.Id });

            return new ResetPasswordResponse { Success = true, Message = "Password reset successfully." };
        }

        private async Task RecordEmployeeCreateAsync(Employee employee, IReadOnlyList<EmployeePermissionView> permissions)
        {
            await RecordAuditAsync(() => _auditLogService.RecordAsync(
                "EMPLOYEE_CREATE",
                "Employee",
                employee.Id.ToString(),
                EmployeeDisplayName(employee),
                after: new
                {
                    employeeId = employee.Id,
                    employee.Username,
                    employee.FullName,
                    employee.Phone,
                    employee.IsActive
                },
                metadata: new
                {
                    permissionCount = permissions.Count,
                    enabledScreens = OrderedEnabledScreens(permissions)
                }));
        }

        private async Task RecordEmployeeUpdateAsync(Employee employee, object before)
        {
            await RecordAuditAsync(() => _auditLogService.RecordAsync(
                "EMPLOYEE_UPDATE",
                "Employee",
                employee.Id.ToString(),
                EmployeeDisplayName(employee),
                before: before,
                after: new
                {
                    employee.FullName,
                    employee.Phone,
                    employee.IsActive
                },
                metadata: new
                {
                    employeeId = employee.Id,
                    employee.Username
                }));
        }

        private async Task RecordPermissionsUpdateAsync(Employee employee, IReadOnlyList<string> before, IReadOnlyList<string> after)
        {
            await RecordAuditAsync(() => _auditLogService.RecordAsync(
                "UPDATE_PERMISSIONS",
                "Employee",
                employee.Id.ToString(),
                EmployeeDisplayName(employee),
                before: new { enabledScreens = before },
                after: new { enabledScreens = after },
                metadata: new
                {
                    employeeId = employee.Id,
                    employee.Username,
                    enabledCountBefore = before.Count,
                    enabledCountAfter = after.Count
                }));
        }

        private async Task RecordEmployeeDeactivateAsync(Employee employee)
        {
            await RecordAuditAsync(() => _auditLogService.RecordAsync(
                "EMPLOYEE_DEACTIVATE",
                "Employee",
                employee.Id.ToString(),
                EmployeeDisplayName(employee),
                before: new { isActive = true },
                after: new { isActive = false },
                metadata: new
                {
                    employeeId = employee.Id,
                    employee.Username,
                    employee.FullName
                }));
        }

        private async Task RecordEmployeeDeleteAsync(Employee employee)
        {
            await RecordAuditAsync(() => _auditLogService.RecordAsync(
                "EMPLOYEE_DELETE",
                "Employee",
                employee.Id.ToString(),
                EmployeeDisplayName(employee),
                before: new
                {
                    employeeId = employee.Id,
                    employee.Username,
                    employee.FullName,
                    employee.Phone,
                    employee.IsActive
                }));
        }

        private static async Task RecordAuditAsync(Func<Task> record)
        {
            try
            {
                await record();
            }
            catch
            {
                // Audit logging is best-effort and must not break employee operations.
            }
        }

        private static string EmployeeDisplayName(Employee employee)
            => string.IsNullOrWhiteSpace(employee.Username) ? employee.FullName : employee.Username;

        private static string[] OrderedEnabledScreens(IEnumerable<EmployeePermissionView> permissions)
            => permissions
                .Where(permission => permission.CanAccess)
                .Select(permission => permission.ScreenKey)
                .OrderBy(screenKey => screenKey, StringComparer.Ordinal)
                .ToArray();

        private static string[] OrderedEnabledScreens(IEnumerable<PermissionEntry> permissions)
            => permissions
                .Where(permission => permission.CanAccess)
                .Select(permission => permission.ScreenKey)
                .OrderBy(screenKey => screenKey, StringComparer.Ordinal)
                .ToArray();

        private static EmployeeDetailResponse MapToDetailResponse(Employee e, IReadOnlyList<EmployeePermissionView> perms)
        {
            return new EmployeeDetailResponse
            {
                Id = e.Id,
                FullName = e.FullName,
                Username = e.Username,
                Phone = e.Phone,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Permissions = perms.Select(p => new PermissionItem
                {
                    ScreenId = p.ScreenId,
                    ScreenKey = p.ScreenKey,
                    ScreenName = p.ScreenName,
                    CanAccess = p.CanAccess
                }).ToList()
            };
        }
    }
}
