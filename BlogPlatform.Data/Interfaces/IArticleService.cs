using BlogPlatform.Data.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Interfaces
{
    public interface IArticleService
    {
        Task<List<ArticleDTO>> GetAllArticlesAsync();
        Task<List<ArticleDTO>> GetAllArticlesWithTagsAsync();
        Task<ArticleDTO> GetArticleByIdAsync(int id);
        Task<ArticleDTO> CreateArticleAsync(CreateArticleDTO articleDto, int authorId);
        Task<ArticleDTO> UpdateArticleAsync(int id, UpdateArticleDTO articleDto);
        Task<bool> DeleteArticleAsync(int id);
        Task IncrementViewCountAsync(int id);
        Task<List<ArticleDTO>> GetArticlesByTagAsync(int tagId);
        Task<List<ArticleDTO>> GetArticlesByAuthorAsync(int authorId);
    }
}