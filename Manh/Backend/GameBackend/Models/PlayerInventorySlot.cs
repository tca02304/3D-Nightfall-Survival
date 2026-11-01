namespace GameBackend.Models
{
    public class PlayerInventorySlot
    {
        public int Id { get; set; }
        public int playerId { get; set; }
        public Player player { get; set; } = null!;
        public int SlotIndex { get; set; }
        public int itemId { get; set; }
        public Items Item { get; set; } = null!;
        public int Amount { get; set; }
    }
}
