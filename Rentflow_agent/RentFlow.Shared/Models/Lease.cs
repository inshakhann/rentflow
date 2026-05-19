using System;
using System.Collections.Generic;

namespace RentFlow.Shared.Models
{
    public class Lease
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public int TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Unit Unit { get; set; } = null!;
        public User Tenant { get; set; } = null!;
        public List<Payment> Payments { get; set; } = new();
    }
}
