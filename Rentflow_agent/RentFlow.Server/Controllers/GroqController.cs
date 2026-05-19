using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RentFlow.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroqController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GroqController> _logger;

        public GroqController(HttpClient httpClient, IConfiguration config, ILogger<GroqController> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var normalizedMessages = NormalizeMessages(request.Messages);
            var system = new ChatMessage
            {
                Role = "system",
                Content =
                    "You are RentFlow Maintenance AI. Diagnose maintenance issues clearly and reason step-by-step. " +
                    "Ask focused follow-up questions when missing details (location, symptoms, timing, safety risk). " +
                    "Prioritize tenant safety, urgency classification (1 Minor, 2 Moderate, 3 Emergency), and practical next steps. " +
                    "Keep responses concise, professional, and actionable."
            };

            var allMessages = new[] { system }.Concat(normalizedMessages).ToList();
            var result = await CallGroq(allMessages, purpose: "maintenance");
            if (result == null)
                return StatusCode(500, "Groq API error. Verify Groq key/model configuration.");

            return Content(result, "application/json");
        }

        private async Task<string?> CallGroq(IReadOnlyList<ChatMessage> messages, string purpose)
        {
            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var models = ResolveModels(purpose);
            var lastError = string.Empty;

            foreach (var model in models)
            {
                var groqRequest = new
                {
                    model,
                    messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                    temperature = 0.3,
                    max_tokens = 1300
                };

                using var requestMessage = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                requestMessage.Headers.TryAddWithoutValidation("User-Agent", "RentFlow/1.0");
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(groqRequest), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                lastError = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Groq request failed for model {Model}. Status={StatusCode}. Body={Body}", model, (int)response.StatusCode, lastError);
            }

            return null;
        }

        [HttpPost("negotiate")]
        public async Task<IActionResult> Negotiate([FromBody] ChatRequest request)
        {
            var normalizedMessages = NormalizeMessages(request.Messages);
            var systemMsg = new ChatMessage
            {
                Role = "system",
                Content =
                    "You are RentFlow's AI rent negotiation assistant. Provide calm, realistic reasoning. " +
                    "Gather missing context first (income change, duration, proposed temporary terms). " +
                    "Then draft a professional landlord letter with: situation summary, requested concession, proposed payment plan, and cooperative close. " +
                    "Tone must be respectful, factual, and solution-oriented."
            };
            var allMessages = new[] { systemMsg }.Concat(normalizedMessages).ToList();

            var result = await CallGroq(allMessages, purpose: "negotiation");
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

            var normalizedMessages = NormalizeMessages(messages);
            var result = await CallGroq(normalizedMessages, purpose: "pricing");
            if (result == null) return StatusCode(500, "Groq API error");
            return Content(result, "application/json");
        }

        private List<ChatMessage> NormalizeMessages(IEnumerable<object>? rawMessages)
        {
            var result = new List<ChatMessage>();
            if (rawMessages == null)
                return result;

            foreach (var item in rawMessages)
            {
                if (item is ChatMessage typed)
                {
                    if (!string.IsNullOrWhiteSpace(typed.Role) && !string.IsNullOrWhiteSpace(typed.Content))
                        result.Add(typed);
                    continue;
                }

                if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var role = element.TryGetProperty("role", out var r) ? r.GetString() : null;
                    var content = element.TryGetProperty("content", out var c) ? c.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(content))
                    {
                        result.Add(new ChatMessage { Role = role!, Content = content! });
                    }
                    continue;
                }

                try
                {
                    var roleProp = item.GetType().GetProperty("role") ?? item.GetType().GetProperty("Role");
                    var contentProp = item.GetType().GetProperty("content") ?? item.GetType().GetProperty("Content");
                    var role = roleProp?.GetValue(item)?.ToString();
                    var content = contentProp?.GetValue(item)?.ToString();
                    if (!string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(content))
                    {
                        result.Add(new ChatMessage { Role = role!, Content = content! });
                    }
                }
                catch
                {
                    // Ignore malformed message entries.
                }
            }

            return result;
        }

        private IReadOnlyList<string> ResolveModels(string purpose)
        {
            var configuredPrimary = _config[$"Groq:Models:{purpose}"];
            var configuredFallback = _config["Groq:Models:Fallback"];
            var defaults = new[] { "llama-3.3-70b-versatile", "llama-3.1-8b-instant" };

            var models = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredPrimary))
                models.Add(configuredPrimary);
            if (!string.IsNullOrWhiteSpace(configuredFallback))
                models.Add(configuredFallback);
            models.AddRange(defaults);

            return models.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public class ChatRequest
    {
        public object[] Messages { get; set; } = Array.Empty<object>();
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class SuggestRentRequest
    {
        public string City { get; set; } = "";
        public int Bedrooms { get; set; }
        public string Size { get; set; } = "";
    }
}
