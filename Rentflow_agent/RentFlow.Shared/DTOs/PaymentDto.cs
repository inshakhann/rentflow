using System;

namespace RentFlow.Shared.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int LeaseId { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
