using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        // Все комментарии - GET: /Comments
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Index()
        {
            var comments = await _commentService.GetAllCommentsAsync();
            return View(comments);
        }

        // Детали комментария - GET: /Comments/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
                return NotFound();

            return View(comment);
        }

        // Редактирование комментария - GET: /Comments/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (comment.UserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                return Forbid();

            return View(comment);
        }

        // Удаление комментария - GET: /Comments/Delete/{id}
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (comment.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            return View(comment);
        }
    }
}