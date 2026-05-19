using System;

namespace RentFlow.Shared.Models
{
    public class MaintenanceTicket
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int PropertyId { get; set; }
        public int UnitId { get; set; }
        public string Category { get; set; } = string.Empty; // Plumbing|Electrical|HVAC|Structural|Other
        public string Description { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string Status { get; set; } = "Open"; // Open | InProgress | Resolved
        public string? AssignedTo { get; set; }
        public string? BotTranscript { get; set; }
        public int Urgency { get; set; } = 1; // 1=Minor, 2=Moderate, 3=Emergency
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User Tenant { get; set; } = null!;
        public Property Property { get; set; } = null!;
        public Unit Unit { get; set; } = null!;
    }
}
