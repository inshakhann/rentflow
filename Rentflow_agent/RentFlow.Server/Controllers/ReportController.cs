using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("monthly")]
        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> GetMonthlyReport([FromQuery] int month, [FromQuery] int year)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var propertyIds = await _context.Properties
                .Where(p => p.LandlordId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var unitIds = await _context.Units
                .Where(u => propertyIds.Contains(u.PropertyId))
                .Select(u => u.Id)
                .ToListAsync();

            var leaseIds = await _context.Leases
                .Where(l => unitIds.Contains(l.UnitId))
                .Select(l => l.Id)
                .ToListAsync();

            // Payments in this month
            var payments = await _context.Payments
                .Where(p => leaseIds.Contains(p.LeaseId)
                    && p.DueDate.Month == month
                    && p.DueDate.Year == year)
                .ToListAsync();

            var totalIncome = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount);
            var totalLateFees = payments.Sum(p => p.LateFee);
            var paidCount = payments.Count(p => p.Status == "Paid");
            var pendingCount = payments.Count(p => p.Status == "Pending");
            var lateCount = payments.Count(p => p.Status == "Late");

            // Open tickets
            var openTickets = await _context.MaintenanceTickets
                .CountAsync(t => unitIds.Contains(t.UnitId)
                    && t.CreatedAt.Month == month
                    && t.CreatedAt.Year == year
                    && t.Status != "Resolved");

            // Occupancy rate
            var totalUnits = unitIds.Count;
            var occupiedUnits = await _context.Leases
                .CountAsync(l => unitIds.Contains(l.UnitId)
                    && l.StartDate <= new DateTime(year, month, DateTime.DaysInMonth(year, month))
                    && (!l.EndDate.HasValue || l.EndDate.Value >= new DateTime(year, month, 1)));

            var occupancyRate = totalUnits > 0 ? Math.Round((double)occupiedUnits / totalUnits * 100) : 0;

            return Ok(new
            {
                Month = month,
                Year = year,
                TotalIncome = totalIncome,
                TotalLateFees = totalLateFees,
                PaidPayments = paidCount,
                PendingPayments = pendingCount,
                LatePayments = lateCount,
                OpenTickets = openTickets,
                OccupancyRate = occupancyRate,
                TotalUnits = totalUnits,
                OccupiedUnits = occupiedUnits
            });
        }
    }
}
