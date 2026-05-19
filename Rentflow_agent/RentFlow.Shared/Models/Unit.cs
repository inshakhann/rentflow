using System;
using System.Collections.Generic;

namespace RentFlow.Shared.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public int Bedrooms { get; set; } = 1;
        public bool IsOccupied { get; set; } = false;

        // Navigation properties
        public Property Property { get; set; } = null!;
        public List<Lease> Leases { get; set; } = new();
    }
}
