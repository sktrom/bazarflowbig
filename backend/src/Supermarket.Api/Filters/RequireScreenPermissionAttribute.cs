using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireScreenPermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _screenKey;

        public RequireScreenPermissionAttribute(string screenKey)
        {
            _screenKey = screenKey;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var accessor = context.HttpContext.RequestServices.GetService<ISessionContextAccessor>();
            if (accessor == null || !accessor.Current.IsAuthenticated)
            {
                // Should have been handled by RequireActiveSession, but safety first
                context.Result = new UnauthorizedObjectResult(new { error = "NO_ACTIVE_SESSION" });
                return;
            }

            var employeeId = accessor.Current.EmployeeId;
            var permissionRepo = context.HttpContext.RequestServices.GetService<IEmployeeScreenPermissionRepository>();

            if (permissionRepo == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Sync version of Task to keep filter simple, or use IAsyncAuthorizationFilter
            // Since we are aiming for simplicity in V1:
            var allowedScreens = permissionRepo.GetAllowedScreenKeysAsync(employeeId).GetAwaiter().GetResult();

            if (!allowedScreens.Contains(_screenKey))
            {
                context.Result = new ObjectResult(new { error = "UNAUTHORIZED_SCREEN_ACCESS" })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
