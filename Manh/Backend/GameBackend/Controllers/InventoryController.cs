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
        private const int TotalInventorySlots = 39;
        private const int TotalCraftingSlots = 9;

        private readonly GameDbContext _context;

        public InventoryController(GameDbContext context)
        {
            _context = context;
        }

        [HttpGet("{playerId:int}")]
        public async Task<IActionResult> GetInventory(int playerId)
        {
            if (playerId <= 0)
                return BadRequest("Invalid player ID");

            bool playerExists = await _context.Players
                .AnyAsync(p => p.Id == playerId);

            if (!playerExists)
                return NotFound(new
                {
                    success = false,
                    message = "Player not found"
                });

            var inventory = await _context.PlayerInventorySlots
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

            var crafting = await _context.PlayerCraftingSlots
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

            var cursor = await _context.PlayerCursorSlots
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .Select(s => new InventorySlotResponse
                {
                    SlotIndex = 0,
                    ItemId = s.ItemId,
                    Amount = s.Amount
                })
                .SingleOrDefaultAsync();

            return Ok(new
            {
                success = true,
                message = "Inventory snapshot loaded successfully",
                data = inventory,
                craftingSlots = crafting,
                cursorSlot = cursor
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

            if (playerId <= 0)
                return BadRequest("Invalid player ID");

            if (request == null ||
                request.Slots == null ||
                request.CraftingSlots == null)
            {
                return BadRequest(
                    "Full snapshot required: slots and craftingSlots");
            }

            bool playerExists = await _context.Players
                .AnyAsync(p => p.Id == playerId);

            if (!playerExists)
                return NotFound("Player not found");

            if (!AreSlotsValid(request.Slots, TotalInventorySlots))
                return BadRequest("Invalid inventory slots");

            if (!AreSlotsValid(request.CraftingSlots, TotalCraftingSlots))
                return BadRequest("Invalid crafting slots");

            if (request.CursorSlot != null &&
                (request.CursorSlot.SlotIndex != 0 ||
                 request.CursorSlot.ItemId <= 0 ||
                 request.CursorSlot.Amount <= 0))
            {
                return BadRequest("Invalid cursor slot");
            }

            var allRecords = request.Slots
                .Concat(request.CraftingSlots)
                .ToList();

            if (request.CursorSlot != null)
                allRecords.Add(request.CursorSlot);

            var itemIds = allRecords
                .Select(s => s.ItemId)
                .Distinct()
                .ToList();

            var items = await _context.Items
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            foreach (var record in allRecords)
            {
                if (!items.TryGetValue(record.ItemId, out var item) ||
                    record.Amount > item.MaxStack)
                {
                    return BadRequest(
                        $"Invalid item or stack: ItemId={record.ItemId}");
                }
            }

            // Inventory, Crafting and Cursor are committed together.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existingInventory =
                await _context.PlayerInventorySlots
                    .Where(s => s.PlayerId == playerId)
                    .ToListAsync();

            var existingCrafting =
                await _context.PlayerCraftingSlots
                    .Where(s => s.PlayerId == playerId)
                    .ToListAsync();

            var existingCursor =
                await _context.PlayerCursorSlots
                    .SingleOrDefaultAsync(s => s.PlayerId == playerId);

            _context.PlayerInventorySlots
                .RemoveRange(existingInventory);

            _context.PlayerCraftingSlots
                .RemoveRange(existingCrafting);

            if (existingCursor != null)
                _context.PlayerCursorSlots.Remove(existingCursor);

            await _context.SaveChangesAsync();

            foreach (var slot in request.Slots)
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

            foreach (var slot in request.CraftingSlots)
            {
                _context.PlayerCraftingSlots.Add(
                    new PlayerCraftingSlot
                    {
                        PlayerId = playerId,
                        SlotIndex = slot.SlotIndex,
                        ItemId = slot.ItemId,
                        Amount = slot.Amount
                    });
            }

            if (request.CursorSlot != null)
            {
                _context.PlayerCursorSlots.Add(
                    new PlayerCursorSlot
                    {
                        PlayerId = playerId,
                        ItemId = request.CursorSlot.ItemId,
                        Amount = request.CursorSlot.Amount
                    });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Full inventory snapshot saved",
                data = new
                {
                    inventoryCount = request.Slots.Count,
                    craftingCount = request.CraftingSlots.Count,
                    cursorOccupied = request.CursorSlot != null
                }
            });
        }

        private static bool AreSlotsValid(
            IReadOnlyList<SaveInventorySlot> slots,
            int maxSlots)
        {
            var used = new HashSet<int>();

            foreach (var slot in slots)
            {
                if (slot == null ||
                    slot.SlotIndex < 0 ||
                    slot.SlotIndex >= maxSlots ||
                    slot.ItemId <= 0 ||
                    slot.Amount <= 0 ||
                    !used.Add(slot.SlotIndex))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class InventorySlotResponse
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }
    }
}
