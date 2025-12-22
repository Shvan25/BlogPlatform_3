using System;
using System.Collections.Generic;

namespace BlogPlatform.Data.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }

        public virtual Article Article { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Comment? Parent { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}