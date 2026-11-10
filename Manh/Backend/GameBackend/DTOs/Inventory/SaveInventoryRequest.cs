namespace GameBackend.DTOs.Inventory
{
    public class SaveInventoryRequest
    {
        public List<SaveInventorySlotDto> Slots { get; set; } = new();
    }

    public class SaveInventorySlotDto
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }
    }
}
