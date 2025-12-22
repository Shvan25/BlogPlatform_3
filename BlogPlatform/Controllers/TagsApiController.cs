using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsApiController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsApiController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagDTO>>> GetAllTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TagDTO>> GetTagById(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
            {
                return NotFound(new { message = $"Tag with ID {id} not found" });
            }
            return Ok(tag);
        }
    }
}