using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameBackend.Models
{
    public class ConsumableStat
    {
        [Key]
        [ForeignKey(nameof(Item))]
        public int ItemId { get; set; }
        public int HealHP { get; set; }
        public int RestoreStamina { get; set; }

        [JsonIgnore]
        public Items Item { get; set; } = null!;
    }
}
