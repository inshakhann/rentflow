namespace RentFlow.Shared.DTOs
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // system, user, assistant
        public string Content { get; set; } = string.Empty;
    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
