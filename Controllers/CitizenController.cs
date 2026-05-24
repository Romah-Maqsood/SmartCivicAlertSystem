using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using SmartCityPulse.Models;
using SmartCityPulse.Data;

namespace SmartCityPulse.Controllers
{
    public class CitizenController : Controller
    {
        private readonly MongoDbContext _context;

        public CitizenController(MongoDbContext context)
        {
            _context = context;
        }

        // Check if user is citizen
        private bool IsCitizen()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Citizen";
        }

        // ==================== CITIZEN DASHBOARD ====================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Get citizen's own incidents (for recent table)
            var myIncidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .Limit(5)
                .ToListAsync();

            // Get all incidents for My Reports section
            var allIncidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            // Get statistics
            var totalIncidents = await _context.Incidents
                .CountAsync(i => i.ReportedBy == userId);

            var resolvedIncidents = await _context.Incidents
                .CountAsync(i => i.ReportedBy == userId && i.Status == "Resolved");

            var pendingIncidents = await _context.Incidents
                .CountAsync(i => i.ReportedBy == userId && i.Status != "Resolved");

            var criticalIncidents = await _context.Incidents
                .CountAsync(i => i.ReportedBy == userId && i.Severity == "Critical");

            // Weekly data for chart (last 7 days)
            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);
            }

            ViewBag.UserName = userName;
            ViewBag.UserEmail = userEmail;
            ViewBag.TotalIncidents = totalIncidents;
            ViewBag.ResolvedIncidents = resolvedIncidents;
            ViewBag.PendingIncidents = pendingIncidents;
            ViewBag.CriticalIncidents = criticalIncidents;
            ViewBag.MyIncidents = myIncidents;
            ViewBag.AllIncidents = allIncidents;
            ViewBag.WeeklyData = weeklyData;

            return View();
        }

        // ==================== MY REPORTS ====================
        [HttpGet]
        public async Task<IActionResult> MyReports()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            return View(incidents);
        }

        // ==================== INCIDENT DETAILS (JSON for AJAX) ====================
        [HttpGet]
        public async Task<IActionResult> GetIncidentJson(string id)
        {
            if (!IsCitizen())
            {
                return Unauthorized();
            }

            var userId = HttpContext.Session.GetString("UserId");
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (incident == null)
            {
                return NotFound();
            }

            // Check if incident belongs to this citizen
            if (incident.ReportedBy != userId)
            {
                return Unauthorized();
            }

            return Json(new
            {
                id = incident.Id,
                title = incident.Title,
                description = incident.Description,
                location = incident.Location,
                severity = incident.Severity,
                status = incident.Status,
                reportedAt = incident.ReportedAt,
                updatedAt = incident.UpdatedAt
            });
        }

        // ==================== INCIDENT DETAILS VIEW ====================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (incident == null)
            {
                return NotFound();
            }

            // Check if incident belongs to this citizen
            if (incident.ReportedBy != userId)
            {
                return Unauthorized();
            }

            return View(incident);
        }

        // ==================== MAP VIEW (Placeholder) ====================
        [HttpGet]
        public IActionResult MapView()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // ==================== NOTIFICATIONS (Placeholder) ====================
        [HttpGet]
        public IActionResult Notifications()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // ==================== DOWNLOAD REPORTS (Placeholder) ====================
        [HttpGet]
        public IActionResult DownloadReports()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // ==================== PROFILE MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserName = user.Name;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserPhone = user.Phone;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string name, string phone)
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            // Update user details
            user.Name = name;
            user.Phone = phone;

            await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

            // Update session
            HttpContext.Session.SetString("UserName", name);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ==================== SAFETY TIPS (Placeholder) ====================
        [HttpGet]
        public IActionResult SafetyTips()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // ==================== CREATE EMERGENCY INCIDENT (SOS) ====================
        [HttpGet]
        public async Task<IActionResult> CreateEmergency()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");

            var emergencyIncident = new Incident
            {
                Title = "🚨 EMERGENCY SOS - Immediate Assistance Required",
                Description = $"Emergency SOS alert raised by citizen {userName}. Immediate assistance required at their location.",
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

            TempData["SuccessMessage"] = "🚨 Emergency alert sent! Authorities have been notified.";
            return RedirectToAction("Index");
        }
        // ==================== MAP VIEW - Get Incidents with Coordinates ====================
        [HttpGet]
        public async Task<IActionResult> GetIncidentsForMap()
        {
            if (!IsCitizen())
            {
                return Unauthorized();
            }

            var userId = HttpContext.Session.GetString("UserId");

            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .ToListAsync();

            var mapData = incidents.Select(i => new {
                i.Id,
                i.Title,
                i.Location,
                i.Severity,
                i.Latitude,
                i.Longitude
            }).Where(i => i.Latitude != null && i.Longitude != null);

            return Json(mapData);
        }
    }
}