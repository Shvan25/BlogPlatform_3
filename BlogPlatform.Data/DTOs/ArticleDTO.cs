using System;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Data.DTOs
{
    public class ArticleDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public string CoverImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Добавляем свойство для тегов
        public List<TagDTO> Tags { get; set; } = new List<TagDTO>();

        // Свойство для комментариев (если нужно)
        public List<CommentDTO> Comments { get; set; } = new List<CommentDTO>();
    }

    public class CreateArticleDTO
    {
        [Required(ErrorMessage = "Заголовок обязателен")]
        [StringLength(200, ErrorMessage = "Заголовок не должен превышать 200 символов")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Содержание обязательно")]
        public string Content { get; set; }

        [StringLength(500, ErrorMessage = "Краткое содержание не должно превышать 500 символов")]
        public string Excerpt { get; set; }

        [Url(ErrorMessage = "Неверный формат URL для изображения")]
        public string CoverImageUrl { get; set; }

        public bool IsPublished { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class UpdateArticleDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Excerpt { get; set; }
        public string CoverImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
    }
}