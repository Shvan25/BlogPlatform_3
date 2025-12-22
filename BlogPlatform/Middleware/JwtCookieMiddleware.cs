using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BlogPlatform.Middleware
{
    public class JwtCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Проверяем куки
            if (context.Request.Cookies.TryGetValue("auth_token", out var tokenFromCookie))
            {
                context.Request.Headers["Authorization"] = $"Bearer {tokenFromCookie}";
            }
            // 2. Проверяем заголовок Authorization (если уже есть, не перезаписываем)
            else if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                // 3. Пытаемся получить токен из строки запроса (для отладки)
                if (context.Request.Query.TryGetValue("token", out var queryToken))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {queryToken}";
                }
            }

            await _next(context);
        }
    }
}