using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevOIlApi.Models
{
    public class Bid
    {
        [Key]
        public int Id { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public bool IsProcessedByAdmin { get; set; } = false;
        public int Id_Client { get; set; }
        public DateTime Date_of_bid { get; set; } = DateTime.UtcNow;

        [ForeignKey("Id_Client")]
        public virtual Client Client { get; set; } = null!;
    }
}
