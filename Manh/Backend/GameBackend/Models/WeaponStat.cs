using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameBackend.Models
{
    public class WeaponStat
    {
        [Key]
        [ForeignKey("Item")]
        public int ItemId { get; set; }

        public int Damage { get; set; }
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public int Durability { get; set; }

        [JsonIgnore]
        public Items Item { get; set; } = null!;
    }
}
