using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RentFlow.Shared.DTOs;

namespace RentFlow.Client.Services
{
    public class LeaseService
    {
        private readonly HttpClient _httpClient;

        public LeaseService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<LeaseDto>?> GetLandlordLeases()
        {
            return await _httpClient.GetFromJsonAsync<List<LeaseDto>>("api/leases/landlord");
        }

        public async Task<List<AvailableTenantView>?> GetAvailableTenants()
        {
            return await _httpClient.GetFromJsonAsync<List<AvailableTenantView>>("api/leases/available-tenants");
        }

        public async Task<bool> CreateLease(CreateLeaseDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/leases", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<TenantLeaseDto?> GetTenantLease()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<TenantLeaseDto>("api/leases/tenant");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaseCountdownData?> GetLeaseCountdown()
        {
            try { return await _httpClient.GetFromJsonAsync<LeaseCountdownData>("api/leases/tenant/countdown"); }
            catch { return null; }
        }

        public async Task<List<OccupancyMonth>?> GetOccupancyHeatmap()
        {
            try { return await _httpClient.GetFromJsonAsync<List<OccupancyMonth>>("api/leases/landlord/occupancy-heatmap"); }
            catch { return new List<OccupancyMonth>(); }
        }
    }

    public class AvailableTenantView
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class LeaseCountdownData
    {
        public int DaysRemaining { get; set; }
        public int TotalDays { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class OccupancyMonth
    {
        public int Month { get; set; }
        public int Occupancy { get; set; }
    }
}
