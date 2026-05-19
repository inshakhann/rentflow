using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroqController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GroqController(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Groq API key not configured.");

            var groqRequest = new
            {
                model = "llama-3.1-8b-instant", // Use a fast LLaMA 3.1 model on Groq
                messages = request.Messages,
                temperature = 0.5,
                max_tokens = 1024
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(groqRequest), Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }
            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Groq API error: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        private async Task<string?> CallGroq(object[] messages)
        {
            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var groqRequest = new
            {
                model = "llama-3.1-8b-instant",
                messages = messages,
                temperature = 0.5,
                max_tokens = 1024
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(groqRequest), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", jsonContent);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadAsStringAsync();
        }

        [HttpPost("negotiate")]
        public async Task<IActionResult> Negotiate([FromBody] ChatRequest request)
        {
            var systemMsg = new { role = "system", content = "You are RentFlow's AI rent negotiation assistant. The tenant is describing their financial situation. Draft a polite, professional rent deferral or reduction request letter that the tenant can send to their landlord. Be empathetic. Output the letter as clean text, ready to copy." };
            var allMessages = new object[] { systemMsg }.Concat(request.Messages.Cast<object>()).ToArray();

            var result = await CallGroq(allMessages);
            if (result == null) return StatusCode(500, "Groq API error");
            return Content(result, "application/json");
        }

        [HttpPost("suggest-rent")]
        public async Task<IActionResult> SuggestRent([FromBody] SuggestRentRequest request)
        {
            var prompt = $"Based on the following property details in Pakistan, suggest a competitive monthly rent range in PKR. City: {request.City}. Bedrooms: {request.Bedrooms}. Size: {request.Size}. Provide ONLY a JSON response like: {{\"minRent\": 30000, \"maxRent\": 50000, \"reasoning\": \"brief explanation\"}}";

            var messages = new object[]
            {
                new { role = "system", content = "You are a Pakistani real estate pricing AI. Respond ONLY with valid JSON." },
                new { role = "user", content = prompt }
            };

            var result = await CallGroq(messages);
            if (result == null) return StatusCode(500, "Groq API error");
            return Content(result, "application/json");
        }
    }

    public class ChatRequest
    {
        public object[] Messages { get; set; } = System.Array.Empty<object>();
    }

    public class SuggestRentRequest
    {
        public string City { get; set; } = "";
        public int Bedrooms { get; set; }
        public string Size { get; set; } = "";
    }
}
