using BlogPlatform.Data.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Interfaces
{
    public interface ITagService
    {
        Task<TagDTO?> GetTagByIdAsync(int id);
        Task<List<TagDTO>> GetAllTagsAsync();
        Task<TagDTO> CreateTagAsync(CreateTagDTO tagDto);
        Task<TagDTO?> UpdateTagAsync(int id, UpdateTagDTO tagDto);
        Task<bool> DeleteTagAsync(int id);
        Task<bool> TagExistsAsync(int id);
    }
}