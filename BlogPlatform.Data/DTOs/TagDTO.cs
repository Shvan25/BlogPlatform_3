using System;
using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Data.DTOs
{
    public class TagDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Количество статей с этим тегом
        public int ArticleCount { get; set; }
    }

    public class CreateTagDTO
    {
        [Required(ErrorMessage = "Название тега обязательно")]
        [StringLength(50, ErrorMessage = "Название тега не должно превышать 50 символов")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Описание не должно превышать 200 символов")]
        public string Description { get; set; }
    }

    public class UpdateTagDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}