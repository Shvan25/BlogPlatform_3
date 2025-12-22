using System;
using System.Collections.Generic;

namespace BlogPlatform.Data.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsPublished { get; set; } = false;
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int AuthorId { get; set; }

        public virtual User? Author { get; set; }
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    }
}