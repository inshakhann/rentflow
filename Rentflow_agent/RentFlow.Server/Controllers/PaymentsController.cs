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
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("landlord/revenue")]
        public async Task<IActionResult> GetLandlordRevenue()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var payments = await _context.Payments
                .Include(p => p.Lease)
                .ThenInclude(l => l.Unit)
                .ThenInclude(u => u.Property)
                .Where(p => p.Lease.Unit.Property.LandlordId == userId && p.Status == "Paid")
                .ToListAsync();

            var revenueByMonth = payments
                .Where(p => p.PaidDate.HasValue)
                .GroupBy(p => new { p.PaidDate!.Value.Year, p.PaidDate.Value.Month })
                .Select(g => new
                {
                    Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Total = g.Sum(p => p.Amount + p.LateFee)
                })
                .OrderBy(x => x.Label)
                .ToList();

            return Ok(revenueByMonth);
        }

        [HttpGet("landlord/late")]
        public async Task<IActionResult> GetLatePayments()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var late = await _context.Payments
                .Include(p => p.Tenant)
                .Include(p => p.Lease)
                .ThenInclude(l => l.Unit)
                .Where(p => p.Lease.Unit.Property.LandlordId == userId && p.Status == "Late")
                .Select(p => new
                {
                    TenantName = p.Tenant.FullName,
                    UnitNumber = p.Lease.Unit.UnitNumber,
                    p.Amount,
                    p.LateFee,
                    DaysOverdue = (System.DateTime.UtcNow - p.DueDate).Days
                })
                .ToListAsync();

            return Ok(late);
        }
        
        [HttpGet("tenant/history")]
        public async Task<IActionResult> GetTenantHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var payments = await _context.Payments
                .Where(p => p.TenantId == userId)
                .OrderByDescending(p => p.DueDate)
                .Select(p => new
                {
                    p.Id,
                    p.DueDate,
                    p.PaidDate,
                    p.Amount,
                    p.LateFee,
                    p.Status
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> PayPayment(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var payment = await _context.Payments
                .Include(p => p.Lease)
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == userId);

            if (payment == null) return NotFound();

            if (payment.Status == "Paid") return BadRequest("Payment already completed.");

            payment.Status = "Paid";
            payment.PaidDate = System.DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("tenant/receipt/{id}")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var receipt = await _context.Payments
                .Include(p => p.Lease).ThenInclude(l => l.Unit).ThenInclude(u => u.Property)
                .Include(p => p.Tenant)
                .Where(p => p.Id == id && p.TenantId == userId && p.Status == "Paid")
                .Select(p => new
                {
                    p.Id,
                    TenantName = p.Tenant.FullName,
                    TenantEmail = p.Tenant.Email,
                    PropertyName = p.Lease.Unit.Property.Name,
                    UnitNumber = p.Lease.Unit.UnitNumber,
                    p.Amount,
                    p.LateFee,
                    Total = p.Amount + p.LateFee,
                    p.PaidDate,
                    p.DueDate
                })
                .FirstOrDefaultAsync();

            if (receipt == null) return NotFound();
            return Ok(receipt);
        }

        [HttpGet("tenant/due-status")]
        public async Task<IActionResult> GetDueStatus()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var nextPending = await _context.Payments
                .Where(p => p.TenantId == userId && (p.Status == "Pending" || p.Status == "Late"))
                .OrderBy(p => p.DueDate)
                .FirstOrDefaultAsync();

            if (nextPending == null)
                return Ok(new { isDueSoon = false, daysUntilDue = -1 });

            var daysUntil = (nextPending.DueDate - System.DateTime.UtcNow.Date).Days;
            return Ok(new { isDueSoon = daysUntil <= 3, daysUntilDue = daysUntil });
        }

        [HttpGet("score/{tenantId}")]
        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> GetPaymentScore(int tenantId)
        {
            var payments = await _context.Payments
                .Where(p => p.TenantId == tenantId && p.Status == "Paid")
                .ToListAsync();

            if (!payments.Any())
                return Ok(new { score = 100, label = "New" });

            // On-time = paid before or on due date
            var onTime = payments.Count(p => p.PaidDate.HasValue && p.PaidDate.Value.Date <= p.DueDate.Date);
            var score = (int)System.Math.Round((double)onTime / payments.Count * 100);

            return Ok(new { score, label = score >= 90 ? "Excellent" : score >= 70 ? "Good" : score >= 50 ? "Fair" : "Poor" });
        }
    }
}
