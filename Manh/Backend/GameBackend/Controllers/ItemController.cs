using GameBackend.Data;
using GameBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly GameDbContext _context;

        public ItemController(GameDbContext context)
        {
            _context = context;
        }

        [HttpGet("getallitem")]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.Items.ToListAsync();

            var re = new ResponseModel<List<Item>>
            {
                Success = true,
                Message = "Get all items successfully",
                Data = items
            };

            return Ok(re);
        }
    }
}
