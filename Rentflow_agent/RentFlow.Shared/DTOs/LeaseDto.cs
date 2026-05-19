using System;

namespace RentFlow.Shared.DTOs
{
    public class LeaseDto
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public int TenantId { get; set; }
        public string TenantEmail { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public bool IsActive { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class CreateLeaseDto
    {
        public int UnitId { get; set; }
        public string TenantEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
    }
}
