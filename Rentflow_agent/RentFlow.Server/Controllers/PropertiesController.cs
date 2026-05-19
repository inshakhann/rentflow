using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;
using RentFlow.Shared.DTOs;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PropertiesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProperties()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role != "Landlord")
                return Forbid();

            var properties = await _context.Properties
                .Where(p => p.LandlordId == userId)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Address,
                    p.City,
                    p.TotalUnits,
                    OccupancyPercent = p.TotalUnits > 0 ? (double)p.Units.Count(u => u.IsOccupied) / p.TotalUnits * 100 : 0
                })
                .ToListAsync();

            return Ok(properties);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProperty(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var property = await _context.Properties
                .Include(p => p.Units)
                .ThenInclude(u => u.Leases.Where(l => l.IsActive))
                .ThenInclude(l => l.Tenant)
                .FirstOrDefaultAsync(p => p.Id == id && p.LandlordId == userId);

            if (property == null)
                return NotFound();

            var result = new
            {
                property.Id,
                property.Name,
                property.Address,
                property.City,
                property.Latitude,
                property.Longitude,
                Units = property.Units.Select(u => new
                {
                    u.Id,
                    u.UnitNumber,
                    u.MonthlyRent,
                    u.Bedrooms,
                    u.IsOccupied,
                    TenantName = u.IsOccupied && u.Leases.Any() ? u.Leases.First().Tenant.FullName : null
                })
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProperty([FromBody] PropertyDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var property = new Property
            {
                LandlordId = userId,
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                TotalUnits = dto.TotalUnits,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Auto-create units based on TotalUnits
            for (int i = 1; i <= property.TotalUnits; i++)
            {
                _context.Units.Add(new Unit
                {
                    PropertyId = property.Id,
                    UnitNumber = $"U-{i}",
                    MonthlyRent = 50000 // default
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new { property.Id });
        }

        [HttpPost("{propertyId}/units")]
        public async Task<IActionResult> AddUnit(int propertyId, [FromBody] UnitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.LandlordId == userId);
            if (property == null) return NotFound();

            var unit = new Unit
            {
                PropertyId = propertyId,
                UnitNumber = dto.UnitNumber,
                MonthlyRent = dto.MonthlyRent,
                Bedrooms = dto.Bedrooms,
                IsOccupied = false
            };

            property.TotalUnits++;
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();

            return Ok(new { unit.Id });
        }

        [HttpPut("units/{unitId}")]
        public async Task<IActionResult> EditUnit(int unitId, [FromBody] UnitDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var unit = await _context.Units
                .Include(u => u.Property)
                .FirstOrDefaultAsync(u => u.Id == unitId && u.Property.LandlordId == userId);

            if (unit == null) return NotFound();

            unit.UnitNumber = dto.UnitNumber;
            unit.MonthlyRent = dto.MonthlyRent;
            unit.Bedrooms = dto.Bedrooms;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
