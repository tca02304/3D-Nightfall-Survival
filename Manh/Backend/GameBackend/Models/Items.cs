namespace GameBackend.Models
{
    public class Items
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Type { get; set; } = null!;
        public int MaxStack { get; set; }
        public int Price { get; set; }
        public bool IsActive { get; set; }
    }
}
