using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Supermarket.Api.Services;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Middleware
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IAuthSessionRepository sessionRepo,
            ISessionContextAccessor contextAccessor)
        {
            // Passive behavior: Start with a fresh unauthenticated context
            var context = new SessionContext { IsAuthenticated = false };

            if (httpContext.Request.Headers.TryGetValue("X-Session-Id", out var values))
            {
                if (long.TryParse(values.FirstOrDefault(), out var sessionId))
                {
                    var session = await sessionRepo.GetActiveByIdAsync(sessionId);
                    if (session != null)
                    {
                        context.SessionId = session.Id;
                        context.EmployeeId = session.EmployeeId;
                        context.DeviceCode = session.Device?.DeviceCode ?? string.Empty;
                        context.IsAuthenticated = true;
                    }
                }
            }

            // Populate the accessor with the result (authenticated or not)
            contextAccessor.SetContext(context);

            // ALWAYS proceed in the pipeline (passive)
            await _next(httpContext);
        }
    }
}
