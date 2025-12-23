using BlogPlatform.API.Filters;
using BlogPlatform.Controllers;
using BlogPlatform.Data.Data;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Reflection;
using System.Text;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("=== Запуск BlogPlatform API ===");

    var builder = WebApplication.CreateBuilder(args);

    // Настройка NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Добавляем сервисы в контейнер
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });

    // ========== НАСТРОЙКА SWAGGER/OPENAPI ==========
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "BlogPlatform REST API",
            Description = "RESTful API для управления блог-платформой",
            Contact = new OpenApiContact
            {
                Name = "BlogPlatform Team",
                Email = "support@blogplatform.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT License"
            }
        });

        // ========== РЕШЕНИЕ КОНФЛИКТОВ МАРШРУТОВ ==========
        // Метод 1: Фильтруем только контроллеры из API проекта
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            if (apiDesc.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                var controllerAssembly = controllerActionDescriptor.ControllerTypeInfo.Assembly;
                var apiAssembly = typeof(Program).Assembly;
                var isApiController = controllerAssembly == apiAssembly;

                logger.Debug($"Controller: {controllerActionDescriptor.ControllerName}, " +
                           $"Assembly: {controllerAssembly.GetName().Name}, " +
                           $"IsApiController: {isApiController}");

                return isApiController;
            }
            return false;
        });

        // Метод 2: Резолвер конфликтов (дополнительная защита)
        options.ResolveConflictingActions(apiDescriptions =>
        {
            var apiAssembly = typeof(Program).Assembly;

            // Логируем все найденные маршруты
            foreach (var desc in apiDescriptions)
            {
                if (desc.ActionDescriptor is ControllerActionDescriptor cad)
                {
                    logger.Debug($"Found route: {desc.RelativePath} " +
                               $"from {cad.ControllerTypeInfo.Assembly.GetName().Name}");
                }
            }

            // Приоритет: контроллеры из API проекта
            var apiController = apiDescriptions.FirstOrDefault(desc =>
            {
                if (desc.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                {
                    return controllerActionDescriptor.ControllerTypeInfo.Assembly == apiAssembly;
                }
                return false;
            });

            if (apiController != null)
            {
                logger.Debug($"Selected API controller for route: {apiController.RelativePath}");
                return apiController;
            }

            // Если не нашли API контроллер, берем первый
            var first = apiDescriptions.First();
            return first;
        });

        // Метод 3: Группировка по тегам (опционально)
        options.TagActionsBy(api =>
        {
            if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
            {
                return new[] { controllerActionDescriptor.ControllerName };
            }
            return new[] { api.GroupName ?? "Default" };
        });

        // ========== XML КОММЕНТАРИИ ==========
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
            logger.Info($"XML documentation loaded from: {xmlPath}");
        }

        // ========== НАСТРОЙКА JWT АВТОРИЗАЦИИ ==========
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n" +
                        "Enter 'Bearer' [space] and then your token in the text input below.\r\n" +
                        "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // ========== ДОПОЛНИТЕЛЬНЫЕ НАСТРОЙКИ ==========
        options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

        // Игнорируем [Obsolete] методы
        options.IgnoreObsoleteActions();
        options.IgnoreObsoleteProperties();
    });

    // ========== НАСТРОЙКА БАЗЫ ДАННЫХ ==========
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=blog.db"));

    // ========== РЕГИСТРАЦИЯ СЕРВИСОВ ==========
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IArticleService, ArticleService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<UserActivityLogger>();

    // ========== НАСТРОЙКА JWT АВТОРИЗАЦИИ ==========
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_1234567890!@#$%^&*()";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };

        // Отладочное логирование
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                logger.Debug($"JWT Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                logger.Debug($"JWT Token validated for user: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

    // ========== НАСТРОЙКА CORS ==========
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        options.AddPolicy("AllowLocalhost", policy =>
        {
            policy.WithOrigins("https://localhost:7001", "https://localhost:7002")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ========== СОЗДАНИЕ ПАПКИ ЛОГОВ ==========
    var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    if (!Directory.Exists(logDirectory))
    {
        Directory.CreateDirectory(logDirectory);
        logger.Info($"Создана папка для логов: {logDirectory}");
    }

    // ========== КОНФИГУРАЦИЯ HTTP PIPELINE ==========
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();

        // ВКЛЮЧАЕМ SWAGGER
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BlogPlatform API v1");
            options.RoutePrefix = "swagger"; // или "api-docs" если предпочитаете

            // Дополнительные настройки Swagger UI
            options.DocumentTitle = "BlogPlatform API Documentation";
            options.DefaultModelsExpandDepth(-1); // Скрыть Models секцию
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();

            // Inject CSS для кастомизации
            options.InjectStylesheet("/swagger-ui/custom.css");
        });

        logger.Info("Swagger UI включен для Development среды");
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Статические файлы для Swagger CSS
    app.UseStaticFiles();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // ========== TELEMETRY И ПРОВЕРОЧНЫЕ ENDPOINTS ==========
    app.MapGet("/", () => new
    {
        Message = "BlogPlatform API запущен и работает!",
        Documentation = "/swagger",
        HealthCheck = "/health",
        Time = DateTime.UtcNow,
        Version = "1.0.0",
        Environment = app.Environment.EnvironmentName
    }).ExcludeFromDescription(); // Исключаем из Swagger

    app.MapGet("/health", () => new
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Service = "BlogPlatform API",
        Version = "1.0.0",
        Uptime = Environment.TickCount64 / 1000
    }).WithTags("Health").WithName("HealthCheck");

    app.MapGet("/api/status", () => "API endpoints are operational")
       .WithTags("Diagnostics");

    app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
    {
        var endpoints = endpointSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>();

        return new
        {
            TotalEndpoints = endpoints.Count(),
            Routes = endpoints.Select(e => new
            {
                Pattern = e.RoutePattern.RawText,
                Method = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault(),
                Controller = e.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName
            }).Where(r => r.Controller != null)
        };
    }).ExcludeFromDescription();

    // ========== ЗАПУСК ПРИЛОЖЕНИЯ ==========
    logger.Info("=== BlogPlatform API готов к работе ===");
    logger.Info($"Environment: {app.Environment.EnvironmentName}");
    logger.Info($"Content Root: {app.Environment.ContentRootPath}");

    var urls = app.Urls;
    foreach (var url in urls)
    {
        logger.Info($"Listening on: {url}");
    }

    logger.Info($"Swagger UI: {string.Join(", ", urls.Select(u => $"{u}/swagger"))}");
    logger.Info($"Health check: {string.Join(", ", urls.Select(u => $"{u}/health"))}");

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "Критическая ошибка при запуске BlogPlatform API");
    throw;
}
finally
{
    logger.Info("=== BlogPlatform API завершает работу ===");
    NLog.LogManager.Shutdown();
}