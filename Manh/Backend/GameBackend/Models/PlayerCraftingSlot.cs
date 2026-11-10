
namespace GameBackend.Models
{
    public class PlayerCraftingSlot
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }

        public Player Player { get; set; } = null!;
        public Items Item { get; set; } = null!;
    }
}