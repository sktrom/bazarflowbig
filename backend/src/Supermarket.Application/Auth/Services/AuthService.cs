using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supermarket.Application.AuditLogs.Interfaces;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Contracts.Auth;
using Supermarket.Domain.Entities;
using Supermarket.Domain.Enums;

namespace Supermarket.Application.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IDeviceRepository _deviceRepo;
        private readonly IAuthSessionRepository _sessionRepo;
        private readonly IEmployeeScreenPermissionRepository _permRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISessionContextAccessor _contextAccessor;
        private readonly ILoginThrottleService _loginThrottleService;
        private readonly IAuditLogService _auditLogService;
        private readonly ISessionTokenGenerator _sessionTokenGenerator;
        private readonly ISetupStateRepository _setupStateRepository;
        private readonly IAuthPolicy _authPolicy;

        public AuthService(
            IEmployeeRepository employeeRepo,
            IDeviceRepository deviceRepo,
            IAuthSessionRepository sessionRepo,
            IEmployeeScreenPermissionRepository permRepo,
            IPasswordHasher passwordHasher,
            ISessionContextAccessor contextAccessor,
            ILoginThrottleService loginThrottleService,
            IAuditLogService auditLogService,
            ISessionTokenGenerator sessionTokenGenerator,
            ISetupStateRepository setupStateRepository,
            IAuthPolicy authPolicy)
        {
            _employeeRepo          = employeeRepo;
            _deviceRepo            = deviceRepo;
            _sessionRepo           = sessionRepo;
            _permRepo              = permRepo;
            _passwordHasher        = passwordHasher;
            _contextAccessor       = contextAccessor;
            _loginThrottleService  = loginThrottleService;
            _auditLogService       = auditLogService;
            _sessionTokenGenerator = sessionTokenGenerator;
            _setupStateRepository  = setupStateRepository;
            _authPolicy            = authPolicy;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var username = request.Username ?? string.Empty;
            var deviceCode = request.DeviceCode ?? string.Empty;

            var setupCompleted = await _setupStateRepository.IsSetupCompletedAsync();
            if (!setupCompleted)
                throw new InvalidOperationException("SETUP_REQUIRED");

            if (string.Equals(deviceCode.Trim(), "DEFAULT_DEVICE", StringComparison.OrdinalIgnoreCase) &&
                !_authPolicy.AllowDefaultDeviceLogin)
            {
                throw new InvalidOperationException("DEFAULT_DEVICE_NOT_ALLOWED");
            }

            if (_loginThrottleService.IsThrottled(username, deviceCode))
            {
                await RecordLoginAuditAsync("LOGIN_THROTTLED", request, "LOGIN_THROTTLED", throttled: true);
                throw new InvalidOperationException("LOGIN_THROTTLED");
            }

            try
            {
                var employee = await _employeeRepo.GetByUsernameAsync(username);
                if (employee == null)
                    throw new InvalidOperationException("EMPLOYEE_NOT_FOUND");

                if (!employee.IsActive)
                    throw new InvalidOperationException("EMPLOYEE_INACTIVE");

                var passwordResult = _passwordHasher.Verify(request.Password, employee.PasswordHash);
                if (passwordResult == PasswordVerifyResult.Invalid)
                    throw new InvalidOperationException("INVALID_CREDENTIALS");

                if (passwordResult == PasswordVerifyResult.ValidNeedsRehash)
                {
                    var updatedHash = _passwordHasher.Hash(request.Password);
                    await _employeeRepo.UpdatePasswordHashAsync(employee.Id, updatedHash, DateTime.UtcNow);
                    employee.PasswordHash = updatedHash;
                }

                var device = await _deviceRepo.GetByCodeAsync(deviceCode);
                if (device == null)
                    throw new InvalidOperationException("DEVICE_NOT_FOUND");

                if (!device.IsActive)
                    throw new InvalidOperationException("DEVICE_INACTIVE");

                device.LastLoginAt = DateTime.UtcNow;
                await _deviceRepo.UpdateAsync(device);

                var existingActive = await _sessionRepo.GetActiveByEmployeeIdAsync(employee.Id);
                if (existingActive != null)
                    throw new InvalidOperationException("EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION");

                var now = DateTime.UtcNow;
                var newSession = new CashSession
                {
                    EmployeeId = employee.Id,
                    DeviceId   = device.Id,
                    StartedAt  = now,
                    SessionToken = await GenerateUniqueSessionTokenAsync(),
                    TokenCreatedAt = now,
                    LastSeenAt = now,
                    ExpiresAt = now.AddHours(8),
                    Status     = CashSessionStatus.Active
                };

                try
                {
                    await _sessionRepo.CreateAsync(newSession);
                }
                catch
                {
                    throw new InvalidOperationException("SESSION_START_FAILED");
                }

                var allowedKeys = await _permRepo.GetAllowedScreenKeysAsync(employee.Id);

                _loginThrottleService.Reset(username, deviceCode);
                await RecordLoginAuditAsync("LOGIN_SUCCESS", request, "SUCCESS", throttled: false);

                return new LoginResponse
                {
                    EmployeeId      = employee.Id,
                    FullName        = employee.FullName,
                    SessionId       = newSession.Id,
                    SessionToken    = newSession.SessionToken ?? string.Empty,
                    DeviceCode      = device.DeviceCode,
                    AllowedScreenKeys = new List<string>(allowedKeys)
                };
            }
            catch (InvalidOperationException ex) when (IsUnifiedLoginFailure(ex.Message))
            {
                _loginThrottleService.RecordFailedAttempt(username, deviceCode);
                await RecordLoginAuditAsync("LOGIN_FAILED", request, ex.Message, throttled: false);
                throw new InvalidOperationException("LOGIN_FAILED");
            }
        }

        private static bool IsUnifiedLoginFailure(string errorCode)
        {
            return errorCode is "EMPLOYEE_NOT_FOUND"
                or "EMPLOYEE_INACTIVE"
                or "INVALID_CREDENTIALS"
                or "DEVICE_NOT_FOUND"
                or "DEVICE_INACTIVE";
        }

        private async Task RecordLoginAuditAsync(string action, LoginRequest request, string internalReason, bool throttled)
        {
            try
            {
                await _auditLogService.RecordAsync(
                    action,
                    "Auth",
                    null,
                    request.Username?.Trim(),
                    null,
                    null,
                    new
                    {
                        username = request.Username?.Trim(),
                        deviceCode = request.DeviceCode?.Trim(),
                        internalReason,
                        throttled
                    });
            }
            catch
            {
                // Audit is best-effort and must not block authentication behavior.
            }
        }

        private async Task<string> GenerateUniqueSessionTokenAsync()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var token = _sessionTokenGenerator.Generate();
                if (await _sessionRepo.GetActiveByTokenAsync(token) == null)
                    return token;
            }

            throw new InvalidOperationException("SESSION_START_FAILED");
        }

        public async Task<LogoutResponse> LogoutAsync(long sessionId)
        {
            var session = await _sessionRepo.GetActiveByIdAsync(sessionId);
            if (session == null)
                throw new InvalidOperationException("NO_ACTIVE_SESSION");

            // Stub: cart check will be wired in cart module
            // if (await _cartService.HasActiveCartAsync(session.Id))
            //     throw new InvalidOperationException("LOGOUT_BLOCKED_CART_NOT_EMPTY");

            await _sessionRepo.CloseAsync(session.Id, DateTime.UtcNow);

            return new LogoutResponse { Success = true, Message = "Logout successful" };
        }

        public async Task<CurrentEmployeeResponse> GetCurrentEmployeeAsync(long sessionId)
        {
            var session = await _sessionRepo.GetActiveByIdAsync(sessionId);
            if (session == null)
                throw new InvalidOperationException("NO_ACTIVE_SESSION");

            var employee = await _employeeRepo.GetByIdAsync(session.EmployeeId);
            var device   = await _deviceRepo.GetByIdAsync(session.DeviceId);

            return new CurrentEmployeeResponse
            {
                EmployeeId      = session.EmployeeId,
                FullName        = employee?.FullName ?? string.Empty,
                Username        = employee?.Username ?? string.Empty,
                SessionId       = session.Id,
                DeviceCode      = device?.DeviceCode ?? string.Empty,
                SessionStartedAt = session.StartedAt
            };
        }

        public async Task<EmployeePermissionsResponse> GetPermissionsAsync(long employeeId)
        {
            var allowedKeys = await _permRepo.GetAllowedScreenKeysAsync(employeeId);
            return new EmployeePermissionsResponse
            {
                EmployeeId       = employeeId,
                AllowedScreenKeys = new List<string>(allowedKeys)
            };
        }
    }
}
