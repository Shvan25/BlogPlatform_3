using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Interfaces;
using BlogPlatform.Data.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Controllers
{
    public class TagsController : Controller
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        // Страница отображения всех тегов - GET: /Tags
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return View(tags);
        }

        // Страница просмотра тега - GET: /Tags/Details/{id}
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
                return NotFound();

            return View(tag);
        }

        // Страница добавления тега - GET: /Tags/Create
        [HttpGet("Create")]
        [Authorize(Roles = "Admin,Moderator")]
        public IActionResult Create()
        {
            return View();
        }

        // Страница редактирования тега - GET: /Tags/Edit/{id}
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
                return NotFound();

            return View(tag);
        }

        // Страница удаления тега - GET: /Tags/Delete/{id}
        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _tagService.GetTagByIdAsync(id);
            if (tag == null)
                return NotFound();

            return View(tag);
        }
    }
}