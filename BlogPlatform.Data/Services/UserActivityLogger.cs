using Microsoft.Extensions.Logging;

namespace BlogPlatform.Services
{
    public class UserActivityLogger
    {
        private readonly ILogger<UserActivityLogger> _logger;

        public UserActivityLogger(ILogger<UserActivityLogger> logger)
        {
            _logger = logger;
        }

        public void LogLogin(string username, bool success, string ipAddress)
        {
            var status = success ? "успешно" : "неудачно";
            _logger.LogInformation("Вход пользователя: {Username}, Статус: {Status}, IP: {IP}",
                username, status, ipAddress);
        }

        public void LogArticleAction(string action, int articleId, string username, string articleTitle)
        {
            _logger.LogInformation("Действие со статьей: {Action}, ID: {ArticleId}, Заголовок: {Title}, Пользователь: {Username}",
                action, articleId, articleTitle, username);
        }

        public void LogCommentAction(string action, int commentId, string username, string articleTitle)
        {
            _logger.LogInformation("Действие с комментарием: {Action}, ID: {CommentId}, Статья: {Article}, Пользователь: {Username}",
                action, commentId, articleTitle, username);
        }

        public void LogError(string message, Exception exception, string username = null)
        {
            _logger.LogError(exception, "Ошибка: {Message}, Пользователь: {Username}",
                message, username ?? "Неизвестно");
        }
    }
}