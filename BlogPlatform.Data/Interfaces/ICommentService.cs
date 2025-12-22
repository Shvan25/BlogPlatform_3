using System.Collections.Generic;
using System.Threading.Tasks;
using BlogPlatform.Data.DTOs;

namespace BlogPlatform.Data.Interfaces
{
    public interface ICommentService
    {
        Task<CommentDTO> GetCommentByIdAsync(int id);
        Task<List<CommentDTO>> GetAllCommentsAsync();
        Task<CommentDTO> CreateCommentAsync(CreateCommentDTO commentDto);
        Task<CommentDTO> UpdateCommentAsync(int id, UpdateCommentDTO commentDto);
        Task<bool> DeleteCommentAsync(int id);
        Task<bool> CommentExistsAsync(int id);
    }
}