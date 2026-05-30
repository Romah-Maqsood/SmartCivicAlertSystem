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
        private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public AIVisionService(IConfiguration configuration, ILogger<AIVisionService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GeminiAPI:ApiKey"] ?? throw new Exception("Gemini API Key not found in configuration");
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
                                    text = @"Analyze this image carefully. This is an emergency incident photo. 
                                    Return ONLY valid JSON in this exact format, no other text:
                                    {
                                        ""title"": ""A specific, detailed title for this incident (max 60 characters)"",
                                        ""description"": ""A detailed description of what you see in the image (max 200 characters)"",
                                        ""severity"": ""One of: Critical, High, Medium, Low"",
                                        ""department"": ""One of: Fire Department, Police Department, Rescue Department""
                                    }
                                    For example:
                                    - If you see fire/smoke: title = 'Fire in Building', severity = 'Critical', department = 'Fire Department'
                                    - If you see car accident: title = 'Car Accident', severity = 'High', department = 'Rescue Department'
                                    - If you see theft/robbery: title = 'Theft Incident', severity = 'Medium', department = 'Police Department'
                                    - If you see medical emergency: title = 'Medical Emergency', severity = 'High', department = 'Rescue Department'"
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var requestUrl = $"{GeminiApiUrl}?key={_apiKey}";

                _logger.LogInformation($"Calling Gemini API at: {requestUrl}");
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Gemini API Response Status: {response.StatusCode}");
                _logger.LogInformation($"Gemini API Response: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    var result = ParseGeminiResponse(responseJson);
                    _logger.LogInformation($"Parsed Result - Title: {result.Title}, Severity: {result.Severity}, Department: {result.Department}");
                    return result;
                }

                _logger.LogError($"Gemini API Error: {response.StatusCode} - {responseJson}");
                return GetDefaultAnalysis("API Error: Unable to analyze image");
            }
            catch (Exception ex)
            {
                _logger.LogError($"AI Vision Analysis Error: {ex.Message}");
                return GetDefaultAnalysis($"Error: {ex.Message}");
            }
        }

        private AIAnalysisResult ParseGeminiResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Navigate to the text response
                var text = root.GetProperty("candidates")[0]
                               .GetProperty("content")
                               .GetProperty("parts")[0]
                               .GetProperty("text")
                               .GetString();

                _logger.LogInformation($"Raw AI Response: {text}");

                if (!string.IsNullOrEmpty(text))
                {
                    // Extract JSON from response
                    var jsonStart = text.IndexOf('{');
                    var jsonEnd = text.LastIndexOf('}') + 1;
                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        var jsonText = text.Substring(jsonStart, jsonEnd - jsonStart);
                        using var jsonDoc = JsonDocument.Parse(jsonText);
                        var rootElement = jsonDoc.RootElement;

                        return new AIAnalysisResult
                        {
                            Title = rootElement.TryGetProperty("title", out var title) ? title.GetString() ?? "Incident Detected" : "Incident Detected",
                            Description = rootElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "Please investigate this incident" : "Please investigate this incident",
                            Severity = rootElement.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "Medium" : "Medium",
                            Department = rootElement.TryGetProperty("department", out var dept) ? dept.GetString() ?? "General Services" : "General Services",
                            Success = true
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Parse error: {ex.Message}");
            }

            return GetDefaultAnalysis("Failed to parse AI response");
        }

        private AIAnalysisResult GetDefaultAnalysis(string reason = "")
        {
            return new AIAnalysisResult
            {
                Title = "Incident Detected",
                Description = "AI could not analyze this image. Please provide description manually.",
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