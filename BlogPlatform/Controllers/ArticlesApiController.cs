using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using BlogPlatform.Services;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesApiController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticlesApiController> _logger;
        private readonly UserActivityLogger _userActivityLogger;

        public ArticlesApiController(
            IArticleService articleService,
            ILogger<ArticlesApiController> logger,
            UserActivityLogger userActivityLogger)
        {
            _articleService = articleService;
            _logger = logger;
            _userActivityLogger = userActivityLogger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ArticleDTO>>> GetAllArticles()
        {
            _logger.LogInformation("API: Получение всех статей");

            try
            {
                var articles = await _articleService.GetAllArticlesAsync();
                _logger.LogInformation("API: Найдено {Count} статей", articles.Count);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статей");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
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

                _userActivityLogger.LogArticleAction("View", article.Id, "API User", article.Title, new { ViewCount = article.ViewCount });
                return Ok(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Ошибка при получении статьи ID: {Id}", id);
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }
    }
}