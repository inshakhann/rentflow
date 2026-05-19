using System;
using System.Collections.Generic;

namespace RentFlow.Shared.Models
{
    public class Property
    {
        public int Id { get; set; }
        public int LandlordId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int TotalUnits { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User Landlord { get; set; } = null!;
        public List<Unit> Units { get; set; } = new();
        public List<MaintenanceTicket> MaintenanceTickets { get; set; } = new();
        public List<WeatherAlertLog> WeatherAlertLogs { get; set; } = new();
    }
}
