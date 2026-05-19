using System;
using System.Collections.Generic;

namespace RentFlow.Shared.DTOs
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int TotalUnits { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class UnitDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public int Bedrooms { get; set; }
        public bool IsOccupied { get; set; }
    }
}
