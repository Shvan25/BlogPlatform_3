using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BlogPlatform.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
            {
                // Для AJAX запросов возвращаем JSON
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.Request.ContentType?.Contains("application/json") == true)
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($$"""
                        {
                            "error": "Unauthorized",
                            "message": "Please login to access this resource",
                            "redirect": "/Auth/Login"
                        }
                        """);
                }
                // Для обычных запросов редиректим на страницу входа
                else
                {
                    context.Response.Redirect("/Auth/Login?returnUrl=" +
                        Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
                }
            }
        }
    }
}