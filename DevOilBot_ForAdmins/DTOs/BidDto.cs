using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOilBot_ForAdmins.DTOs
{
    public class BidDto
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsProcessedByAdmin { get; set; }
        public string ClientFirstName { get; set; } = string.Empty;
        public string ClientLastName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public DateTime Date_of_Bid { get; set; }
    }
}
