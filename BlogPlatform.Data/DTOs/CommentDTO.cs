using System;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Data.DTOs
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ArticleTitle { get; set; } = string.Empty;
    }

    public class CreateCommentDTO
    {
        [Required(ErrorMessage = "Текст комментария обязателен")]
        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        public string Content { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Неверный ID статьи")]
        public int ArticleId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Неверный ID пользователя")]
        public int UserId { get; set; }

        public int? ParentId { get; set; }
    }

    public class UpdateCommentDTO
    {
        public string? Content { get; set; }
        public bool? IsApproved { get; set; }
    }
}