using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            try
            {
                var dbInfo = new
                {
                    Status = "API is running",
                    Timestamp = DateTime.Now,
                    Database = new
                    {
                        CanConnect = await _context.Database.CanConnectAsync(),
                        Provider = _context.Database.ProviderName,
                        UsersCount = await _context.Users.CountAsync()
                    }
                };

                return Ok(dbInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    ex.Message
                });
            }
        }
    }
}