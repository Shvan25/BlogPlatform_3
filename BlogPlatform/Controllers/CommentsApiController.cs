using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsApiController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsApiController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<List<CommentDTO>>> GetAllComments()
        {
            var comments = await _commentService.GetAllCommentsAsync();
            return Ok(comments);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CommentDTO>> GetCommentById(int id)
        {
            var comment = await _commentService.GetCommentByIdAsync(id);
            if (comment == null)
            {
                return NotFound(new { message = $"Comment with ID {id} not found" });
            }
            return Ok(comment);
        }
    }
}