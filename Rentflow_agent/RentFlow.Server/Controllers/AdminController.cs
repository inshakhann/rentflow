using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

        [HttpGet("landlords/{id}")]
        public async Task<IActionResult> GetLandlordDetails(int id)
        {
            var landlord = await _context.Users
                .Where(u => u.Role == "Landlord" && u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    u.CreatedAt,
                    PropertiesCount = u.Properties.Count,
                    ActiveUnits = u.Properties.SelectMany(p => p.Units).Count(),
                    OccupiedUnits = u.Properties.SelectMany(p => p.Units).Count(unit => unit.IsOccupied)
                })
                .FirstOrDefaultAsync();

            if (landlord == null)
                return NotFound();

            return Ok(landlord);
        }

        [HttpPut("landlords/{id}/status")]
        public async Task<IActionResult> UpdateLandlordStatus(int id, [FromQuery] bool active)
        {
            var landlord = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Landlord");
            if (landlord == null)
                return NotFound();

            landlord.IsActive = active;
            await _context.SaveChangesAsync();

            return Ok(new { landlord.Id, landlord.IsActive });
        }

        [HttpGet("tenants/{id}")]
        public async Task<IActionResult> GetTenantDetails(int id)
        {
            var tenant = await _context.Users
                .Where(u => u.Role == "Tenant" && u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.IsActive,
                    u.CreatedAt,
                    ActiveLease = u.Leases
                        .Where(l => l.IsActive)
                        .Select(l => new
                        {
                            l.StartDate,
                            l.EndDate,
                            l.MonthlyRent,
                            UnitNumber = l.Unit.UnitNumber,
                            PropertyName = l.Unit.Property.Name,
                            LandlordName = l.Unit.Property.Landlord.FullName
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }

        [HttpPost("weather/trigger-check")]
        public async Task<IActionResult> TriggerWeatherCheck()
        {
            var apiKey = _configuration["OpenWeatherMap:ApiKey"];
            var properties = await _context.Properties
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                .ToListAsync();

            if (!properties.Any())
            {
                return Ok(new { Added = 0, Checked = 0, Message = "No properties with coordinates found." });
            }

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_", System.StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { Added = 0, Checked = properties.Count, Message = "Weather API key is not configured. Dry run completed." });
            }

            var client = _httpClientFactory.CreateClient();
            var now = System.DateTime.UtcNow;
            var addedCount = 0;
            var checkedCount = 0;

            foreach (var property in properties)
            {
                checkedCount++;
                var lat = property.Latitude!.Value;
                var lon = property.Longitude!.Value;
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

                WeatherSnapshot? weather;
                try
                {
                    weather = await client.GetFromJsonAsync<WeatherSnapshot>(url);
                }
                catch
                {
                    continue;
                }

                if (weather == null || weather.Main == null)
                    continue;

                var conditionText = weather.Weather?.FirstOrDefault()?.Main?.ToLowerInvariant() ?? string.Empty;
                var alertsToAdd = new System.Collections.Generic.List<(string Type, string Message)>();

                if (weather.Main.Temp <= 3)
                {
                    alertsToAdd.Add(("Freeze", "Freezing temperatures detected. Protect exposed pipes and check heating systems."));
                }

                if (weather.Main.Temp >= 40)
                {
                    alertsToAdd.Add(("Heatwave", "Extreme heat warning. Inspect cooling systems and hydration points."));
                }

                if (weather.Wind?.Speed >= 13 || conditionText.Contains("storm") || conditionText.Contains("thunder"))
                {
                    alertsToAdd.Add(("Storm", "Storm risk detected. Secure loose outdoor objects and inspect roof drainage."));
                }

                foreach (var alert in alertsToAdd)
                {
                    var duplicateExists = await _context.WeatherAlertLogs.AnyAsync(w =>
                        w.PropertyId == property.Id &&
                        w.AlertType == alert.Type &&
                        w.TriggeredAt >= now.AddHours(-3));

                    if (duplicateExists)
                        continue;

                    _context.WeatherAlertLogs.Add(new WeatherAlertLog
                    {
                        PropertyId = property.Id,
                        AlertType = alert.Type,
                        Message = alert.Message,
                        TriggeredAt = now
                    });
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Added = addedCount, Checked = checkedCount, Message = $"Weather check completed. {addedCount} alerts added across {checkedCount} properties." });
        }

        private sealed class WeatherSnapshot
        {
            public MainSnapshot? Main { get; set; }
            public WindSnapshot? Wind { get; set; }
            public System.Collections.Generic.List<ConditionSnapshot>? Weather { get; set; }
        }

        private sealed class MainSnapshot
        {
            public double Temp { get; set; }
        }

        private sealed class WindSnapshot
        {
            public double Speed { get; set; }
        }

        private sealed class ConditionSnapshot
        {
            public string? Main { get; set; }
        }
    }
}
