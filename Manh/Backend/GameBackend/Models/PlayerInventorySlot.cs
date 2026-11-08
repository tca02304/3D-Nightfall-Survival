namespace GameBackend.Models
{
    public class PlayerInventorySlot
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public Player player { get; set; } = null!;
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public Items Item { get; set; } = null!;
        public int Amount { get; set; }
    }
}
