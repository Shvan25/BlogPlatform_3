using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using BlogPlatform.Controllers;

namespace BlogPlatform.API.Controllers
{
    /// <summary>
    /// Контроллер для управления комментариями
    /// </summary>
    [Route("api/v1/comments")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentsController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public CommentsController(
            ICommentService commentService,
            ILogger<CommentsController> logger,
            UserActivityLogger userActivityLogger)
        {
            _commentService = commentService;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        private string GetCurrentUsername() => User?.Identity?.Name ?? "Anonymous";
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        /// <summary>
        /// Получить все комментарии
        /// </summary>
        /// <remarks>
        /// Только для администраторов и модераторов
        /// </remarks>
        /// <returns>Список всех комментариев</returns>
        /// <response code="200">Возвращает список комментариев</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(IEnumerable<CommentDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<CommentDTO>>> GetAllComments()
        {
            _logger.LogInformation("API: Получение всех комментариев пользователем: {Username}", GetCurrentUsername());

            try
            {
                var comments = await _commentService.GetAllCommentsAsync();
                _logger.LogInformation("API: Найдено {Count} комментариев", comments.Count);

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении комментариев");
                throw;
            }
        }

        /// <summary>
        /// Получить комментарий по ID
        /// </summary>
        /// <param name="id">ID комментария</param>
        /// <returns>Данные комментария</returns>
        /// <response code="200">Возвращает данные комментария</response>
        /// <response code="404">Комментарий не найден</response>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CommentDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CommentDTO>> GetCommentById(int id)
        {
            _logger.LogInformation("API: Получение комментария по ID: {Id}", id);

            try
            {
                var comment = await _commentService.GetCommentByIdAsync(id);
                if (comment == null)
                {
                    _logger.LogWarning("API: Комментарий с ID {Id} не найдена", id);
                    return NotFound(new { message = $"Comment with ID {id} not found" });
                }

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении комментария ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Получить комментарии к статье
        /// </summary>
        /// <param name="articleId">ID статьи</param>
        /// <returns>Список комментариев к статье</returns>
        /// <response code="200">Возвращает список комментариев</response>
        [HttpGet("by-article/{articleId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CommentDTO>), 200)]
        public async Task<ActionResult<IEnumerable<CommentDTO>>> GetCommentsByArticle(int articleId)
        {
            _logger.LogInformation("API: Получение комментариев к статье ID: {ArticleId}", articleId);

            try
            {
                // Здесь нужно либо добавить метод в сервис, либо фильтровать
                var allComments = await _commentService.GetAllCommentsAsync();
                var articleComments = allComments.Where(c => c.ArticleId == articleId).ToList();

                _logger.LogInformation("API: Найдено {Count} комментариев к статье ID: {ArticleId}",
                    articleComments.Count, articleId);

                return Ok(articleComments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении комментариев к статье ID: {ArticleId}", articleId);
                throw;
            }
        }

        /// <summary>
        /// Создать новый комментарий
        /// </summary>
        /// <param name="createCommentDto">Данные для создания комментария</param>
        /// <returns>Созданный комментарий</returns>
        /// <response code="201">Комментарий успешно создан</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CommentDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<CommentDTO>> CreateComment([FromBody] CreateCommentDTO createCommentDto)
        {
            _logger.LogInformation("API: Создание комментария пользователем: {Username}", GetCurrentUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при создании комментария: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized();
                }

                // Устанавливаем текущего пользователя как автора комментария
                createCommentDto.UserId = currentUserId.Value;

                var comment = await _commentService.CreateCommentAsync(createCommentDto);

                _userActivityLogger.LogCommentAction("Create", comment.Id, GetCurrentUsername(),
                    comment.ArticleId, new
                    {
                        ArticleId = createCommentDto.ArticleId,
                        HasParent = createCommentDto.ParentId.HasValue
                    });

                _logger.LogInformation("API: Комментарий успешно создан. ID: {Id}, ArticleID: {ArticleId}",
                    comment.Id, comment.ArticleId);

                return CreatedAtAction(nameof(GetCommentById), new { id = comment.Id }, comment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API: Некорректные данные при создании комментария: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при создании комментария");
                throw;
            }
        }

        /// <summary>
        /// Обновить комментарий
        /// </summary>
        /// <param name="id">ID комментария</param>
        /// <param name="updateCommentDto">Данные для обновления</param>
        /// <returns>Обновленный комментарий</returns>
        /// <response code="200">Комментарий успешно обновлен</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Комментарий не найден</response>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(CommentDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CommentDTO>> UpdateComment(int id, [FromBody] UpdateCommentDTO updateCommentDto)
        {
            _logger.LogInformation("API: Обновление комментария ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var existingComment = await _commentService.GetCommentByIdAsync(id);
                if (existingComment == null)
                {
                    _logger.LogWarning("API: Комментарий с ID {Id} не найден для обновления", id);
                    return NotFound(new { message = $"Comment with ID {id} not found" });
                }

                var currentUserId = GetCurrentUserId();
                var isAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator");

                // Проверка прав доступа
                if (existingComment.UserId != currentUserId && !isAdminOrModerator)
                {
                    _logger.LogWarning("API: Попытка обновления чужого комментария. UserID: {CurrentUserId}, CommentAuthorID: {AuthorId}",
                        currentUserId, existingComment.UserId);
                    return Forbid();
                }

                var comment = await _commentService.UpdateCommentAsync(id, updateCommentDto);

                _userActivityLogger.LogCommentAction("Update", comment.Id, GetCurrentUsername(),
                    comment.ArticleId, new
                    {
                        OldApproval = existingComment.IsApproved,
                        NewApproval = updateCommentDto.IsApproved
                    });

                _logger.LogInformation("API: Комментарий ID: {Id} успешно обновлен", id);

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при обновлении комментария ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Удалить комментарий
        /// </summary>
        /// <param name="id">ID комментария</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Комментарий успешно удален</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Комментарий не найден</response>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            _logger.LogInformation("API: Удаление комментария ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var comment = await _commentService.GetCommentByIdAsync(id);
                if (comment == null)
                {
                    _logger.LogWarning("API: Комментарий с ID {Id} не найден для удаления", id);
                    return NotFound(new { message = $"Comment with ID {id} not found" });
                }

                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                // Проверка прав доступа
                if (comment.UserId != currentUserId && !isAdmin)
                {
                    _logger.LogWarning("API: Попытка удаления чужого комментария. UserID: {CurrentUserId}, CommentAuthorID: {AuthorId}",
                        currentUserId, comment.UserId);
                    return Forbid();
                }

                var success = await _commentService.DeleteCommentAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { message = "Failed to delete comment" });
                }

                _userActivityLogger.LogCommentAction("Delete", id, GetCurrentUsername(),
                    comment.ArticleId, new { ContentPreview = comment.Content.Substring(0, Math.Min(50, comment.Content.Length)) });

                _logger.LogInformation("API: Комментарий ID: {Id} успешно удален", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при удалении комментария ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Одобрить комментарий
        /// </summary>
        /// <remarks>
        /// Только для администраторов и модераторов
        /// </remarks>
        /// <param name="id">ID комментария</param>
        /// <returns>Обновленный комментарий</returns>
        /// <response code="200">Комментарий успешно одобрен</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Комментарий не найден</response>
        [HttpPatch("{id:int}/approve")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(CommentDTO), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CommentDTO>> ApproveComment(int id)
        {
            _logger.LogInformation("API: Одобрение комментария ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var updateDto = new UpdateCommentDTO { IsApproved = true };
                var comment = await _commentService.UpdateCommentAsync(id, updateDto);

                if (comment == null)
                {
                    return NotFound(new { message = $"Comment with ID {id} not found" });
                }

                _userActivityLogger.LogCommentAction("Approve", comment.Id, GetCurrentUsername(),
                    comment.ArticleId, null);

                _logger.LogInformation("API: Комментарий ID: {Id} успешно одобрен", id);

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при одобрении комментария ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Отклонить комментарий
        /// </summary>
        /// <remarks>
        /// Только для администраторов и модераторов
        /// </remarks>
        /// <param name="id">ID комментария</param>
        /// <returns>Обновленный комментарий</returns>
        /// <response code="200">Комментарий успешно отклонен</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Комментарий не найден</response>
        [HttpPatch("{id:int}/reject")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(CommentDTO), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CommentDTO>> RejectComment(int id)
        {
            _logger.LogInformation("API: Отклонение комментария ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var updateDto = new UpdateCommentDTO { IsApproved = false };
                var comment = await _commentService.UpdateCommentAsync(id, updateDto);

                if (comment == null)
                {
                    return NotFound(new { message = $"Comment with ID {id} not found" });
                }

                _userActivityLogger.LogCommentAction("Reject", comment.Id, GetCurrentUsername(),
                    comment.ArticleId, null);

                _logger.LogInformation("API: Комментарий ID: {Id} успешно отклонен", id);

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при отклонении комментария ID: {Id}", id);
                throw;
            }
        }
    }
}