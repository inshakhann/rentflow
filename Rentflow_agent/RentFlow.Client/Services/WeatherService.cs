using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RentFlow.Client.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<WeatherAlertView>?> GetLandlordAlerts()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<WeatherAlertView>>("api/weather/landlord/alerts");
            }
            catch (Exception)
            {
                return new List<WeatherAlertView>();
            }
        }

        public async Task<bool> MarkAlertAsRead(int id)
        {
            var response = await _httpClient.PutAsync($"api/weather/alerts/{id}/read", null);
            return response.IsSuccessStatusCode;
        }
    }

    public class WeatherAlertView
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public bool IsRead { get; set; }
    }
}
