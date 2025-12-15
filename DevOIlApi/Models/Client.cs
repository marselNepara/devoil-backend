using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevOIlApi.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        public string First_Name { get; set; } = string.Empty;

        [Required]
        public string Last_Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone_Number { get; set; } = string.Empty;

        public DateTime Date_of_registration { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Bid> ClientBids { get; set; } = new List<Bid>();
    }
}
