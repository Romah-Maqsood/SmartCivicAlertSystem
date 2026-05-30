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
        public async Task<IActionResult> Create([FromBody] Incident incident)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userName = HttpContext.Session.GetString("UserName");
                var userRole = HttpContext.Session.GetString("UserRole");

                // ❌ REMOVED: incident.Id = Guid.NewGuid().ToString();
                // MongoDB will generate a valid ObjectId automatically
                incident.Id = null;
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";
                incident.Comments = new List<IncidentComment>();

                if (!string.IsNullOrEmpty(userId))
                {
                    incident.ReportedBy = userId;
                    incident.ReportedByName = userName ?? "Unknown User";
                }
                else
                {
                    incident.ReportedByName = "Anonymous User";
                }

                await _context.Incidents.InsertOneAsync(incident);

                return Json(new { success = true, message = "Incident reported successfully!", incidentId = incident.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetDepartmentBySeverity(string severity)
        {
            switch (severity?.ToLower())
            {
                case "critical":
                    return "Emergency Response";
                case "high":
                    return "Police Department";
                case "medium":
                    return "Fire Department";
                case "low":
                    return "General Services";
                default:
                    return "General Services";
            }
        }

        // ==================== PUBLIC: Basic Incident List ====================
        public async Task<IActionResult> Index()
        {
            var incidents = await _context.Incidents.Find(_ => true).SortByDescending(i => i.ReportedAt).ToListAsync();
            return View(incidents);
        }

        // ==================== GET: Incident Details ====================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return NotFound();

            return View(incident);
        }

        // ==================== GET: Incident Details JSON (For AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetIncidentJson(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid incident ID" });

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return Json(new { success = false, message = "Incident not found" });

            return Json(new
            {
                success = true,
                id = incident.Id,
                title = incident.Title,
                description = incident.Description,
                location = incident.Location,
                severity = incident.Severity,
                status = incident.Status,
                department = incident.Department,
                reportedAt = incident.ReportedAt,
                updatedAt = incident.UpdatedAt,
                reportedByName = incident.ReportedByName,
                latitude = incident.Latitude,
                longitude = incident.Longitude
            });
        }

        // ==================== GET: My Incidents (For Citizen) ====================
        [HttpGet]
        public async Task<IActionResult> MyIncidents()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

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

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var emergencyIncident = new Incident
            {
                // ❌ REMOVED: Id = Guid.NewGuid().ToString();
                Title = "🚨 EMERGENCY SOS - Immediate Assistance Required",
                Description = $"Emergency SOS alert raised by citizen {userName}. Immediate assistance required.",
                Location = "Location shared via emergency system",
                Severity = "Critical",
                Status = "Open",
                ReportedBy = userId,
                ReportedByName = userName ?? "Citizen",
                ReportedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Department = "Emergency Response",
                Comments = new List<IncidentComment>()
            };

            await _context.Incidents.InsertOneAsync(emergencyIncident);

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification",
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
                return RedirectToAction("Index", "Citizen");

            return RedirectToAction("Index", "Home");
        }

        // ==================== UPDATE INCIDENT STATUS (For Operators) ====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status))
                return Json(new { success = false, message = "Invalid data" });

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return Json(new { success = false, message = "Incident not found" });

            incident.Status = status;
            incident.UpdatedAt = DateTime.UtcNow;
            await _context.Incidents.ReplaceOneAsync(i => i.Id == id, incident);

            try
            {
                if (!string.IsNullOrEmpty(incident.ReportedBy))
                {
                    await _hubContext.Clients.User(incident.ReportedBy).SendAsync("ReceiveNotification",
                        "Incident Status Updated",
                        $"Your incident '{incident.Title}' status changed to {status}",
                        DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Status update notification error: {ex.Message}");
            }

            return Json(new { success = true, message = $"Status updated to {status}" });
        }

        // ==================== ADD COMMENT TO INCIDENT ====================
        [HttpPost]
        public async Task<IActionResult> AddComment(string id, string comment)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(comment))
                return Json(new { success = false, message = "Invalid data" });

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return Json(new { success = false, message = "Incident not found" });

            var userName = HttpContext.Session.GetString("UserName") ?? "System";
            var userRole = HttpContext.Session.GetString("UserRole") ?? "User";

            incident.Comments.Add(new IncidentComment
            {
                Text = comment,
                Author = $"{userName} ({userRole})",
                CreatedAt = DateTime.UtcNow
            });
            incident.UpdatedAt = DateTime.UtcNow;
            await _context.Incidents.ReplaceOneAsync(i => i.Id == id, incident);

            return Json(new { success = true, message = "Comment added successfully" });
        }
    }
}