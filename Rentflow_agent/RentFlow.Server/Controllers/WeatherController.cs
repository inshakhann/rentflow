using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RentFlow.Server.Data;
using RentFlow.Shared.Models;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public WeatherController(HttpClient httpClient, IConfiguration config, AppDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeather([FromQuery] double lat, [FromQuery] double lon)
        {
            var apiKey = _config["OpenWeatherMap:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Weather API key not configured.");

            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";
            
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch weather data.");

            var data = await response.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [HttpGet("landlord/alerts")]
        public async Task<IActionResult> GetLandlordAlerts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var alerts = await _context.WeatherAlertLogs
                .Include(w => w.Property)
                .Where(w => w.Property.LandlordId == userId && !w.IsRead)
                .Select(w => new
                {
                    w.Id,
                    w.PropertyId,
                    PropertyName = w.Property.Name,
                    w.AlertType,
                    w.Message,
                    w.TriggeredAt,
                    w.IsRead
                })
                .OrderByDescending(w => w.TriggeredAt)
                .ToListAsync();

            return Ok(alerts);
        }

        [HttpPut("alerts/{id}/read")]
        public async Task<IActionResult> MarkAlertAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            WeatherAlertLog? alert = null;
            if (role == "Landlord")
            {
                alert = await _context.WeatherAlertLogs
                    .Include(w => w.Property)
                    .FirstOrDefaultAsync(w => w.Id == id && w.Property.LandlordId == userId);
            }
            else if (role == "Tenant")
            {
                var lease = await _context.Leases
                    .Include(l => l.Unit)
                    .FirstOrDefaultAsync(l => l.TenantId == userId && l.IsActive);

                if (lease != null)
                {
                    alert = await _context.WeatherAlertLogs
                        .FirstOrDefaultAsync(w => w.Id == id && w.PropertyId == lease.Unit.PropertyId);
                }
            }

            if (alert == null) return NotFound();

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("tenant/alerts")]
        public async Task<IActionResult> GetTenantAlerts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role != "Tenant")
                return Forbid();

            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.TenantId == userId && l.IsActive);

            if (lease == null)
                return Ok(new System.Collections.Generic.List<object>());

            var alerts = await _context.WeatherAlertLogs
                .Where(w => w.PropertyId == lease.Unit.PropertyId && !w.IsRead)
                .Select(w => new
                {
                    w.Id,
                    w.PropertyId,
                    w.AlertType,
                    w.Message,
                    w.TriggeredAt,
                    w.IsRead
                })
                .OrderByDescending(w => w.TriggeredAt)
                .ToListAsync();

            return Ok(alerts);
        }
    }
}
