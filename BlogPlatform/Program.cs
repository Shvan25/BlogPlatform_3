using BlogPlatform.Data.Data;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.Services;
using BlogPlatform.Middleware;
using BlogPlatform.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using System.Security.Claims;
using System.Text;

// Создать логгер
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Запуск приложения BlogPlatform");

    var builder = WebApplication.CreateBuilder(args);

    // Добавить NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Добавить сервис логирования действий пользователя
    builder.Services.AddScoped<UserActivityLogger>();

    // 1. Добавляем оба типа контроллеров
    builder.Services.AddControllersWithViews(); // Для MVC контроллеров

    // 2. База данных
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=blog.db"));

    // 3. Регистрация сервисов
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<IArticleService, ArticleService>();
    builder.Services.AddScoped<ICommentService, CommentService>();

    // И JWT аутентификация:
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_1234567890!@#$%^&*()";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "BlogPlatform",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BlogPlatformUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        // Для отладки
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claims = context.Principal?.Claims;
                Console.WriteLine($"Токен валидирован. Claims:");
                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        Console.WriteLine($"  {claim.Type}: {claim.Value}");
                    }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Ошибка аутентификации: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

    // Разрешаем доступ к страницам ошибок без авторизации
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ErrorPages", policy =>
            policy.RequireAssertion(context => true)); // Всегда разрешено
    });

    var app = builder.Build();

    // 4. Конфигурация pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");

    // ВАЖНО: Authentication должен быть ДО Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Middleware для работы с JWT в куках
    app.UseMiddleware<JwtCookieMiddleware>();

    // API маршруты
    app.MapControllers(); // API контроллеры

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithGroupName("MVC");

    // 6. Инициализация базы данных
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Убедимся, что база создана и применим миграции
        await context.Database.EnsureCreatedAsync();

        // Проверим существующие маршруты в БД
        Console.WriteLine($"Database created. Users: {await context.Users.CountAsync()}");

        // Создаем начальные данные, если база пустая
        if (!await context.Users.AnyAsync())
        {
            // Создаем тестового пользователя
            var user = new BlogPlatform.Data.Models.User
            {
                Username = "admin",
                Email = "admin@test.com",
                PasswordHash = "admin123",
                FullName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Добавляем роли
            if (!await context.Roles.AnyAsync())
            {
                var roles = new[]
                {
                new BlogPlatform.Data.Models.Role { Name = "Admin" },
                new BlogPlatform.Data.Models.Role { Name = "Moderator" },
                new BlogPlatform.Data.Models.Role { Name = "User" }
            };
                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
            }

            // Назначаем роль админа
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");

            if (adminUser != null && adminRole != null)
            {
                context.UserRoles.Add(new BlogPlatform.Data.Models.UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            Console.WriteLine("Test user created: admin / admin123 (with Admin role)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }

    // Тестовые маршруты для проверки
    app.MapGet("/api/test", () => "API test endpoint works!");
    app.MapGet("/api/articles/test", () => "Articles API endpoint works!");

    app.MapGet("/health", () => new
    {
        Status = "OK",
        Time = DateTime.UtcNow,
        Message = "BlogPlatform API is running"
    });

    builder.Services.AddEndpointsApiExplorer();

    Console.WriteLine("Application starting...");
    Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
    Console.WriteLine($"Content Root: {app.Environment.ContentRootPath}");
    Console.WriteLine($"Web Root: {app.Environment.WebRootPath}");

    app.Run();
}

catch (Exception ex)
{
    logger.Error(ex, "Ошибка при запуске приложения");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}