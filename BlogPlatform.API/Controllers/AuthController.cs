using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogPlatform.API.Controllers
{
    /// <summary>
    /// Контроллер для аутентификации и регистрации
    /// </summary>
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public AuthController(
            IUserService userService,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            UserActivityLogger userActivityLogger)
        {
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        /// <summary>
        /// Вход в систему
        /// </summary>
        /// <param name="loginDto">Данные для входа</param>
        /// <returns>JWT токен и данные пользователя</returns>
        /// <response code="200">Успешный вход</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Неверные учетные данные</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при попытке входа: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("API: Попытка входа пользователя: {Username}", loginDto.Username);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var isAuthenticated = await _userService.AuthenticateAsync(loginDto.Username, loginDto.Password);
                if (!isAuthenticated)
                {
                    _userActivityLogger.LogLogin(loginDto.Username, false, ipAddress, "Invalid credentials");
                    _logger.LogWarning("API: Неудачная попытка входа: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                var user = await _userService.GetUserByUsernameAsync(loginDto.Username);
                if (user == null)
                {
                    _userActivityLogger.LogLogin(loginDto.Username, false, ipAddress, "User not found");
                    _logger.LogError("API: Пользователь не найден после успешной аутентификации: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "User not found" });
                }

                var roles = await _userService.GetUserRolesAsync(user.Id);
                _logger.LogInformation("API: Успешный вход пользователя: {Username} (ID: {UserId}), роли: {Roles}",
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
                _logger.LogError(ex, "API: Ошибка при входе пользователя: {Username}", loginDto.Username);
                _userActivityLogger.LogError("Login error", ex, loginDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="createUserDto">Данные для регистрации</param>
        /// <returns>Результат регистрации</returns>
        /// <response code="201">Пользователь успешно зарегистрирован</response>
        /// <response code="400">Некорректные данные или пользователь уже существует</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] CreateUserDTO createUserDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при регистрации: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("API: Попытка регистрации пользователя: {Username}", createUserDto.Username);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                var createdUser = await _userService.CreateUserAsync(createUserDto);
                _logger.LogInformation("API: Пользователь успешно зарегистрирован: {Username} (ID: {UserId})",
                    createdUser.Username, createdUser.Id);

                _userActivityLogger.LogUserAction(createUserDto.Username, "Register", "New user registration", ipAddress);
                _userActivityLogger.LogLogin(createUserDto.Username, true, ipAddress, "New registration");

                return CreatedAtAction(nameof(Login), new LoginDTO
                {
                    Username = createUserDto.Username,
                    Password = createUserDto.Password
                }, new
                {
                    message = "Registration successful. Please login.",
                    userId = createdUser.Id,
                    username = createdUser.Username
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API: Ошибка при регистрации пользователя {Username}: {Message}",
                    createUserDto.Username, ex.Message);
                _userActivityLogger.LogLogin(createUserDto.Username, false, ipAddress, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при регистрации пользователя: {Username}", createUserDto.Username);
                _userActivityLogger.LogError("Registration error", ex, createUserDto.Username);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Проверить валидность токена
        /// </summary>
        /// <returns>Данные из токена</returns>
        /// <response code="200">Токен валиден</response>
        /// <response code="401">Токен не валиден или отсутствует</response>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public IActionResult ValidateToken()
        {
            var username = User.Identity?.Name;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            _logger.LogDebug("API: Валидация токена для пользователя: {Username}", username);

            return Ok(new
            {
                message = "Token is valid",
                userId = userId,
                username = username,
                email = email,
                roles = roles,
                isValid = true,
                expiresAt = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
            });
        }

        /// <summary>
        /// Обновить токен
        /// </summary>
        /// <returns>Новый JWT токен</returns>
        /// <response code="200">Токен успешно обновлен</response>
        /// <response code="401">Токен не валиден</response>
        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RefreshToken()
        {
            var username = User.Identity?.Name;
            _logger.LogInformation("API: Обновление токена для пользователя: {Username}", username);

            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var roles = await _userService.GetUserRolesAsync(user.Id);

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

                _userActivityLogger.LogUserAction(username, "RefreshToken", "Token refreshed",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new
                {
                    token = token,
                    user = user,
                    roles = roles,
                    expiresAt = DateTime.UtcNow.AddHours(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при обновлении токена для пользователя: {Username}", username);
                return StatusCode(500, new { message = "Internal server error" });
            }
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