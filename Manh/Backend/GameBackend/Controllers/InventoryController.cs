using GameBackend.Data;
using GameBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly GameDbContext _context;

        public InventoryController(GameDbContext context)
        {
            _context = context;
        }

        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetInventory(int playerId)
        {
            var playerExists = await _context.Players
                .AnyAsync(p => p.Id == playerId);

            if (!playerExists)
            {
                return NotFound(
                    new ResponseModel<List<InventorySlotResponse>>
                    {
                        Success = false,
                        Message = "Player not found",
                        Data = null
                    });
            }

            List<InventorySlotResponse> inventory =
                await _context.PlayerInventorySlots
                .AsNoTracking()
                .Where(s => s.playerId == playerId)
                .OrderBy(s => s.SlotIndex)
                .Select(s => new InventorySlotResponse
                {
                    SlotIndex = s.SlotIndex,
                    ItemId = s.itemId,
                    Amount = s.Amount
                })
                .ToListAsync();

            return Ok(
                new ResponseModel<List<InventorySlotResponse>>
                {
                    Success = true,
                    Message = "Get inventory successfully",
                    Data = inventory
                });
        }
    }

    public class InventorySlotResponse
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }
    }
}
