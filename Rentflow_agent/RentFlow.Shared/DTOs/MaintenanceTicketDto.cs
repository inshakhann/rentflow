using System;

namespace RentFlow.Shared.DTOs
{
    public class MaintenanceTicketDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int UnitId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public int Urgency { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
