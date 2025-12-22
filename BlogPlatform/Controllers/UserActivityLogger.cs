using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BlogPlatform.Services
{
    public class UserActivityLogger
    {
        private readonly ILogger<UserActivityLogger> _logger;

        public UserActivityLogger(ILogger<UserActivityLogger> logger)
        {
            _logger = logger;
        }

        public void LogUserAction(string username, string action, string details, string ipAddress)
        {
            _logger.LogInformation("UserAction | User: {Username} | Action: {Action} | Details: {Details} | IP: {IP}",
                username, action, details, ipAddress);
        }

        public void LogLogin(string username, bool success, string ipAddress, string reason = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var logMessage = $"Authentication | User: {username} | Status: {status} | IP: {ipAddress}";

            if (!string.IsNullOrEmpty(reason))
                logMessage += $" | Reason: {reason}";

            if (success)
                _logger.LogInformation(logMessage);
            else
                _logger.LogWarning(logMessage);
        }

        public void LogArticleAction(string action, int? articleId, string username, string articleTitle, object additionalData = null)
        {
            var data = additionalData != null ? JsonSerializer.Serialize(additionalData) : "{}";
            _logger.LogInformation("ArticleAction | Action: {Action} | ArticleID: {ArticleId} | Title: {Title} | User: {Username} | Data: {Data}",
                action, articleId, articleTitle, username, data);
        }

        public void LogCommentAction(string action, int? commentId, string username, int? articleId, object additionalData = null)
        {
            var data = additionalData != null ? JsonSerializer.Serialize(additionalData) : "{}";
            _logger.LogInformation("CommentAction | Action: {Action} | CommentID: {CommentId} | ArticleID: {ArticleId} | User: {Username} | Data: {Data}",
                action, commentId, articleId, username, data);
        }

        public void LogRoleAction(string action, string roleName, string username, int? userId = null)
        {
            _logger.LogInformation("RoleAction | Action: {Action} | Role: {RoleName} | TargetUserID: {UserId} | ByUser: {Username}",
                action, roleName, userId, username);
        }

        public void LogTagAction(string action, int? tagId, string tagName, string username)
        {
            _logger.LogInformation("TagAction | Action: {Action} | TagID: {TagId} | Name: {TagName} | User: {Username}",
                action, tagId, tagName, username);
        }

        public void LogError(string context, Exception exception, string username = null, object additionalData = null)
        {
            var data = additionalData != null ? JsonSerializer.Serialize(additionalData) : "{}";
            _logger.LogError(exception, "Error | Context: {Context} | User: {Username} | Data: {Data}",
                context, username ?? "Anonymous", data);
        }
    }
}