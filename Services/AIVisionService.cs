using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartCityPulse.Services
{
    public class AIVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<AIVisionService> _logger;

        // Standard Gemini API endpoint — works with ALL key formats including AQ. keys
        private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={0}";

        public AIVisionService(IConfiguration configuration, ILogger<AIVisionService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GeminiApiKey:Citizen"] ?? throw new Exception("Gemini API Key for Citizen not found in configuration");
            _logger = logger;
        }

        public async Task<AIAnalysisResult> AnalyzeIncidentImage(byte[] imageBytes, string mimeType = "image/jpeg")
        {
            try
            {
                string base64Image = Convert.ToBase64String(imageBytes);
                _logger.LogInformation($"Analyzing image of size: {imageBytes.Length} bytes");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Image
                                    }
                                },
                                new
                                {
                                    text = @"Analyze this image. This is an emergency incident photo. 
                                    Return ONLY valid JSON. Do not add any other text before or after the JSON.
                                    {
                                        ""title"": ""Short title of what happened (max 60 characters)"",
                                        ""description"": ""Brief description of the incident (max 200 characters)"",
                                        ""severity"": ""Critical or High or Medium or Low"",
                                        ""department"": ""Fire Department or Police Department or Rescue Department""
                                    }"
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Use API key as a query parameter — no Authorization header needed
                _httpClient.DefaultRequestHeaders.Clear();

                var requestUrl = string.Format(GeminiApiUrl, _apiKey);

                _logger.LogInformation("Calling Gemini API...");
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Body: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    var result = ParseGeminiResponse(responseJson);
                    return result;
                }

                _logger.LogError($"API Error: {response.StatusCode} - {responseJson}");
                return GetDefaultAnalysis($"API Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return GetDefaultAnalysis($"Exception: {ex.Message}");
            }
        }

        private AIAnalysisResult ParseGeminiResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var content))
                    {
                        if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var part = parts[0];
                            if (part.TryGetProperty("text", out var textElement))
                            {
                                var text = textElement.GetString();
                                _logger.LogInformation($"Raw AI Text: {text}");

                                if (!string.IsNullOrEmpty(text))
                                {
                                    int jsonStart = text.IndexOf('{');
                                    int jsonEnd = text.LastIndexOf('}') + 1;

                                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                                    {
                                        string jsonText = text.Substring(jsonStart, jsonEnd - jsonStart);
                                        using var jsonDoc = JsonDocument.Parse(jsonText);
                                        var rootElement = jsonDoc.RootElement;

                                        return new AIAnalysisResult
                                        {
                                            Title = rootElement.TryGetProperty("title", out var title) ? title.GetString() ?? "Incident Detected" : "Incident Detected",
                                            Description = rootElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "No description" : "No description",
                                            Severity = rootElement.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "Medium" : "Medium",
                                            Department = rootElement.TryGetProperty("department", out var dept) ? dept.GetString() ?? "General Services" : "General Services",
                                            Success = true
                                        };
                                    }
                                }
                            }
                        }
                    }
                }

                return GetDefaultAnalysis("No valid response from API");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Parse error: {ex.Message}");
                return GetDefaultAnalysis($"Parse error: {ex.Message}");
            }
        }

        private AIAnalysisResult GetDefaultAnalysis(string reason)
        {
            return new AIAnalysisResult
            {
                Title = "Incident Detected",
                Description = $"AI could not analyze: {reason}. Please provide description manually.",
                Severity = "Medium",
                Department = "General Services",
                Success = false
            };
        }
    }

    public class AIAnalysisResult
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}