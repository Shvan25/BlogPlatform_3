using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpPost("database")]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                return Ok(new { message = "Database seeded successfully with initial data" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearDatabase()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Очищаем все таблицы (в правильном порядке из-за внешних ключей)
                    context.Comments.RemoveRange(context.Comments);
                    context.ArticleTags.RemoveRange(context.ArticleTags);
                    context.Articles.RemoveRange(context.Articles);
                    context.Tags.RemoveRange(context.Tags);
                    context.Users.RemoveRange(context.Users);

                    await context.SaveChangesAsync();

                    return Ok(new { message = "Database cleared successfully" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Удаляем базу
                    await context.Database.EnsureDeletedAsync();
                    Console.WriteLine("Database deleted");

                    // Создаем заново
                    await context.Database.EnsureCreatedAsync();
                    Console.WriteLine("Database created");

                    return Ok(new { message = "Database reset and seeded successfully" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var stats = new
                {
                    Users = await context.Users.CountAsync(),
                    Articles = await context.Articles.CountAsync(),
                    PublishedArticles = await context.Articles.CountAsync(a => a.IsPublished),
                    DraftArticles = await context.Articles.CountAsync(a => !a.IsPublished),
                    Tags = await context.Tags.CountAsync(),
                    Comments = await context.Comments.CountAsync(),
                    ApprovedComments = await context.Comments.CountAsync(c => c.IsApproved),
                    ArticleTags = await context.ArticleTags.CountAsync()
                };

                return Ok(stats);
            }
        }
    }
}