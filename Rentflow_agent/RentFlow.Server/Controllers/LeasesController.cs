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
    public class LeasesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LeasesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("landlord")]
        public async Task<IActionResult> GetLandlordLeases()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role != "Landlord")
                return Forbid();

            var leases = await _context.Leases
                .Include(l => l.Tenant)
                .Include(l => l.Unit)
                .ThenInclude(u => u.Property)
                .Where(l => l.Unit.Property.LandlordId == userId && l.IsActive)
                .Select(l => new LeaseDto
                {
                    Id = l.Id,
                    UnitId = l.UnitId,
                    TenantId = l.TenantId,
                    TenantEmail = l.Tenant.Email,
                    TenantName = l.Tenant.FullName,
                    PropertyName = l.Unit.Property.Name,
                    UnitNumber = l.Unit.UnitNumber,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    MonthlyRent = l.MonthlyRent,
                    IsActive = l.IsActive,
                    PaymentStatus = l.Payments.OrderByDescending(p => p.DueDate).FirstOrDefault() != null 
                        ? l.Payments.OrderByDescending(p => p.DueDate).First().Status 
                        : "No Payments"
                })
                .ToListAsync();

            return Ok(leases);
        }

        [HttpGet("available-tenants")]
        public async Task<IActionResult> GetAvailableTenants()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "Landlord")
                return Forbid();

            // Fetch tenants who don't have an active lease
            var activeTenantIds = await _context.Leases
                .Where(l => l.IsActive)
                .Select(l => l.TenantId)
                .ToListAsync();

            var availableTenants = await _context.Users
                .Where(u => u.Role == "Tenant" && !activeTenantIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToListAsync();

            return Ok(availableTenants);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLease([FromBody] CreateLeaseDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role != "Landlord")
                return Forbid();

            // Find tenant by email
            var tenant = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.TenantEmail.ToLower() && u.Role == "Tenant");

            if (tenant == null)
            {
                return BadRequest("Tenant not found with the provided email address.");
            }

            // Check if tenant already has an active lease
            var hasActiveLease = await _context.Leases.AnyAsync(l => l.TenantId == tenant.Id && l.IsActive);
            if (hasActiveLease)
            {
                return BadRequest("This tenant already has an active lease.");
            }

            // Find unit and make sure landlord owns it
            var unit = await _context.Units
                .Include(u => u.Property)
                .FirstOrDefaultAsync(u => u.Id == dto.UnitId && u.Property.LandlordId == userId);

            if (unit == null)
            {
                return NotFound("Unit not found or you do not have permission to manage this unit.");
            }

            if (unit.IsOccupied)
            {
                return BadRequest("This unit is already occupied.");
            }

            // Create lease
            var lease = new Lease
            {
                UnitId = dto.UnitId,
                TenantId = tenant.Id,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MonthlyRent = dto.MonthlyRent,
                IsActive = true
            };

            // Mark unit occupied
            unit.IsOccupied = true;

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();

            // Create first payment for current month
            var payment = new Payment
            {
                LeaseId = lease.Id,
                TenantId = tenant.Id,
                DueDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                Amount = dto.MonthlyRent,
                Status = "Pending"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { lease.Id });
        }
    }
}
