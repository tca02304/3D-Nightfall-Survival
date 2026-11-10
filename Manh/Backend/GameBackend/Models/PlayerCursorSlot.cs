
namespace GameBackend.Models
{
    public class PlayerCursorSlot
    {
        // Moi Player chi co mot Cursor.
        public int PlayerId { get; set; }
        public int ItemId { get; set; }
        public int Amount { get; set; }

        public Player Player { get; set; } = null!;
        public Items Item { get; set; } = null!;
    }
}