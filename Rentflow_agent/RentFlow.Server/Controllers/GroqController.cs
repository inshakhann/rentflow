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
    }

    public class ChatRequest
    {
        public object[] Messages { get; set; } = System.Array.Empty<object>();
    }
}
