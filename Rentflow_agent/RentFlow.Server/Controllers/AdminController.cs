using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using RentFlow.Shared.Models;
using RentFlow.Server.Services;

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
            var weatherApiComKey = ApiKeyResolver.Resolve(
                _configuration,
                "WeatherApi:ApiKey",
                "WEATHERAPI_COM_API_KEY",
                "WEATHERAPI_API_KEY");

            var openWeatherMapKey = ApiKeyResolver.Resolve(
                _configuration,
                "OpenWeatherMap:ApiKey",
                "OPENWEATHERMAP_API_KEY",
                "WEATHERMAP_API_KEY");

            var properties = await _context.Properties
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                .ToListAsync();

            if (!properties.Any())
            {
                return Ok(new { Added = 0, Checked = 0, Message = "No properties with coordinates found." });
            }

            if (string.IsNullOrWhiteSpace(weatherApiComKey) && string.IsNullOrWhiteSpace(openWeatherMapKey))
            {
                return StatusCode(500, new
                {
                    Added = 0,
                    Checked = properties.Count,
                    Message = "Weather API key is missing. Configure WeatherApi:ApiKey/WEATHERAPI_COM_API_KEY or OpenWeatherMap:ApiKey/OPENWEATHERMAP_API_KEY."
                });
            }

            var client = _httpClientFactory.CreateClient();
            var now = System.DateTime.UtcNow;
            var addedCount = 0;
            var checkedCount = 0;
            var failedChecks = 0;

            foreach (var property in properties)
            {
                checkedCount++;
                var lat = property.Latitude!.Value;
                var lon = property.Longitude!.Value;

                var weather = await GetUnifiedWeatherSnapshot(client, lat, lon, weatherApiComKey, openWeatherMapKey);
                if (weather == null)
                {
                    failedChecks++;
                    continue;
                }

                var conditionText = weather.ConditionText.ToLowerInvariant();
                var alertsToAdd = new System.Collections.Generic.List<(string Type, string Message)>();

                if (weather.TemperatureC <= 3)
                {
                    alertsToAdd.Add(("Freeze", "Freezing temperatures detected. Protect exposed pipes and check heating systems."));
                }

                if (weather.TemperatureC >= 40)
                {
                    alertsToAdd.Add(("Heatwave", "Extreme heat warning. Inspect cooling systems and hydration points."));
                }

                if (weather.WindSpeedMps >= 13 || conditionText.Contains("storm") || conditionText.Contains("thunder"))
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
            return Ok(new
            {
                Added = addedCount,
                Checked = checkedCount,
                Failed = failedChecks,
                Message = $"Weather check completed. {addedCount} alerts added across {checkedCount} properties." +
                          (failedChecks > 0 ? $" {failedChecks} API request(s) failed." : string.Empty)
            });
        }

        private async Task<UnifiedWeatherSnapshot?> GetUnifiedWeatherSnapshot(
            HttpClient client,
            double lat,
            double lon,
            string? weatherApiComKey,
            string? openWeatherMapKey)
        {
            if (!string.IsNullOrWhiteSpace(weatherApiComKey))
            {
                var weatherApiUrl = $"https://api.weatherapi.com/v1/current.json?key={weatherApiComKey}&q={lat},{lon}&aqi=no";
                try
                {
                    var weatherApiResponse = await client.GetFromJsonAsync<WeatherApiCurrentResponse>(weatherApiUrl);
                    if (weatherApiResponse?.Current != null)
                    {
                        return new UnifiedWeatherSnapshot
                        {
                            TemperatureC = weatherApiResponse.Current.TempC,
                            WindSpeedMps = weatherApiResponse.Current.WindKph / 3.6,
                            ConditionText = weatherApiResponse.Current.Condition?.Text ?? string.Empty
                        };
                    }
                }
                catch
                {
                    // fallback to OpenWeatherMap if configured
                }
            }

            if (!string.IsNullOrWhiteSpace(openWeatherMapKey))
            {
                var openWeatherUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={openWeatherMapKey}&units=metric";
                try
                {
                    var openWeatherResponse = await client.GetFromJsonAsync<OpenWeatherSnapshot>(openWeatherUrl);
                    if (openWeatherResponse?.Main != null)
                    {
                        return new UnifiedWeatherSnapshot
                        {
                            TemperatureC = openWeatherResponse.Main.Temp,
                            WindSpeedMps = openWeatherResponse.Wind?.Speed ?? 0,
                            ConditionText = openWeatherResponse.Weather?.FirstOrDefault()?.Main ?? string.Empty
                        };
                    }
                }
                catch
                {
                    // no provider left
                }
            }

            return null;
        }

        private sealed class UnifiedWeatherSnapshot
        {
            public double TemperatureC { get; set; }
            public double WindSpeedMps { get; set; }
            public string ConditionText { get; set; } = string.Empty;
        }

        private sealed class WeatherApiCurrentResponse
        {
            public WeatherApiCurrent? Current { get; set; }
        }

        private sealed class WeatherApiCurrent
        {
            public double TempC { get; set; }
            public double WindKph { get; set; }
            public WeatherApiCondition? Condition { get; set; }
        }

        private sealed class WeatherApiCondition
        {
            public string? Text { get; set; }
        }

        private sealed class OpenWeatherSnapshot
        {
            public OpenWeatherMain? Main { get; set; }
            public OpenWeatherWind? Wind { get; set; }
            public System.Collections.Generic.List<OpenWeatherCondition>? Weather { get; set; }
        }

        private sealed class OpenWeatherMain
        {
            public double Temp { get; set; }
        }

        private sealed class OpenWeatherWind
        {
            public double Speed { get; set; }
        }

        private sealed class OpenWeatherCondition
        {
            public string? Main { get; set; }
        }

    }
}
