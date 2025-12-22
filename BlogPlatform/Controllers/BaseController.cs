using BlogPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlogPlatform.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ILogger<BaseController> _logger;
        protected readonly UserActivityLogger _userActivityLogger;

        public BaseController(
            ILogger<BaseController> logger,
            UserActivityLogger userActivityLogger)
        {
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        protected string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? "Anonymous";
        }

        protected int? GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        protected void LogInformation(string message)
        {
            _logger.LogInformation("{Username}: {Message}", GetCurrentUsername(), message);
        }

        protected void LogWarning(string message)
        {
            _logger.LogWarning("{Username}: {Message}", GetCurrentUsername(), message);
        }

        protected void LogError(string message, Exception ex = null)
        {
            _logger.LogError(ex, "{Username}: {Message}", GetCurrentUsername(), message);
        }

        protected void LogUserActivity(string action, string details = null)
        {
            _userActivityLogger.LogUserAction(
                GetCurrentUsername(),
                action,
                details,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }
    }
}