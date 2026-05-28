using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;

namespace SmartCityPulse.Controllers
{
    public class IncidentController : Controller
    {
        private readonly MongoDbContext _context;

        public IncidentController(MongoDbContext context)
        {
            _context = context;
        }

        // ==================== PUBLIC: Report Incident ====================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Incident incident)
        {
            if (ModelState.IsValid)
            {
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";

                // ✅ Store the logged-in citizen's ID (if any)
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId))
                {
                    incident.ReportedBy = userId;
                }

                await _context.Incidents.InsertOneAsync(incident);

                TempData["SuccessMessage"] = "✅ Incident reported successfully!";
                return RedirectToAction("Index", "Home");
            }
            return View(incident);
        }

        // ==================== PUBLIC: Incident List (optional) ====================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();
            return View(incidents);
        }
    }
}