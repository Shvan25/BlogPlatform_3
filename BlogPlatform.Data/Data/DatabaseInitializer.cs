using BlogPlatform.Data.Data;
using BlogPlatform.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BlogPlatform.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            // Создание ролей если их нет
            if (!await context.Roles.AnyAsync())
            {
                var roles = new[]
                {
                    new Role { Name = "Admin", Description = "Администратор системы", CreatedAt = DateTime.UtcNow },
                    new Role { Name = "Moderator", Description = "Модератор контента", CreatedAt = DateTime.UtcNow },
                    new Role { Name = "User", Description = "Обычный пользователь", CreatedAt = DateTime.UtcNow }
                };

                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
                Console.WriteLine("Roles created");
            }

            // Создание пользователей если их нет
            if (!await context.Users.AnyAsync())
            {
                Console.WriteLine("Creating users...");

                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@blog.com",
                    PasswordHash = "admin123", // Пароль без хэширования
                    FullName = "Администратор",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var moderator = new User
                {
                    Username = "moderator",
                    Email = "moderator@blog.com",
                    PasswordHash = "moderator123", // Пароль без хэширования
                    FullName = "Модератор",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var user = new User
                {
                    Username = "user",
                    Email = "user@blog.com",
                    PasswordHash = "user123", // Пароль без хэширования
                    FullName = "Пользователь",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(admin, moderator, user);
                await context.SaveChangesAsync();
                Console.WriteLine("Users created");

                // Получаем роли
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var moderatorRole = await context.Roles.FirstAsync(r => r.Name == "Moderator");
                var userRole = await context.Roles.FirstAsync(r => r.Name == "User");

                // Назначаем роли
                context.UserRoles.AddRange(
                    new UserRole { UserId = admin.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow },
                    new UserRole { UserId = moderator.Id, RoleId = moderatorRole.Id, AssignedAt = DateTime.UtcNow },
                    new UserRole { UserId = user.Id, RoleId = userRole.Id, AssignedAt = DateTime.UtcNow }
                );

                await context.SaveChangesAsync();
                Console.WriteLine("Roles assigned to users");

                // Создаем несколько тестовых тегов
                var tags = new[]
                {
                    new Tag { Name = "Программирование", Slug = "programming", Description = "Статьи о программировании", CreatedAt = DateTime.UtcNow },
                    new Tag { Name = "C#", Slug = "csharp", Description = "Статьи о C#", CreatedAt = DateTime.UtcNow },
                    new Tag { Name = "ASP.NET", Slug = "aspnet", Description = "Статьи о ASP.NET", CreatedAt = DateTime.UtcNow },
                    new Tag { Name = "Базы данных", Slug = "databases", Description = "Статьи о базах данных", CreatedAt = DateTime.UtcNow }
                };

                context.Tags.AddRange(tags);
                await context.SaveChangesAsync();
                Console.WriteLine("Tags created");
            }

            Console.WriteLine("Database initialization completed");
        }
    }
}