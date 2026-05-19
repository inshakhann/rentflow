using System;

namespace RentFlow.Shared.Models
{
    public class WeatherAlertLog
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string AlertType { get; set; } = string.Empty; // Freeze | Storm | Heatwave | Flood
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        // Navigation properties
        public Property Property { get; set; } = null!;
    }
}
