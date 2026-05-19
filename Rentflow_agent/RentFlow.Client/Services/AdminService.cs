using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;

namespace RentFlow.Client.Services
{
    public class AdminService
    {
        private readonly HttpClient _httpClient;

        public AdminService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<DashboardStats?> GetDashboardStats()
        {
            return await _httpClient.GetFromJsonAsync<DashboardStats>("api/admin/dashboard");
        }

        public async Task<List<LandlordView>?> GetLandlords()
        {
            return await _httpClient.GetFromJsonAsync<List<LandlordView>>("api/admin/landlords");
        }

        public async Task<List<TenantView>?> GetTenants()
        {
            return await _httpClient.GetFromJsonAsync<List<TenantView>>("api/admin/tenants");
        }

        public async Task<List<PropertyView>?> GetProperties()
        {
            return await _httpClient.GetFromJsonAsync<List<PropertyView>>("api/admin/properties");
        }

        public async Task<LandlordDetailView?> GetLandlordDetails(int id)
        {
            return await _httpClient.GetFromJsonAsync<LandlordDetailView>($"api/admin/landlords/{id}");
        }

        public async Task<bool> UpdateLandlordStatus(int id, bool active)
        {
            var response = await _httpClient.PutAsync($"api/admin/landlords/{id}/status?active={active.ToString().ToLowerInvariant()}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<TenantDetailView?> GetTenantDetails(int id)
        {
            return await _httpClient.GetFromJsonAsync<TenantDetailView>($"api/admin/tenants/{id}");
        }

        public async Task<WeatherCheckResult?> TriggerWeatherCheck()
        {
            var response = await _httpClient.PostAsync("api/admin/weather/trigger-check", null);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<WeatherCheckResult>();
        }
    }

    public class DashboardStats
    {
        public int TotalLandlords { get; set; }
        public int TotalTenants { get; set; }
        public int ActiveLeases { get; set; }
        public int OpenTickets { get; set; }
        public List<TicketCategory> TicketsByCategory { get; set; } = new();
        public List<ActivityLog> RecentActivity { get; set; } = new();
    }

    public class TicketCategory { public string Category { get; set; } = ""; public int Count { get; set; } }
    public class ActivityLog { public string Event { get; set; } = ""; public string User { get; set; } = ""; public System.DateTime Time { get; set; } }

    public class LandlordView
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public int PropertiesCount { get; set; }
    }

    public class TenantView
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public string Unit { get; set; } = "";
        public string Landlord { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    public class PropertyView
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string LandlordName { get; set; } = "";
        public int TotalUnits { get; set; }
        public double OccupancyPercent { get; set; }
    }

    public class LandlordDetailView
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PropertiesCount { get; set; }
        public int ActiveUnits { get; set; }
        public int OccupiedUnits { get; set; }
    }

    public class TenantDetailView
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public TenantLeaseDetailView? ActiveLease { get; set; }
    }

    public class TenantLeaseDetailView
    {
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public string UnitNumber { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public string LandlordName { get; set; } = "";
    }

    public class WeatherCheckResult
    {
        public int Added { get; set; }
        public int Checked { get; set; }
        public string Message { get; set; } = "";
    }
}
