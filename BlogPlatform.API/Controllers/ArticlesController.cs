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
    /// Контроллер для управления статьями
    /// </summary>
    [Route("api/v1/articles")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticlesController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public ArticlesController(
            IArticleService articleService,
            ILogger<ArticlesController> logger,
            UserActivityLogger userActivityLogger)
        {
            _articleService = articleService;
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
        /// Получить все опубликованные статьи
        /// </summary>
        /// <returns>Список статей</returns>
        /// <response code="200">Возвращает список статей</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ArticleDTO>), 200)]
        public async Task<ActionResult<IEnumerable<ArticleDTO>>> GetAllArticles()
        {
            _logger.LogInformation("API: Получение всех опубликованных статей");

            try
            {
                var articles = await _articleService.GetAllArticlesAsync();
                _logger.LogInformation("API: Найдено {Count} опубликованных статей", articles.Count);

                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статей");
                throw;
            }
        }

        /// <summary>
        /// Получить статью по ID
        /// </summary>
        /// <param name="id">ID статьи</param>
        /// <returns>Данные статьи</returns>
        /// <response code="200">Возвращает данные статьи</response>
        /// <response code="404">Статья не найдена</response>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ArticleDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArticleDTO>> GetArticleById(int id)
        {
            _logger.LogInformation("API: Получение статьи по ID: {Id}", id);

            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("API: Статья с ID {Id} не найдена", id);
                    return NotFound(new { message = $"Article with ID {id} not found" });
                }

                // Увеличиваем счетчик просмотров
                await _articleService.IncrementViewCountAsync(id);

                _userActivityLogger.LogArticleAction("View", article.Id, GetCurrentUsername(),
                    article.Title, new { ViewCount = article.ViewCount });

                return Ok(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статьи ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Создать новую статью
        /// </summary>
        /// <param name="createArticleDto">Данные для создания статьи</param>
        /// <returns>Созданная статья</returns>
        /// <response code="201">Статья успешно создана</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ArticleDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ArticleDTO>> CreateArticle([FromBody] CreateArticleDTO createArticleDto)
        {
            _logger.LogInformation("API: Создание статьи пользователем: {Username}", GetCurrentUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при создании статьи: {@Errors}",
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

                var article = await _articleService.CreateArticleAsync(createArticleDto, currentUserId.Value);

                _userActivityLogger.LogArticleAction("Create", article.Id, GetCurrentUsername(),
                    article.Title, new
                    {
                        IsPublished = createArticleDto.IsPublished,
                        TagCount = createArticleDto.TagIds?.Count ?? 0
                    });

                _logger.LogInformation("API: Статья успешно создана. ID: {Id}, Title: {Title}",
                    article.Id, article.Title);

                return CreatedAtAction(nameof(GetArticleById), new { id = article.Id }, article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при создании статьи");
                throw;
            }
        }

        /// <summary>
        /// Обновить статью
        /// </summary>
        /// <param name="id">ID статьи</param>
        /// <param name="updateArticleDto">Данные для обновления</param>
        /// <returns>Обновленная статья</returns>
        /// <response code="200">Статья успешно обновлена</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Статья не найдена</response>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ArticleDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArticleDTO>> UpdateArticle(int id, [FromBody] UpdateArticleDTO updateArticleDto)
        {
            _logger.LogInformation("API: Обновление статьи ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при обновлении статьи ID: {Id}: {@Errors}",
                    id, ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var existingArticle = await _articleService.GetArticleByIdAsync(id);
                if (existingArticle == null)
                {
                    _logger.LogWarning("API: Статья с ID {Id} не найдена для обновления", id);
                    return NotFound(new { message = $"Article with ID {id} not found" });
                }

                var currentUserId = GetCurrentUserId();
                var isAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator");

                // Проверка прав доступа
                if (existingArticle.AuthorId != currentUserId && !isAdminOrModerator)
                {
                    _logger.LogWarning("API: Попытка обновления чужой статьи. UserID: {CurrentUserId}, ArticleAuthorID: {AuthorId}",
                        currentUserId, existingArticle.AuthorId);
                    return Forbid();
                }

                var article = await _articleService.UpdateArticleAsync(id, updateArticleDto);

                _userActivityLogger.LogArticleAction("Update", article.Id, GetCurrentUsername(),
                    article.Title, new
                    {
                        OldStatus = existingArticle.IsPublished,
                        NewStatus = updateArticleDto.IsPublished,
                        TagChanges = updateArticleDto.TagIds?.Count ?? 0
                    });

                _logger.LogInformation("API: Статья ID: {Id} успешно обновлена", id);

                return Ok(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при обновлении статьи ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Удалить статью
        /// </summary>
        /// <param name="id">ID статьи</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Статья успешно удалена</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Статья не найдена</response>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            _logger.LogInformation("API: Удаление статьи ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    _logger.LogWarning("API: Статья с ID {Id} не найдена для удаления", id);
                    return NotFound(new { message = $"Article with ID {id} not found" });
                }

                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                // Проверка прав доступа
                if (article.AuthorId != currentUserId && !isAdmin)
                {
                    _logger.LogWarning("API: Попытка удаления чужой статьи. UserID: {CurrentUserId}, ArticleAuthorID: {AuthorId}",
                        currentUserId, article.AuthorId);
                    return Forbid();
                }

                var success = await _articleService.DeleteArticleAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { message = "Failed to delete article" });
                }

                _userActivityLogger.LogArticleAction("Delete", id, GetCurrentUsername(),
                    article.Title, new
                    {
                        ViewCount = article.ViewCount,
                        CommentCount = article.Comments?.Count ?? 0
                    });

                _logger.LogInformation("API: Статья ID: {Id} успешно удалена", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при удалении статьи ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Получить статьи по тегу
        /// </summary>
        /// <param name="tagId">ID тега</param>
        /// <returns>Список статей с указанным тегом</returns>
        /// <response code="200">Возвращает список статей</response>
        [HttpGet("by-tag/{tagId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ArticleDTO>), 200)]
        public async Task<ActionResult<IEnumerable<ArticleDTO>>> GetArticlesByTag(int tagId)
        {
            _logger.LogInformation("API: Получение статей по тегу ID: {TagId}", tagId);

            try
            {
                var articles = await _articleService.GetArticlesByTagAsync(tagId);
                _logger.LogInformation("API: Найдено {Count} статей с тегом ID: {TagId}",
                    articles.Count, tagId);

                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статей по тегу ID: {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Получить статьи по автору
        /// </summary>
        /// <param name="authorId">ID автора</param>
        /// <returns>Список статей указанного автора</returns>
        /// <response code="200">Возвращает список статей</response>
        [HttpGet("by-author/{authorId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ArticleDTO>), 200)]
        public async Task<ActionResult<IEnumerable<ArticleDTO>>> GetArticlesByAuthor(int authorId)
        {
            _logger.LogInformation("API: Получение статей по автору ID: {AuthorId}", authorId);

            try
            {
                var articles = await _articleService.GetArticlesByAuthorAsync(authorId);
                _logger.LogInformation("API: Найдено {Count} статей автора ID: {AuthorId}",
                    articles.Count, authorId);

                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статей по автору ID: {AuthorId}", authorId);
                throw;
            }
        }

        /// <summary>
        /// Получить неопубликованные статьи (черновики)
        /// </summary>
        /// <remarks>
        /// Только для администраторов и модераторов
        /// </remarks>
        /// <returns>Список неопубликованных статей</returns>
        /// <response code="200">Возвращает список черновиков</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpGet("drafts")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(IEnumerable<ArticleDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<ArticleDTO>>> GetDrafts()
        {
            _logger.LogInformation("API: Получение черновиков пользователем: {Username}", GetCurrentUsername());

            try
            {
                // Для получения черновиков нужно модифицировать ArticleService
                // или использовать существующие методы с фильтрацией
                var allArticles = await _articleService.GetAllArticlesWithTagsAsync();
                var drafts = allArticles.Where(a => !a.IsPublished).ToList();

                _logger.LogInformation("API: Найдено {Count} черновиков", drafts.Count);

                return Ok(drafts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении черновиков");
                throw;
            }
        }
    }
}