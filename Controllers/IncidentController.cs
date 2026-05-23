using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;

namespace SmartCityPulse.Controllers
{
    public class IncidentController : Controller
    {
        private readonly MongoDbContext _context;

        public IncidentController(MongoDbContext context)
        {
            _context = context;
        }

        // ==================== PUBLIC: Report Incident (Citizen/Public) ====================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Incident());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Incident incident)
        {
            if (ModelState.IsValid)
            {
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";
                incident.Comments = new List<IncidentComment>();

                // Save citizen ID if logged in
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

        // ==================== PUBLIC: Basic Incident List ====================
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true).ToListAsync();
            return View(incidents);
        }

        // ==================== GET: Incident Details ====================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
            {
                return NotFound();
            }

            return View(incident);
        }

        // ==================== GET: My Incidents (For Citizen) ====================
        [HttpGet]
        public async Task<IActionResult> MyIncidents()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            return View(incidents);
        }
    }
}