using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RentFlow.Shared.DTOs;

namespace RentFlow.Client.Services
{
    public class PropertyService
    {
        private readonly HttpClient _httpClient;

        public PropertyService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<PropertyView>?> GetProperties()
        {
            return await _httpClient.GetFromJsonAsync<List<PropertyView>>("api/properties");
        }

        public async Task<PropertyDetailView?> GetProperty(int id)
        {
            return await _httpClient.GetFromJsonAsync<PropertyDetailView>($"api/properties/{id}");
        }

        public async Task<int> CreateProperty(PropertyDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/properties", dto);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateResult>();
                return result?.Id ?? 0;
            }
            return 0;
        }

        private class CreateResult { public int Id { get; set; } }
    }

    public class PropertyDetailView
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<UnitView> Units { get; set; } = new();
    }

    public class UnitView
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = "";
        public decimal MonthlyRent { get; set; }
        public bool IsOccupied { get; set; }
        public string? TenantName { get; set; }
    }
}
