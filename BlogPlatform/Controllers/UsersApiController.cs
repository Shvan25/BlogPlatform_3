using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersApiController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<List<UserDTO>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDTO>> GetUserById(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator");

            if (!isAdminOrModerator && id != currentUserId)
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
    }
}