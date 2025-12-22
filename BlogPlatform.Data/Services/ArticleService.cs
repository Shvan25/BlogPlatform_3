using Microsoft.EntityFrameworkCore;
using BlogPlatform.Data.Data;
using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Services
{
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;

        public ArticleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ArticleDTO>> GetAllArticlesAsync()
        {
            var articles = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.Comments)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return articles.Select(MapToDTO).ToList();
        }

        public async Task<List<ArticleDTO>> GetAllArticlesWithTagsAsync()
        {
            var articles = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return articles.Select(MapToDTOWithTags).ToList();
        }

        public async Task<ArticleDTO> GetArticleByIdAsync(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return null;

            return MapToDTOWithTags(article);
        }

        public async Task<ArticleDTO> CreateArticleAsync(CreateArticleDTO articleDto, int authorId)
        {
            // Создаем slug из заголовка
            var slug = GenerateSlug(articleDto.Title);

            var article = new Article
            {
                Title = articleDto.Title,
                Slug = slug,
                Content = articleDto.Content,
                Excerpt = articleDto.Excerpt,
                CoverImageUrl = articleDto.CoverImageUrl,
                IsPublished = articleDto.IsPublished,
                AuthorId = authorId,
                PublishedAt = articleDto.IsPublished ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Добавляем теги
            if (articleDto.TagIds != null && articleDto.TagIds.Any())
            {
                foreach (var tagId in articleDto.TagIds)
                {
                    if (await _context.Tags.AnyAsync(t => t.Id == tagId))
                    {
                        var articleTag = new ArticleTag
                        {
                            ArticleId = article.Id,
                            TagId = tagId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ArticleTags.Add(articleTag);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return await GetArticleByIdAsync(article.Id);
        }

        public async Task<ArticleDTO> UpdateArticleAsync(int id, UpdateArticleDTO articleDto)
        {
            var article = await _context.Articles
                .Include(a => a.ArticleTags)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
                return null;

            article.Title = articleDto.Title;
            article.Slug = GenerateSlug(articleDto.Title);
            article.Content = articleDto.Content;
            article.Excerpt = articleDto.Excerpt;
            article.CoverImageUrl = articleDto.CoverImageUrl;
            article.IsPublished = articleDto.IsPublished;
            article.UpdatedAt = DateTime.UtcNow;

            if (article.IsPublished && article.PublishedAt == null)
            {
                article.PublishedAt = DateTime.UtcNow;
            }

            // Обновляем теги
            if (articleDto.TagIds != null)
            {
                // Удаляем старые связи
                var existingTags = article.ArticleTags.ToList();
                _context.ArticleTags.RemoveRange(existingTags);

                // Добавляем новые связи
                foreach (var tagId in articleDto.TagIds)
                {
                    if (await _context.Tags.AnyAsync(t => t.Id == tagId))
                    {
                        var articleTag = new ArticleTag
                        {
                            ArticleId = article.Id,
                            TagId = tagId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ArticleTags.Add(articleTag);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetArticleByIdAsync(id);
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
                return false;

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ArticleDTO>> GetArticlesByTagAsync(int tagId)
        {
            var articles = await _context.ArticleTags
                .Where(at => at.TagId == tagId)
                .Include(at => at.Article)
                    .ThenInclude(a => a.Author)
                .Include(at => at.Article)
                    .ThenInclude(a => a.ArticleTags)
                        .ThenInclude(at2 => at2.Tag)
                .Select(at => at.Article)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return articles.Select(MapToDTOWithTags).ToList();
        }

        public async Task<List<ArticleDTO>> GetArticlesByAuthorAsync(int authorId)
        {
            var articles = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.AuthorId == authorId && a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return articles.Select(MapToDTOWithTags).ToList();
        }

        private ArticleDTO MapToDTO(Article article)
        {
            return new ArticleDTO
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,
                Content = article.Content,
                Excerpt = article.Excerpt,
                CoverImageUrl = article.CoverImageUrl,
                IsPublished = article.IsPublished,
                PublishedAt = article.PublishedAt,
                ViewCount = article.ViewCount,
                AuthorId = article.AuthorId,
                AuthorName = article.Author?.Username,
                AuthorEmail = article.Author?.Email,
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt
            };
        }

        private ArticleDTO MapToDTOWithTags(Article article)
        {
            var dto = MapToDTO(article);

            // Добавляем теги
            if (article.ArticleTags != null)
            {
                dto.Tags = article.ArticleTags
                    .Select(at => new TagDTO
                    {
                        Id = at.Tag.Id,
                        Name = at.Tag.Name,
                        Slug = at.Tag.Slug,
                        Description = at.Tag.Description,
                        CreatedAt = at.Tag.CreatedAt
                    })
                    .ToList();
            }

            return dto;
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            // Простая реализация генерации slug
            return title.ToLower()
                .Replace(" ", "-")
                .Replace("а", "a").Replace("б", "b").Replace("в", "v").Replace("г", "g")
                .Replace("д", "d").Replace("е", "e").Replace("ё", "yo").Replace("ж", "zh")
                .Replace("з", "z").Replace("и", "i").Replace("й", "y").Replace("к", "k")
                .Replace("л", "l").Replace("м", "m").Replace("н", "n").Replace("о", "o")
                .Replace("п", "p").Replace("р", "r").Replace("с", "s").Replace("т", "t")
                .Replace("у", "u").Replace("ф", "f").Replace("х", "kh").Replace("ц", "ts")
                .Replace("ч", "ch").Replace("ш", "sh").Replace("щ", "shch").Replace("ъ", "")
                .Replace("ы", "y").Replace("ь", "").Replace("э", "e").Replace("ю", "yu")
                .Replace("я", "ya")
                .Replace(".", "-").Replace(",", "-").Replace("!", "").Replace("?", "")
                .Replace(":", "").Replace(";", "").Replace("(", "").Replace(")", "")
                .Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "")
                .Replace("\"", "").Replace("'", "").Replace("`", "")
                .Replace("--", "-").Replace("---", "-");
        }
    }
}