using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class MaintenanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaintenanceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("landlord")]
        public async Task<IActionResult> GetLandlordTickets()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var tickets = await _context.MaintenanceTickets
                .Include(t => t.Property)
                .Include(t => t.Tenant)
                .Include(t => t.Unit)
                .Where(t => t.Property.LandlordId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.Category,
                    t.Description,
                    t.Status,
                    t.Urgency,
                    t.CreatedAt,
                    PropertyName = t.Property.Name,
                    TenantName = t.Tenant.FullName,
                    t.AssignedTo,
                    t.PhotoPath,
                    UnitNumber = t.Unit.UnitNumber
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet("tenant")]
        public async Task<IActionResult> GetTenantTickets()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var tickets = await _context.MaintenanceTickets
                .Where(t => t.TenantId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.Category,
                    t.Description,
                    t.Status,
                    t.Urgency,
                    t.PhotoPath,
                    t.CreatedAt
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] MaintenanceTicketDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Get tenant's active lease to find property and unit
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.TenantId == userId && l.IsActive);
                
            if (lease == null) return BadRequest("No active lease found.");

            var ticket = new MaintenanceTicket
            {
                TenantId = userId,
                PropertyId = lease.Unit.PropertyId,
                UnitId = lease.UnitId,
                Category = dto.Category,
                Description = dto.Description,
                Urgency = dto.Urgency,
                Status = "Open",
                PhotoPath = dto.PhotoPath,
                BotTranscript = "{}" // Just a placeholder
            };

            _context.MaintenanceTickets.Add(ticket);
            await _context.SaveChangesAsync();

            var landlordId = await _context.Properties
                .Where(p => p.Id == ticket.PropertyId)
                .Select(p => p.LandlordId)
                .FirstOrDefaultAsync();

            if (landlordId > 0)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = landlordId,
                    Title = "New Maintenance Ticket",
                    Message = $"A new {ticket.Category} ticket was submitted for unit #{lease.Unit.UnitNumber}.",
                    Type = "Ticket"
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { ticket.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] MaintenanceTicketDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ticket = await _context.MaintenanceTickets
                .Include(t => t.Property)
                .FirstOrDefaultAsync(t => t.Id == id && t.Property.LandlordId == userId);

            if (ticket == null) return NotFound();

            ticket.Status = dto.Status;
            ticket.AssignedTo = dto.AssignedTo;
            ticket.UpdatedAt = System.DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            const long maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxFileSizeBytes)
                return BadRequest("File is too large. Max allowed size is 5 MB.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Unsupported file type. Allowed types: .jpg, .jpeg, .png, .webp");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{fileName}";
            return Ok(new { photoPath = relativePath });
        }
    }
}
