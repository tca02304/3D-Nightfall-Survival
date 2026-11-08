namespace GameBackend.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public ICollection<PlayerInventorySlot> InventorySlots
        { get; set; } = new List<PlayerInventorySlot>();
    }
}
