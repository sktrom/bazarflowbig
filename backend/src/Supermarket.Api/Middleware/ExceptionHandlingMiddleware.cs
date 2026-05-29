using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Supermarket.Api.Models;
using Supermarket.Application.Common.Interfaces;

namespace Supermarket.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private const string GenericErrorMessage = "حدث خطأ غير متوقع. يرجى المحاولة لاحقاً.";
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly IReadOnlyDictionary<string, (int StatusCode, string Message)> KnownBusinessErrors =
            new Dictionary<string, (int StatusCode, string Message)>(StringComparer.Ordinal)
            {
                ["NO_ACTIVE_SESSION"] = (StatusCodes.Status401Unauthorized, "لا توجد جلسة نشطة."),
                ["UNAUTHORIZED_SCREEN_ACCESS"] = (StatusCodes.Status403Forbidden, "لا تملك صلاحية الوصول لهذه الشاشة."),
                ["SETUP_REQUIRED"] = (StatusCodes.Status403Forbidden, "النظام يحتاج إلى الإعداد الأول."),
                ["SETUP_STATE_INVALID"] = (StatusCodes.Status409Conflict, "حالة إعداد النظام غير صالحة."),
                ["DEFAULT_DEVICE_NOT_ALLOWED"] = (StatusCodes.Status403Forbidden, "لا يمكن استخدام الجهاز الافتراضي بعد إعداد النظام."),
                ["LOGIN_FAILED"] = (StatusCodes.Status401Unauthorized, "اسم المستخدم أو كلمة المرور غير صحيحة"),
                ["LOGIN_THROTTLED"] = (StatusCodes.Status429TooManyRequests, "تم تجاوز عدد المحاولات المسموح. حاول بعد عدة دقائق."),
                ["EMPLOYEE_ALREADY_HAS_ACTIVE_SESSION"] = (StatusCodes.Status409Conflict, "هذا الحساب لديه جلسة نشطة بالفعل.")
            };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context, ISessionContextAccessor sessionContextAccessor)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, sessionContextAccessor);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, ISessionContextAccessor sessionContextAccessor)
        {
            var error = MapException(exception);
            
            // Add status to response
            error.Response.Status = error.StatusCode;

            var employeeId = sessionContextAccessor.Current?.EmployeeId.ToString() ?? "Anonymous";

            _logger.LogError(
                exception,
                "Unhandled API exception. ErrorCode: {ErrorCode}; TraceId: {TraceId}; Path: {Path}; Method: {Method}; User: {User}",
                error.Response.Error,
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method,
                employeeId);

            if (context.Response.HasStarted)
            {
                throw exception;
            }

            if (_env.IsDevelopment())
            {
                error.Response.Details = exception.ToString();
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
            
            if (exception is KeyNotFoundException)
            {
                return Create(StatusCodes.Status404NotFound, "NOT_FOUND", "العنصر المطلوب غير موجود.");
            }
            
            if (exception is ArgumentException)
            {
                return Create(StatusCodes.Status400BadRequest, "BAD_REQUEST", "البيانات المدخلة غير صحيحة.");
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
