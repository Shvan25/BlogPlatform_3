using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    [Route("[controller]")]
    [Authorize(Roles = "Admin,Moderator")]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // Все роли - GET: /Role
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        // Добавление роли - GET: /Role/Create
        [HttpGet("Create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // Детали роли - GET: /Role/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        // Редактирование роли - GET: /Role/Edit/{id}
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            var standardRoles = new[] { "Admin", "Moderator", "User" };
            if (standardRoles.Contains(role.Name))
            {
                TempData["Error"] = "Нельзя редактировать стандартные роли";
                return RedirectToAction("Index");
            }

            return View(role);
        }

        // Удаление роли - GET: /Role/Delete/{id}
        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            var standardRoles = new[] { "Admin", "Moderator", "User" };
            if (standardRoles.Contains(role.Name))
            {
                TempData["Error"] = "Нельзя удалять стандартные роли";
                return RedirectToAction("Index");
            }

            return View(role);
        }
    }
}