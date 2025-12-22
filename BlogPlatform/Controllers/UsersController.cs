using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using BlogPlatform.Services;

namespace BlogPlatform.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger,
            UserActivityLogger userActivityLogger)
            : base(logger, userActivityLogger)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Index()
        {
            LogUserActivity("Просмотр списка пользователей");

            try
            {
                var users = await _userService.GetAllUsersAsync();
                LogInformation($"Загружено {users.Count} пользователей");
                return View(users);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке списка пользователей", ex);
                return View("Error");
            }
        }

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            LogUserActivity("Просмотр профиля");

            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    LogWarning("Пользователь не аутентифицирован при просмотре профиля");
                    return RedirectToAction("Login", "Auth");
                }

                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    LogWarning($"Пользователь с ID {userId} не найден");
                    return RedirectToAction("Login", "Auth");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке профиля", ex);
                return View("Error");
            }
        }

        [HttpGet("Details/{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Details(int id)
        {
            LogInformation($"Просмотр деталей пользователя ID: {id}");

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    LogWarning($"Пользователь с ID {id} не найден");
                    return NotFound();
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "ViewUserDetails", $"Viewed user ID: {id}", null);

                return View(user);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке деталей пользователя ID: {id}", ex);
                return View("Error");
            }
        }

        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            LogInformation($"Редактирование пользователя ID: {id}");

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    LogWarning($"Пользователь с ID {id} не найден для редактирования");
                    return NotFound();
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "EditUserForm", $"Editing user ID: {id}", null);

                return View(user);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке формы редактирования пользователя ID: {id}", ex);
                return View("Error");
            }
        }

        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            LogInformation($"Подтверждение удаления пользователя ID: {id}");

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    LogWarning($"Пользователь с ID {id} не найден для удаления");
                    return NotFound();
                }

                _userActivityLogger.LogUserAction(GetCurrentUsername(), "DeleteUserConfirmation", $"Confirming deletion of user ID: {id}", null);

                return View(user);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке формы удаления пользователя ID: {id}", ex);
                return View("Error");
            }
        }
    }
}