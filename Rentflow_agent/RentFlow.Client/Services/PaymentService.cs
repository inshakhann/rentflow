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

        public async Task<ReceiptData?> GetReceiptData(int paymentId)
        {
            try { return await _httpClient.GetFromJsonAsync<ReceiptData>($"api/payments/tenant/receipt/{paymentId}"); }
            catch { return null; }
        }

        public async Task<DueStatusData?> GetDueStatus()
        {
            try { return await _httpClient.GetFromJsonAsync<DueStatusData>("api/payments/tenant/due-status"); }
            catch { return null; }
        }

        public async Task<PaymentScoreData?> GetPaymentScore(int tenantId)
        {
            try { return await _httpClient.GetFromJsonAsync<PaymentScoreData>($"api/payments/score/{tenantId}"); }
            catch { return null; }
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

    public class ReceiptData
    {
        public int Id { get; set; }
        public string TenantName { get; set; } = "";
        public string TenantEmail { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public string UnitNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; }
        public decimal Total { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class DueStatusData
    {
        public bool IsDueSoon { get; set; }
        public int DaysUntilDue { get; set; }
    }

    public class PaymentScoreData
    {
        public int Score { get; set; }
        public string Label { get; set; } = "";
    }
}
