using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BlogPlatform.Middleware
{
    public class RequireAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _protectedPaths = new() { "/tags/create", "/role", "/users" };

        public RequireAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            if (_protectedPaths.Any(p => path?.Contains(p) == true))
            {
                if (!context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.Redirect("/Auth/Login");
                    return;
                }
            }

            await _next(context);
        }
    }
}