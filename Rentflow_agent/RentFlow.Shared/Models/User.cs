using System;
using System.Collections.Generic;

namespace RentFlow.Shared.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin | Landlord | Tenant
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<Property> Properties { get; set; } = new();
        public List<Lease> Leases { get; set; } = new();
        public List<MaintenanceTicket> MaintenanceTickets { get; set; } = new();
    }
}
