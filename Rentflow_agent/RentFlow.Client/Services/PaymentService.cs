using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RentFlow.Client.Services
{
    public class PaymentService
    {
        private readonly HttpClient _httpClient;

        public PaymentService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ServerAPI");
        }

        public async Task<List<RevenueItem>?> GetLandlordRevenue()
        {
            return await _httpClient.GetFromJsonAsync<List<RevenueItem>>("api/payments/landlord/revenue");
        }

        public async Task<List<LatePaymentItem>?> GetLatePayments()
        {
            return await _httpClient.GetFromJsonAsync<List<LatePaymentItem>>("api/payments/landlord/late");
        }

        public async Task<List<TenantPaymentHistory>?> GetTenantHistory()
        {
            return await _httpClient.GetFromJsonAsync<List<TenantPaymentHistory>>("api/payments/tenant/history");
        }

        public async Task<bool> PayPayment(int paymentId)
        {
            var response = await _httpClient.PostAsync($"api/payments/{paymentId}/pay", null);
            return response.IsSuccessStatusCode;
        }
    }

    public class RevenueItem
    {
        public string Label { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class LatePaymentItem
    {
        public string TenantName { get; set; } = "";
        public string UnitNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class TenantPaymentHistory
    {
        public int Id { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; }
        public string Status { get; set; } = "";
    }
}
