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
    public class TagService(AppDbContext context) : ITagService
    {
        public async Task<TagDTO?> GetTagByIdAsync(int id)
        {
            var tag = await context.Tags.FindAsync(id);

            if (tag == null) return null;

            return new TagDTO
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description ?? string.Empty,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<List<TagDTO>> GetAllTagsAsync()
        {
            var tags = await context.Tags.ToListAsync();

            return tags.Select(tag => new TagDTO
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description ?? string.Empty,
                CreatedAt = tag.CreatedAt
            }).ToList();
        }

        public async Task<TagDTO> CreateTagAsync(CreateTagDTO tagDto)
        {
            // Проверка на уникальность имени
            if (await context.Tags.AnyAsync(t => t.Name == tagDto.Name))
                throw new ArgumentException("Tag name already exists");

            var tag = new Tag
            {
                Name = tagDto.Name,
                Slug = GenerateSlug(tagDto.Name),
                Description = tagDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            context.Tags.Add(tag);
            await context.SaveChangesAsync();

            return new TagDTO
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description ?? string.Empty,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<TagDTO?> UpdateTagAsync(int id, UpdateTagDTO tagDto)
        {
            var tag = await context.Tags.FindAsync(id);
            if (tag == null) return null;

            if (!string.IsNullOrEmpty(tagDto.Name))
            {
                // Проверка на уникальность нового имени
                if (await context.Tags.AnyAsync(t => t.Name == tagDto.Name && t.Id != id))
                    throw new ArgumentException("Tag name already exists");

                tag.Name = tagDto.Name;
                tag.Slug = GenerateSlug(tagDto.Name);
            }

            if (tagDto.Description != null)
                tag.Description = tagDto.Description;

            await context.SaveChangesAsync();

            return new TagDTO
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description ?? string.Empty,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = await context.Tags.FindAsync(id);
            if (tag == null) return false;

            context.Tags.Remove(tag);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TagExistsAsync(int id)
        {
            return await context.Tags.AnyAsync(t => t.Id == id);
        }

        private static string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "");
        }
    }
}