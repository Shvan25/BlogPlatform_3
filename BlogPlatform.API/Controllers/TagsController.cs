using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BlogPlatform.Controllers;

namespace BlogPlatform.API.Controllers
{
    /// <summary>
    /// Контроллер для управления тегами
    /// </summary>
    [Route("api/v1/tags")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly ILogger<TagsController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public TagsController(
            ITagService tagService,
            ILogger<TagsController> logger,
            UserActivityLogger userActivityLogger)
        {
            _tagService = tagService;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        private string GetCurrentUsername() => User?.Identity?.Name ?? "Anonymous";

        /// <summary>
        /// Получить все теги
        /// </summary>
        /// <returns>Список тегов</returns>
        /// <response code="200">Возвращает список тегов</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<TagDTO>), 200)]
        public async Task<ActionResult<IEnumerable<TagDTO>>> GetAllTags()
        {
            _logger.LogInformation("API: Получение всех тегов");

            try
            {
                var tags = await _tagService.GetAllTagsAsync();
                _logger.LogInformation("API: Найдено {Count} тегов", tags.Count);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении тегов");
                throw;
            }
        }

        /// <summary>
        /// Получить тег по ID
        /// </summary>
        /// <param name="id">ID тега</param>
        /// <returns>Данные тега</returns>
        /// <response code="200">Возвращает данные тега</response>
        /// <response code="404">Тег не найден</response>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TagDTO), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TagDTO>> GetTagById(int id)
        {
            _logger.LogInformation("API: Получение тега по ID: {Id}", id);

            try
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                if (tag == null)
                {
                    _logger.LogWarning("API: Тег с ID {Id} не найден", id);
                    return NotFound(new { message = $"Tag with ID {id} not found" });
                }

                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении тега ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Создать новый тег
        /// </summary>
        /// <param name="createTagDto">Данные для создания тега</param>
        /// <returns>Созданный тег</returns>
        /// <response code="201">Тег успешно создан</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(TagDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<TagDTO>> CreateTag([FromBody] CreateTagDTO createTagDto)
        {
            _logger.LogInformation("API: Создание тега пользователем: {Username}", GetCurrentUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при создании тега: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var tag = await _tagService.CreateTagAsync(createTagDto);

                _userActivityLogger.LogTagAction("Create", tag.Id, tag.Name, GetCurrentUsername());

                _logger.LogInformation("API: Тег успешно создан. ID: {Id}, Name: {Name}",
                    tag.Id, tag.Name);

                return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, tag);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API: Некорректные данные при создании тега: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при создании тега");
                throw;
            }
        }

        /// <summary>
        /// Обновить тег
        /// </summary>
        /// <param name="id">ID тега</param>
        /// <param name="updateTagDto">Данные для обновления</param>
        /// <returns>Обновленный тег</returns>
        /// <response code="200">Тег успешно обновлен</response>
        /// <response code="400">Некорректные данные</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Тег не найден</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(TagDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TagDTO>> UpdateTag(int id, [FromBody] UpdateTagDTO updateTagDto)
        {
            _logger.LogInformation("API: Обновление тега ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API: Невалидные данные при обновлении тега ID: {Id}: {@Errors}",
                    id, ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var tag = await _tagService.UpdateTagAsync(id, updateTagDto);
                if (tag == null)
                {
                    _logger.LogWarning("API: Тег с ID {Id} не найден для обновления", id);
                    return NotFound(new { message = $"Tag with ID {id} not found" });
                }

                _userActivityLogger.LogTagAction("Update", tag.Id, tag.Name, GetCurrentUsername());

                _logger.LogInformation("API: Тег ID: {Id} успешно обновлен", id);

                return Ok(tag);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("API: Некорректные данные при обновлении тега ID: {Id}: {Message}",
                    id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при обновлении тега ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Удалить тег
        /// </summary>
        /// <param name="id">ID тега</param>
        /// <returns>Результат операции</returns>
        /// <response code="204">Тег успешно удален</response>
        /// <response code="401">Пользователь не авторизован</response>
        /// <response code="403">Недостаточно прав</response>
        /// <response code="404">Тег не найден</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteTag(int id)
        {
            _logger.LogInformation("API: Удаление тега ID: {Id} пользователем: {Username}",
                id, GetCurrentUsername());

            try
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                if (tag == null)
                {
                    _logger.LogWarning("API: Тег с ID {Id} не найден для удаления", id);
                    return NotFound(new { message = $"Tag with ID {id} not found" });
                }

                var success = await _tagService.DeleteTagAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { message = "Failed to delete tag" });
                }

                _userActivityLogger.LogTagAction("Delete", id, tag.Name, GetCurrentUsername());

                _logger.LogInformation("API: Тег ID: {Id} успешно удален", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при удалении тега ID: {Id}", id);
                throw;
            }
        }
    }
}
