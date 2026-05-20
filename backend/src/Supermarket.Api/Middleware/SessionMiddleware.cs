using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Supermarket.Api.Services;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Middleware
{
    public class SessionMiddleware
    {
        private static readonly TimeSpan RollingTimeout = TimeSpan.FromHours(8);
        private static readonly TimeSpan TouchInterval = TimeSpan.FromSeconds(60);
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

            if (httpContext.Request.Headers.TryGetValue("X-Session-Token", out var values))
            {
                var token = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token) && token.Length >= 32)
                {
                    var now = DateTime.UtcNow;
                    var session = await sessionRepo.GetActiveByTokenAsync(token);
                    if (session != null)
                    {
                        if (session.ExpiresAt.HasValue && session.ExpiresAt.Value <= now)
                        {
                            await sessionRepo.ExpireAsync(session.Id, now);
                        }
                        else
                        {
                            context.SessionId = session.Id;
                            context.EmployeeId = session.EmployeeId;
                            context.DeviceCode = session.Device?.DeviceCode ?? string.Empty;
                            context.IsAuthenticated = true;

                            if (!session.LastSeenAt.HasValue || now - session.LastSeenAt.Value >= TouchInterval)
                            {
                                await sessionRepo.TouchAsync(session.Id, now, now.Add(RollingTimeout));
                            }
                        }
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
