using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using BlogPlatform.Controllers;

namespace BlogPlatform.API.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public GlobalExceptionFilter(
            ILogger<GlobalExceptionFilter> logger,
            UserActivityLogger userActivityLogger)
        {
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        public void OnException(ExceptionContext context)
        {
            var username = context.HttpContext.User?.Identity?.Name ?? "Anonymous";
            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;

            _logger.LogError(context.Exception,
                "API Exception: {Method} {Path}, User: {Username}",
                method, path, username);

            _userActivityLogger.LogError("API exception", context.Exception, username,
                new
                {
                    Method = method,
                    Path = path,
                    Query = context.HttpContext.Request.QueryString
                });

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = context.HttpContext.Request.Path,
                Detail = context.Exception.Message
            };

            // В разработке добавляем больше деталей
            if (context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                problemDetails.Extensions["stackTrace"] = context.Exception.StackTrace;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}