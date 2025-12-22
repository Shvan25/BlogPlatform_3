using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlogPlatform.Controllers
{
    [Route("api/debug")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value,
                Issuer = c.Issuer
            }).ToList();

            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name,
                Claims = claims,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                IsAdmin = User.IsInRole("Admin"),
                IsModerator = User.IsInRole("Moderator")
            });
        }

        [HttpGet("check-admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult CheckAdmin()
        {
            return Ok(new
            {
                Message = "Вы администратор!",
                User = User.Identity?.Name,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("check-moderator")]
        [Authorize(Roles = "Moderator")]
        public IActionResult CheckModerator()
        {
            return Ok(new
            {
                Message = "Вы модератор!",
                User = User.Identity?.Name,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("check-admin-moderator")]
        [Authorize(Roles = "Admin,Moderator")]
        public IActionResult CheckAdminOrModerator()
        {
            return Ok(new
            {
                Message = "Вы администратор или модератор!",
                User = User.Identity?.Name,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                Timestamp = DateTime.UtcNow
            });
        }
    }
}