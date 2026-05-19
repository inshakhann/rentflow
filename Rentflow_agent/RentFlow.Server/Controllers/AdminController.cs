using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;

namespace RentFlow.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalLandlords = await _context.Users.CountAsync(u => u.Role == "Landlord");
            var totalTenants = await _context.Users.CountAsync(u => u.Role == "Tenant");
            var activeLeases = await _context.Leases.CountAsync(l => l.IsActive);
            var openTickets = await _context.MaintenanceTickets.CountAsync(t => t.Status == "Open");

            var ticketsByCategory = await _context.MaintenanceTickets
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var recentActivity = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(u => new { Event = "User Joined", User = u.FullName, Time = u.CreatedAt })
                .ToListAsync();

            return Ok(new
            {
                TotalLandlords = totalLandlords,
                TotalTenants = totalTenants,
                ActiveLeases = activeLeases,
                OpenTickets = openTickets,
                TicketsByCategory = ticketsByCategory,
                RecentActivity = recentActivity
            });
        }

        [HttpGet("landlords")]
        public async Task<IActionResult> GetLandlords()
        {
            var landlords = await _context.Users
                .Where(u => u.Role == "Landlord")
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    PropertiesCount = u.Properties.Count
                })
                .ToListAsync();

            return Ok(landlords);
        }

        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenants()
        {
            var tenants = await _context.Users
                .Where(u => u.Role == "Tenant")
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    Unit = u.Leases.Any(l => l.IsActive) ? u.Leases.First(l => l.IsActive).Unit.UnitNumber : "None",
                    Landlord = u.Leases.Any(l => l.IsActive) ? u.Leases.First(l => l.IsActive).Unit.Property.Landlord.FullName : "None",
                    PaymentStatus = u.Leases.Any(l => l.IsActive) 
                        ? (u.Leases.First(l => l.IsActive).Payments.OrderByDescending(p => p.DueDate).FirstOrDefault() != null 
                            ? u.Leases.First(l => l.IsActive).Payments.OrderByDescending(p => p.DueDate).First().Status 
                            : "No Payments")
                        : "No Lease"
                })
                .ToListAsync();

            return Ok(tenants);
        }

        [HttpGet("properties")]
        public async Task<IActionResult> GetProperties()
        {
            var properties = await _context.Properties
                .Include(p => p.Landlord)
                .Include(p => p.Units)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Address,
                    p.City,
                    LandlordName = p.Landlord.FullName,
                    p.TotalUnits,
                    OccupancyPercent = p.TotalUnits > 0 ? (double)p.Units.Count(u => u.IsOccupied) / p.TotalUnits * 100 : 0
                })
                .ToListAsync();

            return Ok(properties);
        }
    }
}
