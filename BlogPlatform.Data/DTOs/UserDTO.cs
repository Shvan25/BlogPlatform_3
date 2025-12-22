using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Data.DTOs
{
    public record UserDTO(
    int Id,
    string Username,
    string Email,
    string FullName,
    string AvatarUrl,
    string Bio,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Roles);

    public class CreateUserDTO
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно быть от 3 до 50 символов")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Имя пользователя может содержать только буквы, цифры и подчеркивание")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(100, ErrorMessage = "Email не должен превышать 100 символов")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Полное имя обязательно")]
        [StringLength(100, ErrorMessage = "Полное имя не должно превышать 100 символов")]
        public string FullName { get; set; }
    }

    public record UpdateUserDTO(
        string? FullName,
        string? AvatarUrl,
        string? Bio,
        bool? IsActive);

    public class LoginDTO
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public record AuthResponseDTO(
        string Token,
        UserDTO User,
        List<string> Roles,
        DateTime ExpiresAt);
}