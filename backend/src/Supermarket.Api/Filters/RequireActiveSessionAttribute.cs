using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireActiveSessionAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var accessor = context.HttpContext.RequestServices.GetService<ISessionContextAccessor>();
            
            // Critical check: Decide based only on IsAuthenticated property
            if (accessor == null || !accessor.Current.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "NO_ACTIVE_SESSION" });
            }
        }
    }
}
