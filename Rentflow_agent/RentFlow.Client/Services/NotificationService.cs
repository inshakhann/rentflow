using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RentFlow.Client.Services
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;

        public NotificationService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<NotificationView>?> GetNotifications()
        {
            try { return await _httpClient.GetFromJsonAsync<List<NotificationView>>("api/notifications"); }
            catch { return new List<NotificationView>(); }
        }

        public async Task<bool> MarkAsRead(int id)
        {
            var response = await _httpClient.PutAsync($"api/notifications/{id}/read", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MarkAllAsRead()
        {
            var response = await _httpClient.PutAsync("api/notifications/read-all", null);
            return response.IsSuccessStatusCode;
        }
    }

    public class NotificationView
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "System";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
