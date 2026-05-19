using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RentFlow.Shared.DTOs;

namespace RentFlow.Client.Services
{
    public class MaintenanceService
    {
        private readonly HttpClient _httpClient;

        public MaintenanceService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<LandlordTicketView>?> GetLandlordTickets()
        {
            return await _httpClient.GetFromJsonAsync<List<LandlordTicketView>>("api/maintenance/landlord");
        }

        public async Task<List<TenantTicketView>?> GetTenantTickets()
        {
            return await _httpClient.GetFromJsonAsync<List<TenantTicketView>>("api/maintenance/tenant");
        }

        public async Task<int> CreateTicket(MaintenanceTicketDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/maintenance", dto);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateResult>();
                return result?.Id ?? 0;
            }
            return 0;
        }

        public async Task<bool> UpdateTicket(int id, string status, string? assignedTo)
        {
            var dto = new MaintenanceTicketDto { Status = status, AssignedTo = assignedTo };
            var response = await _httpClient.PutAsJsonAsync($"api/maintenance/{id}", dto);
            return response.IsSuccessStatusCode;
        }

        private class CreateResult { public int Id { get; set; } }
    }

    public class LandlordTicketView
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public int Urgency { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PropertyName { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string? AssignedTo { get; set; }
        public string? PhotoPath { get; set; }
        public string UnitNumber { get; set; } = "";
    }

    public class TenantTicketView
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public int Urgency { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
