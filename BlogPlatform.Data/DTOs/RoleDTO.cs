using System;

namespace BlogPlatform.Data.DTOs
{
    public record RoleDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public record AssignRoleDTO
    {
        public int UserId { get; init; }
        public int RoleId { get; init; }
    }
}