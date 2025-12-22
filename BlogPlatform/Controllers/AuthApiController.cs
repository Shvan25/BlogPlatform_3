using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using BlogPlatform.Services;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthApiController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public AuthApiController(
            IUserService userService,
            IConfiguration configuration,
            ILogger<AuthApiController> logger,
            UserActivityLogger userActivityLogger)
        {
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Невалидные данные при попытке входа: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Попытка входа пользователя: {Username}", loginDto.Username);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var isAuthenticated = await _userService.AuthenticateAsync(loginDto.Username, loginDto.Password);
                if (!isAuthenticated)
                {
                    _userActivityLogger.LogLogin(loginDto.Username, false, ipAddress, "Invalid credentials");
                    _logger.LogWarning("Неудачная попытка входа: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                var user = await _userService.GetUserByUsernameAsync(loginDto.Username);
                if (user == null)
                {
                    _userActivityLogger.LogLogin(loginDto.Username, false, ipAddress, "User not found");
                    _logger.LogError("Пользователь не найден после успешной аутентификации: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "User not found" });
                }

                var roles = await _userService.GetUserRolesAsync(user.Id);
                _logger.LogInformation("Успешный вход пользователя: {Username} (ID: {UserId}), роли: {Roles}",
                    user.Username, user.Id, string.Join(", ", roles));

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.Email, user.Email),
                    new("fullname", user.FullName)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = GenerateJwtToken(claims);

                _userActivityLogger.LogLogin(loginDto.Username, true, ipAddress);
                _userActivityLogger.LogUserAction(loginDto.Username, "Login", "Successful authentication", ipAddress);

                return Ok(new AuthResponseDTO(token, user, roles, DateTime.UtcNow.AddHours(1)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя: {Username}", loginDto.Username);
                _userActivityLogger.LogError("Login error", ex, loginDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDTO userDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Невалидные данные при регистрации: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Попытка регистрации пользователя: {Username}", userDto.Username);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var createdUser = await _userService.CreateUserAsync(userDto);
                _logger.LogInformation("Пользователь успешно зарегистрирован: {Username} (ID: {UserId})",
                    createdUser.Username, createdUser.Id);

                _userActivityLogger.LogUserAction(userDto.Username, "Register", "New user registration", ipAddress);

                // Возвращаем успешный результат без автоматического входа
                return Ok(new
                {
                    message = "Registration successful. You can now login.",
                    userId = createdUser.Id,
                    username = createdUser.Username
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Ошибка при регистрации пользователя {Username}: {Message}",
                    userDto.Username, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя: {Username}", userDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var username = User.Identity?.Name;
            _logger.LogDebug("Валидация токена для пользователя: {Username}", username);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            return Ok(new
            {
                message = "Token is valid",
                userId = userId,
                username = username,
                roles = roles,
                isValid = true
            });
        }

        private string GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "super_secret_key_1234567890!@#$%^&*()"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "BlogPlatform",
                audience: _configuration["Jwt:Audience"] ?? "BlogPlatformUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}