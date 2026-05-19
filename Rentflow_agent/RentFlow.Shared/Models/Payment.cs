using System;

namespace RentFlow.Shared.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int LeaseId { get; set; }
        public int TenantId { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; } = 0;
        public string Status { get; set; } = "Pending"; // Pending | Paid | Late

        // Navigation properties
        public Lease Lease { get; set; } = null!;
        public User Tenant { get; set; } = null!;
    }
}
