using BlogPlatform.Controllers;
using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BlogPlatform.API.Controllers
{
    /// <summary>
    /// Контроллер для управления пользователями
    /// </summary>
    [Route("api/v1/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger,
            UserActivityLogger userActivityLogger)
        {
            _userService = userService;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        private string GetCurrentUsername() => User?.Identity?.Name ?? "API User";
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        /// <summary>
        /// Получить всех пользователей
        /// </summary>
        /// <returns>Список пользователей</returns>
        /// <response code="200">Возвращает список пользователей</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(IEnumerable<UserDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
        {
            _logger.LogInformation("API: Получение всех пользователей запрошено пользователем: {Username}", GetCurrentUsername());

            try
            {
                var users = await _userService.GetAllUsersAsync();
                _logger.LogInformation("API: Найдено {Count} пользователей", users.Count);

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "GetAllUsers", $"Retrieved {users.Count} users", null);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении пользователей");
                throw;
            }
        }

        /// <summary>
        /// Получить пользователя по ID
        /// </summary>
        /// <param name="id">ID пользователя</param>
        /// <returns>Данные пользователя</returns>
        /// <response code="200">Возвращает данные пользователя</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDTO), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDTO>> GetUserById(int id)
        {
            _logger.LogInformation("API: Получение пользователя по ID: {Id} запрошено пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator");

                // Проверка прав доступа
                if (!isAdminOrModerator && id != currentUserId)
                {
                    _logger.LogWarning("API: Попытка доступа к чужому профилю. UserID: {CurrentUserId}, RequestedID: {Id}",
                        currentUserId, id);
                    return Forbid();
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("API: Пользователь с ID {Id} не найден", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "GetUserById", $"Retrieved user ID: {id}", null);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении пользователя ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Получить текущего пользователя
        /// </summary>
        /// <returns>Данные текущего пользователя</returns>
        /// <response code="200">Возвращает данные текущего пользователя</response>
        /// <response code="401">Пользователь не авторизован</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDTO), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserDTO>> GetCurrentUser()
        {
            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
            {
                return Unauthorized();
            }

            _logger.LogInformation("API: Получение текущего пользователя ID: {Id}", currentUserId);

            try
            {
                var user = await _userService.GetUserByIdAsync(currentUserId.Value);
                if (user == null)
                {
                    return NotFound(new { message = "Current user not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении текущего пользователя");
                throw;
            }
        }

        /// <summary>
        /// Обновить данные пользователя
        /// </summary>
        /// <param name="id">ID пользователя</param>
        /// <param name="updateUserDto">Данные для обновления</param>
        /// <returns>Обновленные данные пользователя</returns>
        /// <response code="200">Данные пользователя обновлены</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UserDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDTO>> UpdateUser(int id, [FromBody] UpdateUserDTO updateUserDto)
        {
            _logger.LogInformation("API: Обновление пользователя ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                // Проверка прав доступа
                if (!isAdmin && id != currentUserId)
                {
                    _logger.LogWarning("API: Попытка обновления чужого профиля. UserID: {CurrentUserId}, RequestedID: {Id}",
                        currentUserId, id);
                    return Forbid();
                }

                var user = await _userService.UpdateUserAsync(id, updateUserDto);
                if (user == null)
                {
                    _logger.LogWarning("API: Пользователь с ID {Id} не найден для обновления", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "UpdateUser",
                    $"Updated user ID: {id}", null);

                _logger.LogInformation("API: Пользователь ID: {Id} успешно обновлен", id);

                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API: Некорректные данные при обновлении пользователя ID: {Id}: {Message}",
                    id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при обновлении пользователя ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Удалить пользователя
        /// </summary>
        /// <param name="id">ID пользователя</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Пользователь удален</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation("API: Удаление пользователя ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var currentUserId = GetCurrentUserId();

                // Нельзя удалить самого себя
                if (id == currentUserId)
                {
                    _logger.LogWarning("API: Попытка удаления собственного аккаунта. UserID: {Id}", id);
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("API: Пользователь с ID {Id} не найден для удаления", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { message = "Failed to delete user" });
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "DeleteUser",
                    $"Deleted user ID: {id}, Username: {user.Username}", null);

                _logger.LogInformation("API: Пользователь ID: {Id} успешно удален", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при удалении пользователя ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Получить роли пользователя
        /// </summary>
        /// <param name="id">ID пользователя</param>
        /// <returns>Список ролей пользователя</returns>
        /// <response code="200">Возвращает список ролей</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet("{id:int}/roles")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(int id)
        {
            _logger.LogInformation("API: Получение ролей пользователя ID: {Id}", id);

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                var roles = await _userService.GetUserRolesAsync(id);

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении ролей пользователя ID: {Id}", id);
                throw;
            }
        }
    }
}