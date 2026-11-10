using GameBackend.Data;
using GameBackend.Models;
using GameBackend.DTOs.Inventory;
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
                .Where(s => s.PlayerId == playerId)
                .OrderBy(s => s.SlotIndex)
                .Select(s => new InventorySlotResponse
                {
                    SlotIndex = s.SlotIndex,
                    ItemId = s.ItemId,
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

        [HttpPost("{playerId:int}/save-test")]
        public async Task<IActionResult> SaveInventoryTest(
            int playerId,
            [FromBody] SaveInventoryRequest request,
            [FromServices] IWebHostEnvironment environment)
        {
            // Chỉ cho phép dùng trong Development
            if (!environment.IsDevelopment())
                return NotFound();

            if (request?.Slots == null)
                return BadRequest("Invalid request");

            if (playerId <= 0)
                return BadRequest("Invalid player ID");

            bool playerExists = await _context.Players
                .AnyAsync(p => p.Id == playerId);

            if (!playerExists)
                return NotFound("Player not found");

            // 32 inventory + 7 hot bar
            const int totalSlots = 39;

            if (request.Slots.Any(s =>
            s.SlotIndex < 0 ||
            s.SlotIndex >= totalSlots ||
            s.ItemId <= 0 ||
            s.Amount <= 0))
            {
                return BadRequest("Invalid inventory slot");
            }

            if (request.Slots
                .Select(s => s.SlotIndex)
                .Distinct()
                .Count() != request.Slots.Count)
            {
                return BadRequest("Duplicate slot index");
            }

            var itemIds = request.Slots
                .Select(s => s.ItemId)
                .Distinct()
                .ToList();

            var items = await _context.Items
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            foreach(var slot in request.Slots)
            {
                if (!items.TryGetValue(slot.ItemId, out var item)
                    || slot.Amount > item.MaxStack)
                {
                    return BadRequest(
                        $"Invalid item or stack at slot {slot.SlotIndex}");
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existingSlots = await _context.PlayerInventorySlots
                .Where(s => s.PlayerId == playerId)
                .ToListAsync();

            _context.PlayerInventorySlots.RemoveRange(existingSlots);

            await _context.SaveChangesAsync();

            foreach(var slot in request.Slots)
            {
                _context.PlayerInventorySlots.Add(
                    new PlayerInventorySlot
                    {
                        PlayerId = playerId,
                        SlotIndex = slot.SlotIndex,
                        ItemId = slot.ItemId,
                        Amount = slot.Amount
                    });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new ResponseModel<int>
            {
                Success = true,
                Message = "Inventory saved successfully",
                Data = request.Slots.Count
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
