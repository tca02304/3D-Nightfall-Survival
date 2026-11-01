using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameBackend.Models
{
    public class ToolStat
    {
        [Key]
        [ForeignKey("Item")]
        public int ItemId { get; set; }

        public string ToolType { get; set; } = null!;
        public int ToolPower { get; set; }

        [JsonIgnore]
        public Items Item { get; set; } = null!;
    }
}
