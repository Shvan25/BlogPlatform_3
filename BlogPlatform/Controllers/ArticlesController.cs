using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using BlogPlatform.Services;

namespace BlogPlatform.Controllers
{
    public class ArticlesController : BaseController
    {
        private readonly IArticleService _articleService;
        private readonly ITagService _tagService;

        public ArticlesController(
            IArticleService articleService,
            ITagService tagService,
            ILogger<ArticlesController> logger,
            UserActivityLogger userActivityLogger)
            : base(logger, userActivityLogger)
        {
            _articleService = articleService;
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            LogInformation("Просмотр списка статей");

            try
            {
                var articles = await _articleService.GetAllArticlesWithTagsAsync();
                LogInformation($"Загружено {articles.Count} статей");
                return View(articles);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке списка статей", ex);
                return View("Error");
            }
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            LogInformation($"Просмотр статьи ID: {id}");

            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    LogWarning($"Статья с ID {id} не найдена");
                    return NotFound();
                }

                await _articleService.IncrementViewCountAsync(id);
                _userActivityLogger.LogArticleAction("View", article.Id, GetCurrentUsername(), article.Title);

                return View(article);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при просмотре статьи ID: {id}", ex);
                return StatusCode(500);
            }
        }

        [HttpGet("Create")]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            LogUserActivity("Переход к созданию статьи");

            try
            {
                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;
                return View();
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке формы создания статьи", ex);
                return View("Error");
            }
        }

        [HttpPost("Create")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateArticleDTO articleDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Невалидные данные при создании статьи пользователем {Username}: {@Errors}",
                    GetCurrentUsername(), ModelState.Values.SelectMany(v => v.Errors));

                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;
                return View(articleDto);
            }

            try
            {
                var userId = GetCurrentUserId();
                LogInformation($"Создание статьи: {articleDto.Title}");

                var article = await _articleService.CreateArticleAsync(articleDto, userId.Value);

                _userActivityLogger.LogArticleAction("Create", article.Id, GetCurrentUsername(), article.Title,
                    new { IsPublished = articleDto.IsPublished, TagCount = articleDto.TagIds?.Count ?? 0 });

                LogInformation($"Статья успешно создана: ID={article.Id}, Title={article.Title}");

                return RedirectToAction(nameof(Details), new { id = article.Id });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при создании статьи: {articleDto.Title}", ex);

                ModelState.AddModelError("", "Произошла ошибка при создании статьи");
                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;
                return View(articleDto);
            }
        }

        [HttpGet("Edit/{id}")]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            LogInformation($"Редактирование статьи ID: {id}");

            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    LogWarning($"Статья с ID {id} не найдена для редактирования");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (article.AuthorId != userId && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    LogWarning($"Попытка редактирования чужой статьи ID: {id} пользователем {GetCurrentUsername()}");
                    return Forbid();
                }

                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;

                var model = new UpdateArticleDTO
                {
                    Title = article.Title,
                    Content = article.Content,
                    Excerpt = article.Excerpt,
                    CoverImageUrl = article.CoverImageUrl,
                    IsPublished = article.IsPublished,
                    TagIds = article.Tags.Select(t => t.Id).ToList()
                };

                ViewBag.Article = article;

                _userActivityLogger.LogArticleAction("EditForm", article.Id, GetCurrentUsername(), article.Title);

                return View(model);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке формы редактирования статьи ID: {id}", ex);
                return View("Error");
            }
        }

        [HttpPost("Edit/{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateArticleDTO articleDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Невалидные данные при редактировании статьи ID: {Id} пользователем {Username}: {@Errors}",
                    id, GetCurrentUsername(), ModelState.Values.SelectMany(v => v.Errors));

                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;
                return View(articleDto);
            }

            try
            {
                var existingArticle = await _articleService.GetArticleByIdAsync(id);
                if (existingArticle == null)
                {
                    LogWarning($"Статья с ID {id} не найдена для обновления");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (existingArticle.AuthorId != userId && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    LogWarning($"Попытка редактирования чужой статьи ID: {id} пользователем {GetCurrentUsername()}");
                    return Forbid();
                }

                LogInformation($"Обновление статьи ID: {id}");

                var article = await _articleService.UpdateArticleAsync(id, articleDto);

                _userActivityLogger.LogArticleAction("Update", article.Id, GetCurrentUsername(), article.Title,
                    new
                    {
                        OldStatus = existingArticle.IsPublished,
                        NewStatus = articleDto.IsPublished,
                        TagChanges = articleDto.TagIds?.Count ?? 0
                    });

                LogInformation($"Статья ID: {id} успешно обновлена");

                return RedirectToAction(nameof(Details), new { id = article.Id });
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при обновлении статьи ID: {id}", ex);

                ModelState.AddModelError("", "Произошла ошибка при обновлении статьи");
                var tags = await _tagService.GetAllTagsAsync();
                ViewBag.Tags = tags;
                return View(articleDto);
            }
        }

        [HttpGet("Delete/{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            LogInformation($"Подтверждение удаления статьи ID: {id}");

            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    LogWarning($"Статья с ID {id} не найдена для удаления");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (article.AuthorId != userId && !User.IsInRole("Admin"))
                {
                    LogWarning($"Попытка удаления чужой статьи ID: {id} пользователем {GetCurrentUsername()}");
                    return Forbid();
                }

                _userActivityLogger.LogArticleAction("DeleteConfirmation", article.Id, GetCurrentUsername(), article.Title);

                return View(article);
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при загрузке формы удаления статьи ID: {id}", ex);
                return View("Error");
            }
        }

        [HttpPost("Delete/{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    LogWarning($"Статья с ID {id} не найдена для удаления");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (article.AuthorId != userId && !User.IsInRole("Admin"))
                {
                    LogWarning($"Попытка удаления чужой статьи ID: {id} пользователем {GetCurrentUsername()}");
                    return Forbid();
                }

                LogInformation($"Удаление статьи ID: {id}");

                var title = article.Title;
                await _articleService.DeleteArticleAsync(id);

                _userActivityLogger.LogArticleAction("Delete", id, GetCurrentUsername(), title,
                    new { ViewCount = article.ViewCount, CommentCount = article.Comments?.Count ?? 0 });

                LogInformation($"Статья ID: {id} успешно удалена");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при удалении статьи ID: {id}", ex);
                return View("Error");
            }
        }
    }
}