using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using BlogPlatform.Services;

namespace BlogPlatform.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public ErrorController(
            ILogger<ErrorController> logger,
            UserActivityLogger userActivityLogger)
        {
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            var path = exceptionHandlerPathFeature?.Path;
            var username = User?.Identity?.Name ?? "Anonymous";

            // Логируем ошибку статуса
            _logger.LogWarning("Ошибка {StatusCode} на пути: {Path}, пользователь: {Username}",
                statusCode, path, username);

            ViewData["StatusCode"] = statusCode;
            ViewData["Title"] = "Ошибка";

            switch (statusCode)
            {
                case 403:
                    ViewData["Title"] = "Доступ запрещен";
                    _userActivityLogger.LogUserAction(username, "AccessDenied", $"Path: {path}",
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                    return View("AccessDenied");
                case 404:
                    ViewData["Title"] = "Страница не найдена";
                    _logger.LogWarning("404 Not Found: {Path}", path);
                    return View("NotFound");
                case 401:
                    ViewData["Title"] = "Неавторизованный доступ";
                    return View("Unauthorized");
                default:
                    ViewData["Title"] = "Ошибка";
                    return View("Error");
            }
        }

        [Route("Error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            var path = exceptionHandlerPathFeature?.Path;
            var username = User?.Identity?.Name ?? "Anonymous";

            // Логирование ошибки
            if (exception != null)
            {
                _logger.LogError(exception, "Необработанное исключение на пути: {Path}, пользователь: {Username}",
                    path, username);

                _userActivityLogger.LogError("Unhandled exception", exception, username,
                    new { Path = path, ExceptionType = exception.GetType().Name });
            }

            ViewData["Title"] = "Что-то пошло не так";
            ViewData["ErrorMessage"] = exception?.Message;
            ViewData["Path"] = path;

            // Показываем stack trace только админам
            if (User?.IsInRole("Admin") == true)
            {
                ViewData["StackTrace"] = exception?.StackTrace;
            }

            return View("Error");
        }

        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            var username = User?.Identity?.Name ?? "Anonymous";
            var path = HttpContext.Request.Path;

            _logger.LogWarning("Access Denied для пользователя {Username} на пути: {Path}",
                username, path);

            _userActivityLogger.LogUserAction(username, "AccessDenied", $"Path: {path}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ViewData["Title"] = "Доступ запрещен";
            ViewData["StatusCode"] = 403;
            return View();
        }

        [Route("NotFound")]
        public IActionResult NotFoundPage()
        {
            var path = HttpContext.Request.Path;
            var username = User?.Identity?.Name ?? "Anonymous";

            _logger.LogWarning("404 Not Found: {Path}, пользователь: {Username}", path, username);

            ViewData["Title"] = "Страница не найдена";
            ViewData["StatusCode"] = 404;
            return View();
        }

        [Route("Unauthorized")]
        public IActionResult UnauthorizedAccess()
        {
            var path = HttpContext.Request.Path;
            var username = User?.Identity?.Name ?? "Anonymous";

            _logger.LogWarning("401 Unauthorized: {Path}, пользователь: {Username}", path, username);

            ViewData["Title"] = "Неавторизованный доступ";
            ViewData["StatusCode"] = 401;
            return View();
        }
    }
}