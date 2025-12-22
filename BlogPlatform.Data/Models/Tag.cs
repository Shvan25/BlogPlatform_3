using System;
using System.Collections.Generic;

namespace BlogPlatform.Data.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    }
}