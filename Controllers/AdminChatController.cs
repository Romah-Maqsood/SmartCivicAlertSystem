using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using SmartCityPulse.Services;
using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SmartCityPulse.Controllers
{
    public class AdminChatController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly RAGService _ragService;
        private readonly GeminiService _geminiService;

        public AdminChatController(MongoDbContext context, RAGService ragService, GeminiService geminiService)
        {
            _context = context;
            _ragService = ragService;
            _geminiService = geminiService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromForm] string message)   // ✅ Changed to FromForm
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, reply = "Message is empty." });

            var adminId = HttpContext.Session.GetString("UserId") ?? "admin-default";

            // Save user message
            var userMsg = new ChatHistory
            {
                UserId = adminId,
                Role = "user",
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            await _context.ChatHistoryCollection.InsertOneAsync(userMsg);

            // Build RAG context
            var context = await _ragService.BuildContextAsync();

            // Get answer from Gemini
            string reply;
            try
            {
                reply = await _geminiService.AskAsync(message, context);
            }
            catch (Exception)   // ✅ removed unused variable
            {
                reply = "Sorry, the AI assistant encountered an error. Please try again later.";
            }

            // Save bot reply
            var botMsg = new ChatHistory
            {
                UserId = adminId,
                Role = "bot",
                Message = reply,
                Timestamp = DateTime.UtcNow
            };
            await _context.ChatHistoryCollection.InsertOneAsync(botMsg);

            return Json(new { success = true, reply });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            var adminId = HttpContext.Session.GetString("UserId") ?? "admin-default";
            var history = await _context.ChatHistoryCollection
                .Find(h => h.UserId == adminId)
                .SortByDescending(h => h.Timestamp)
                .Limit(50)
                .ToListAsync();
            return Json(history);
        }
    }
}