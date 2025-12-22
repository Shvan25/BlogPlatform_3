using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoleApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<List<dynamic>>> GetAllRoles()
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<dynamic>> GetRoleById(int id)
        {
            var role = await _context.Roles
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return NotFound(new { message = $"Role with ID {id} not found" });
            }

            return Ok(role);
        }
    }
}