using BlogPlatform.Data.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogPlatform.Data.Interfaces
{
    public interface IUserService
    {
        Task<UserDTO?> GetUserByIdAsync(int id);
        Task<UserDTO?> GetUserByUsernameAsync(string username);
        Task<List<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> CreateUserAsync(CreateUserDTO userDto);
        Task<UserDTO?> UpdateUserAsync(int id, UpdateUserDTO userDto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UserExistsAsync(int id);
        Task<bool> AuthenticateAsync(string username, string password);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> AssignRoleAsync(int userId, int roleId);
    }
}