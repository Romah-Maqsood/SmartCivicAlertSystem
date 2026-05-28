using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;
using SmartCityPulse.Hubs;

namespace SmartCityPulse.Controllers
{
    public class IncidentController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IncidentController(MongoDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                var userRole = HttpContext.Session.GetString("UserRole");
                var userName = HttpContext.Session.GetString("UserName");

                if (!string.IsNullOrEmpty(userId))
                {
                    incident.ReportedBy = userId;
                }

                await _context.Incidents.InsertOneAsync(incident);

                // Send real-time notification to department
                try
                {
                    await _hubContext.Clients.Group(incident.Department)
                        .SendAsync("ReceiveNotification",
                            "🚨 New Incident Reported",
                            $"{incident.Title} at {incident.Location}",
                            DateTime.Now);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notification error: {ex.Message}");
                }

                TempData["SuccessMessage"] = "✅ Incident reported successfully! Your report has been submitted.";

                // Redirect based on user role
                if (userRole == "Citizen")
                {
                    return RedirectToAction("Index", "Citizen");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
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

        // ==================== CREATE EMERGENCY INCIDENT (SOS) ====================
        [HttpGet]
        public async Task<IActionResult> CreateEmergency()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            var emergencyIncident = new Incident
            {
                Title = "🚨 EMERGENCY SOS - Immediate Assistance Required",
                Description = $"Emergency SOS alert raised by citizen {userName}. Immediate assistance required.",
                Location = "Location shared via emergency system",
                Severity = "Critical",
                Status = "Open",
                ReportedBy = userId,
                ReportedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Department = "Emergency Response",
                Comments = new List<IncidentComment>()
            };

            await _context.Incidents.InsertOneAsync(emergencyIncident);

            // Send emergency notification to all departments
            try
            {
                await _hubContext.Clients.All
                    .SendAsync("ReceiveNotification",
                        "🚨 EMERGENCY SOS",
                        $"Emergency alert from {userName}. Immediate assistance required!",
                        DateTime.Now);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Emergency notification error: {ex.Message}");
            }

            TempData["SuccessMessage"] = "🚨 Emergency alert sent! Authorities have been notified.";

            if (userRole == "Citizen")
            {
                return RedirectToAction("Index", "Citizen");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}