using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using BlogPlatform.Data.Models;
using BlogPlatform.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommentDTO> GetCommentByIdAsync(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Article)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return null;

            return MapToDTO(comment);
        }

        public async Task<List<CommentDTO>> GetAllCommentsAsync()
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Article)
                .ToListAsync();

            return comments.Select(MapToDTO).ToList();
        }

        public async Task<CommentDTO> CreateCommentAsync(CreateCommentDTO commentDto)
        {
            // Проверка существования статьи
            var article = await _context.Articles.FindAsync(commentDto.ArticleId);
            if (article == null)
                throw new ArgumentException("Article not found");

            // Проверка существования пользователя
            var user = await _context.Users.FindAsync(commentDto.UserId);
            if (user == null)
                throw new ArgumentException("User not found");

            // Если есть parentId, проверяем существование родительского комментария
            if (commentDto.ParentId.HasValue)
            {
                var parentComment = await _context.Comments.FindAsync(commentDto.ParentId.Value);
                if (parentComment == null)
                    throw new ArgumentException("Parent comment not found");
            }

            var comment = new Comment
            {
                Content = commentDto.Content,
                ArticleId = commentDto.ArticleId,
                UserId = commentDto.UserId,
                ParentId = commentDto.ParentId,
                IsApproved = false, // По умолчанию не одобрен
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return await GetCommentByIdAsync(comment.Id);
        }

        public async Task<CommentDTO> UpdateCommentAsync(int id, UpdateCommentDTO commentDto)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return null;

            if (!string.IsNullOrEmpty(commentDto.Content))
                comment.Content = commentDto.Content;

            if (commentDto.IsApproved.HasValue)
                comment.IsApproved = commentDto.IsApproved.Value;

            comment.UpdatedAt = DateTime.UtcNow;

            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return await GetCommentByIdAsync(id);
        }

        public async Task<bool> DeleteCommentAsync(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CommentExistsAsync(int id)
        {
            return await _context.Comments.AnyAsync(e => e.Id == id);
        }

        private CommentDTO MapToDTO(Comment comment)
        {
            return new CommentDTO
            {
                Id = comment.Id,
                Content = comment.Content,
                IsApproved = comment.IsApproved,
                ArticleId = comment.ArticleId,
                UserId = comment.UserId,
                ParentId = comment.ParentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                UserName = comment.User?.Username,
                ArticleTitle = comment.Article?.Title
            };
        }
    }
}