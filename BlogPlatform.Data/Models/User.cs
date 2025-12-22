using System;
using System.Collections.Generic;

namespace BlogPlatform.Data.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

}