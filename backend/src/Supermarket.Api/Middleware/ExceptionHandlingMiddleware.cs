using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Supermarket.Api.Models;

namespace Supermarket.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private const string GenericErrorMessage = "حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.";
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        private static readonly IReadOnlyDictionary<string, (int StatusCode, string Message)> KnownBusinessErrors =
            new Dictionary<string, (int StatusCode, string Message)>(StringComparer.Ordinal)
            {
                ["NO_ACTIVE_SESSION"] = (StatusCodes.Status401Unauthorized, "لا توجد جلسة نشطة."),
                ["UNAUTHORIZED_SCREEN_ACCESS"] = (StatusCodes.Status403Forbidden, "لا تملك صلاحية الوصول لهذه الشاشة."),
                ["SETUP_REQUIRED"] = (StatusCodes.Status403Forbidden, "النظام يحتاج إلى الإعداد الأول."),
                ["SETUP_STATE_INVALID"] = (StatusCodes.Status409Conflict, "حالة إعداد النظام غير صالحة."),
                ["DEFAULT_DEVICE_NOT_ALLOWED"] = (StatusCodes.Status403Forbidden, "لا يمكن استخدام الجهاز الافتراضي بعد إعداد النظام."),
                ["LOGIN_FAILED"] = (StatusCodes.Status401Unauthorized, "تعذر تسجيل الدخول."),
                ["LOGIN_THROTTLED"] = (StatusCodes.Status429TooManyRequests, "تم تجاوز عدد المحاولات. حاول بعد قليل."),
                ["EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION"] = (StatusCodes.Status409Conflict, "هذا الحساب لديه جلسة نشطة بالفعل.")
            };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var error = MapException(exception);

            _logger.LogError(
                exception,
                "Unhandled API exception. ErrorCode: {ErrorCode}; TraceId: {TraceId}",
                error.Response.Error,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw exception;
            }

            context.Response.Clear();
            context.Response.StatusCode = error.StatusCode;
            context.Response.ContentType = "application/json";

            error.Response.TraceId = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(error.Response);
        }

        private static (int StatusCode, ApiErrorResponse Response) MapException(Exception exception)
        {
            if (exception is UnauthorizedAccessException)
            {
                return Create(StatusCodes.Status403Forbidden, "FORBIDDEN", "لا تملك صلاحية تنفيذ هذه العملية.");
            }

            if (exception is DbUpdateException)
            {
                return Create(StatusCodes.Status500InternalServerError, "DATABASE_ERROR", GenericErrorMessage);
            }

            if (exception is InvalidOperationException &&
                KnownBusinessErrors.TryGetValue(exception.Message, out var mapped))
            {
                return Create(mapped.StatusCode, exception.Message, mapped.Message);
            }

            return Create(StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", GenericErrorMessage);
        }

        private static (int StatusCode, ApiErrorResponse Response) Create(int statusCode, string error, string message)
        {
            return (statusCode, new ApiErrorResponse
            {
                Error = error,
                Message = message
            });
        }
    }
}
