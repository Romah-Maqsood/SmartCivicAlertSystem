using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartCityPulse.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["GeminiApiKey:Admin"];
        }

        public async Task<string> AskAsync(string userQuestion, string context)
        {
            var prompt = $@"
You are a professional analytics assistant for the Smart Civic Alert System.
Use ONLY the provided context to answer. If the answer is not in the context, say 'I don't have enough data to answer that.'
Be clear, concise, and professional. Do not use emojis. Use standard icons like ✓ ✗ ⚠ where appropriate.

Context:
{context}

Question:
{userQuestion}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 512
                }
            };

            // ✅ Using Gemini 2.5 Flash (latest stable free-tier model)
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Gemini API error: " + responseBody);
                    return "AI service is currently unavailable. Please try again later.";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var answer = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return answer ?? "I couldn't process that request.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gemini exception: " + ex.Message);
                return "Sorry, the AI assistant encountered an error. Please try again later.";
            }
        }
    }
}