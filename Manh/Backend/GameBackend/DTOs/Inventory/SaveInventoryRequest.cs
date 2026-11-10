
using System.Collections.Generic;

namespace GameBackend.DTOs.Inventory
{
    public class SaveInventoryRequest
    {
        public List<SaveInventorySlot> Slots { get; set; } = new();

        public List<SaveInventorySlot>? CraftingSlots { get; set; }

        public SaveInventorySlot? CursorSlot { get; set; }
    }

    public class SaveInventorySlot
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }
    }
}
