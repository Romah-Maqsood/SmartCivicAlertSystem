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
                .CountDocumentsAsync(i => i.ReportedBy == userId);

            var resolvedIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Resolved");

            var pendingIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status != "Resolved");

            var criticalIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Critical");

            // Weekly data for chart (last 7 days)
            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountDocumentsAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);
            }

            // Severity counts for charts
            var highIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "High");
            var mediumIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Medium");
            var lowIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Low");

            // Status counts for charts
            var openIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Open");
            var inProgressIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "In Progress");

            ViewBag.UserName = userName;
            ViewBag.UserEmail = userEmail;
            ViewBag.TotalIncidents = (int)totalIncidents;
            ViewBag.ResolvedIncidents = (int)resolvedIncidents;
            ViewBag.PendingIncidents = (int)pendingIncidents;
            ViewBag.CriticalIncidents = (int)criticalIncidents;
            ViewBag.HighIncidents = (int)highIncidents;
            ViewBag.MediumIncidents = (int)mediumIncidents;
            ViewBag.LowIncidents = (int)lowIncidents;
            ViewBag.OpenIncidents = (int)openIncidents;
            ViewBag.InProgressIncidents = (int)inProgressIncidents;
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

            if (incident.ReportedBy != userId)
            {
                return Unauthorized();
            }

            return View(incident);
        }

        // ==================== MAP VIEW ====================
        [HttpGet]
        public IActionResult MapView()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
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

            var mapData = incidents
                .Where(i => i.Latitude.HasValue && i.Longitude.HasValue)
                .Select(i => new {
                    i.Id,
                    i.Title,
                    i.Location,
                    i.Severity,
                    Latitude = i.Latitude.Value,
                    Longitude = i.Longitude.Value
                });

            return Json(mapData);
        }

        // ==================== NOTIFICATIONS ====================
        [HttpGet]
        public IActionResult Notifications()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsCitizen()) return Unauthorized();

            var userId = HttpContext.Session.GetString("UserId");

            // Get incidents that have status changes
            var notifications = await _context.Incidents
                .Find(i => i.ReportedBy == userId && (i.Status != "Open" || i.UpdatedAt > DateTime.UtcNow.AddDays(-1)))
                .SortByDescending(i => i.UpdatedAt)
                .Limit(20)
                .ToListAsync();

            var result = notifications.Select(i => new {
                i.Id,
                i.Title,
                i.Status,
                UpdatedAt = i.UpdatedAt,
                Message = GetStatusMessage(i.Status, i.Title)
            });

            return Json(result);
        }

        private string GetStatusMessage(string status, string title)
        {
            switch (status)
            {
                case "In Progress":
                    return $"Your incident '{title}' is now In Progress. The concerned department is working on it.";
                case "Resolved":
                    return $"Great news! Your incident '{title}' has been resolved.";
                case "Assigned":
                    return $"Your incident '{title}' has been assigned to the appropriate department.";
                default:
                    return $"Status updated to {status} for your incident '{title}'.";
            }
        }

        // ==================== DOWNLOAD REPORTS ====================
        [HttpGet]
        public IActionResult DownloadReports()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPDF()
        {
            if (!IsCitizen()) return Unauthorized();

            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents.Find(i => i.ReportedBy == userId).ToListAsync();

            var html = @"<html><head><style>
                body{font-family:Arial; margin:20px;}
                h1{color:#333;}
                table{border-collapse:collapse;width:100%;margin-top:20px;}
                th,td{border:1px solid #ddd;padding:8px;text-align:left;}
                th{background-color:#f2f2f2;}
                </style></head><body>
                <h1>My Incident Reports</h1>
                <table>
                    <thead>
                        <tr><th>ID</th><th>Title</th><th>Location</th><th>Severity</th><th>Status</th><th>Date</th></thead>
                    <tbody>";

            foreach (var i in incidents)
            {
                html += $"<tr><td>{i.Id}</td><td>{i.Title}</td><td>{i.Location}</td><td>{i.Severity}</td><td>{i.Status}</td><td>{i.ReportedAt:yyyy-MM-dd}</td></tr>";
            }

            html += @"</tbody>
                </table>
                </body></html>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/pdf", $"IncidentReports_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCSV()
        {
            if (!IsCitizen()) return Unauthorized();

            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents.Find(i => i.ReportedBy == userId).ToListAsync();

            var csv = "ID,Title,Location,Severity,Status,ReportedAt\n";
            foreach (var i in incidents)
            {
                csv += $"{i.Id},{i.Title},{i.Location},{i.Severity},{i.Status},{i.ReportedAt:yyyy-MM-dd}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"IncidentReports_{DateTime.Now:yyyyMMdd}.csv");
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

            user.Name = name;
            user.Phone = phone;

            await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

            HttpContext.Session.SetString("UserName", name);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ==================== SAFETY TIPS ====================
        [HttpGet]
        public IActionResult SafetyTips()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // ==================== STATISTICS ====================
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");

            // Weekly data for chart (last 7 days)
            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountDocumentsAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);
            }

            // Severity counts
            var criticalCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Critical");
            var highCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "High");
            var mediumCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Medium");
            var lowCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Low");

            // Status counts
            var openCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Open");
            var inProgressCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "In Progress");
            var resolvedCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Resolved");

            // Total incidents
            var totalIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId);

            ViewBag.WeeklyData = weeklyData;
            ViewBag.CriticalIncidents = (int)criticalCount;
            ViewBag.HighIncidents = (int)highCount;
            ViewBag.MediumIncidents = (int)mediumCount;
            ViewBag.LowIncidents = (int)lowCount;
            ViewBag.OpenIncidents = (int)openCount;
            ViewBag.InProgressIncidents = (int)inProgressCount;
            ViewBag.ResolvedIncidents = (int)resolvedCount;
            ViewBag.TotalIncidents = (int)totalIncidents;

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
                Title = "EMERGENCY SOS - Immediate Assistance Required",
                Description = $"Emergency SOS alert raised by citizen {userName}. Immediate assistance required.",
                Location = "Emergency Location",
                Severity = "Critical",
                Status = "Open",
                ReportedBy = userId,
                ReportedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Department = "Emergency Response",
                Comments = new List<IncidentComment>()
            };

            await _context.Incidents.InsertOneAsync(emergencyIncident);

            TempData["SuccessMessage"] = "Emergency alert sent! Authorities have been notified.";
            return RedirectToAction("Index");
        }

        // ==================== MARK NOTIFICATION AS READ ====================
        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            await _context.Notifications.UpdateOneAsync(
                n => n.Id == id,
                Builders<Notification>.Update.Set(n => n.IsRead, true)
            );
            return Ok();
        }
        [HttpGet]
        public IActionResult Reports()
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetString("UserId");

            // Get counts for stats
            var totalIncidents = _context.Incidents.CountDocuments(i => i.ReportedBy == userId);
            var resolvedIncidents = _context.Incidents.CountDocuments(i => i.ReportedBy == userId && i.Status == "Resolved");
            var pendingIncidents = _context.Incidents.CountDocuments(i => i.ReportedBy == userId && i.Status != "Resolved");

            ViewBag.TotalIncidents = (int)totalIncidents;
            ViewBag.ResolvedIncidents = (int)resolvedIncidents;
            ViewBag.PendingIncidents = (int)pendingIncidents;

            return View();
        }

    }
}