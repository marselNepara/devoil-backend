namespace DevOIlApi.DTOs
{
    public class CreateBidRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime Date_of_Bid { get; set; } = DateTime.UtcNow;
        public bool ClientAgreedToPrivacyPolicy { get; set; }
    }
}
