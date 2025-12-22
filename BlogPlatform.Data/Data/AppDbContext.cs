using BlogPlatform.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Data.Data
{

    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Article> Articles => Set<Article>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Username).IsRequired().HasMaxLength(50);
                e.HasIndex(u => u.Username).IsUnique();
                e.Property(u => u.Email).IsRequired().HasMaxLength(100);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                e.Property(u => u.FullName).HasMaxLength(100);
                e.Property(u => u.IsActive).HasDefaultValue(true);
                e.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Role
            modelBuilder.Entity<Role>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Name).IsRequired().HasMaxLength(50);
                e.HasIndex(r => r.Name).IsUnique();
                e.Property(r => r.Description).HasMaxLength(200);
                e.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // UserRole
            modelBuilder.Entity<UserRole>(e =>
            {
                e.HasKey(ur => new { ur.UserId, ur.RoleId });
                e.Property(ur => ur.AssignedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
            });

            // Article
            modelBuilder.Entity<Article>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Title).IsRequired().HasMaxLength(255);
                e.Property(a => a.Slug).IsRequired().HasMaxLength(255);
                e.HasIndex(a => a.Slug).IsUnique();
                e.Property(a => a.Content).IsRequired();
                e.Property(a => a.IsPublished).HasDefaultValue(false);
                e.Property(a => a.ViewCount).HasDefaultValue(0);
                e.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(a => a.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasOne(a => a.Author).WithMany(u => u.Articles).HasForeignKey(a => a.AuthorId).OnDelete(DeleteBehavior.Cascade);
            });

            // Tag
            modelBuilder.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(50);
                e.HasIndex(t => t.Name).IsUnique();
                e.Property(t => t.Slug).IsRequired().HasMaxLength(50);
                e.HasIndex(t => t.Slug).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Comment
            modelBuilder.Entity<Comment>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Content).IsRequired();
                e.Property(c => c.IsApproved).HasDefaultValue(false);
                e.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(c => c.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasOne(c => c.Article).WithMany(a => a.Comments).HasForeignKey(c => c.ArticleId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Parent).WithMany(c => c.Replies).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Cascade);
            });

            // ArticleTag
            modelBuilder.Entity<ArticleTag>(e =>
            {
                e.HasKey(at => new { at.ArticleId, at.TagId });
                e.Property(at => at.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.HasOne(at => at.Article).WithMany(a => a.ArticleTags).HasForeignKey(at => at.ArticleId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(at => at.Tag).WithMany(t => t.ArticleTags).HasForeignKey(at => at.TagId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}