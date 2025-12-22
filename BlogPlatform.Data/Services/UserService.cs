using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Models;
using BlogPlatform.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Services
{
    public class UserService(AppDbContext context) : IUserService
    {
        public async Task<UserDTO?> GetUserByIdAsync(int id)
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user != null ? MapToDTO(user) : null;
        }

        public async Task<UserDTO?> GetUserByUsernameAsync(string username)
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            return user != null ? MapToDTO(user) : null;
        }

        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            return users.Select(MapToDTO).ToList();
        }

        public async Task<UserDTO> CreateUserAsync(CreateUserDTO userDto)
        {
            if (await context.Users.AnyAsync(u => u.Username == userDto.Username))
                throw new ArgumentException("Username already exists");

            if (await context.Users.AnyAsync(u => u.Email == userDto.Email))
                throw new ArgumentException("Email already exists");

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = userDto.Password,
                FullName = userDto.FullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            return await GetUserByIdAsync(user.Id) ?? throw new Exception("User creation failed");
        }

        public async Task<UserDTO?> UpdateUserAsync(int id, UpdateUserDTO userDto)
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            if (!string.IsNullOrEmpty(userDto.FullName)) user.FullName = userDto.FullName;
            user.AvatarUrl = userDto.AvatarUrl;
            user.Bio = userDto.Bio;
            if (userDto.IsActive.HasValue) user.IsActive = userDto.IsActive.Value;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return MapToDTO(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null) return false;

            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsAsync(int id) =>
            await context.Users.AnyAsync(u => u.Id == id);

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            var user = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            return user != null && user.IsActive && user.PasswordHash == password;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            Console.WriteLine($"=== GET USER ROLES FOR USER ID: {userId} ===");

            try
            {
                // Проверяем соединение с БД
                Console.WriteLine($"Database can connect: {await context.Database.CanConnectAsync()}");

                // Проверяем таблицы
                Console.WriteLine($"Users table exists: {await context.Users.AnyAsync()}");
                Console.WriteLine($"Roles table exists: {await context.Roles.AnyAsync()}");
                Console.WriteLine($"UserRoles table exists: {await context.UserRoles.AnyAsync()}");

                // Получаем пользователя
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"User with ID {userId} not found!");
                    return new List<string>();
                }
                Console.WriteLine($"Found user: {user.Username}");

                // Получаем роли через UserRoles
                var userRoles = await context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .ToListAsync();

                Console.WriteLine($"Found {userRoles.Count} UserRole records");

                var roles = userRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .ToList();

                Console.WriteLine($"Roles from DB: {string.Join(", ", roles)}");

                // Если ролей нет, проверяем стандартные
                if (!roles.Any())
                {
                    Console.WriteLine("No roles found. Checking default assignments...");

                    // Проверяем, есть ли стандартные роли в базе
                    var defaultRoles = new[] { "Admin", "Moderator", "User" };
                    foreach (var roleName in defaultRoles)
                    {
                        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                        if (role != null)
                        {
                            Console.WriteLine($"Role '{roleName}' exists in DB with ID: {role.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Role '{roleName}' NOT found in DB!");
                        }
                    }
                }

                return roles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetUserRolesAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<string>();
            }
        }

        public async Task<bool> AssignRoleAsync(int userId, int roleId)
        {
            if (await context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
                return false;

            context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return true;
        }

        private static UserDTO MapToDTO(User user) => new(
            user.Id,
            user.Username,
            user.Email,
            user.FullName ?? string.Empty,
            user.AvatarUrl ?? string.Empty,
            user.Bio ?? string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            user.UserRoles?.Select(ur => ur.Role?.Name ?? string.Empty).ToList() ?? new List<string>()
        );
    }
}