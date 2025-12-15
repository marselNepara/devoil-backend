namespace DevOIlApi.DTOs
{
    public class GetBidRequest
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
