using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;
using SmartCityPulse.Models;
using SmartCityPulse.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCityPulse.Controllers
{
    public class CitizenController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CitizenController(MongoDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ==================== CHECK IF USER IS CITIZEN ====================
        private bool IsCitizen()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Citizen";
        }

        // ==================== DASHBOARD ====================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var myIncidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt).Limit(5).ToListAsync();
            var allIncidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt).ToListAsync();

            var totalIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId);
            var resolvedIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Resolved");
            var pendingIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Status != "Resolved");
            var criticalIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Critical");

            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountDocumentsAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);
            }

            var highIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "High");
            var mediumIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Medium");
            var lowIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Low");
            var openIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Open");
            var inProgressIncidents = await _context.Incidents.CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "In Progress");

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
                return RedirectToAction("Login", "Account");
            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt).ToListAsync();
            return View(incidents);
        }

        // ==================== INCIDENT DETAILS ====================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            var userId = HttpContext.Session.GetString("UserId");
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();
            if (incident.ReportedBy != userId) return Unauthorized();
            return View(incident);
        }

        // ==================== MAP VIEW ====================
        [HttpGet]
        public IActionResult MapView()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetIncidentsForMap()
        {
            if (!IsCitizen())
                return Unauthorized();
            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents.Find(i => i.ReportedBy == userId).ToListAsync();
            var mapData = incidents
                .Where(i => i.Latitude.HasValue && i.Longitude.HasValue)
                .Select(i => new { i.Id, i.Title, i.Location, i.Severity, Latitude = i.Latitude.Value, Longitude = i.Longitude.Value });
            return Json(mapData);
        }

        // ==================== REPORT INCIDENT (GET) - NO LOGIN REQUIRED ====================
        [HttpGet]
        public IActionResult ReportIncident()
        {
            return View();
        }

        // ==================== REPORT INCIDENT (POST) - ANONYMOUS ALLOWED ====================
        [HttpPost]
        public async Task<IActionResult> ReportIncident([FromBody] Incident model)
        {
            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Description) || string.IsNullOrWhiteSpace(model.Location))
                return Json(new { success = false, message = "Title, Description and Location are required." });

            var deptMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Fire", "Fire Department" },
                { "Fire Department", "Fire Department" },
                { "Police", "Police Department" },
                { "Police Department", "Police Department" },
                { "Rescue", "Rescue Department" },
                { "Rescue Department", "Rescue Department" },
                { "Rescue/Medical", "Rescue Department" },
                { "Emergency Response", "Rescue Department" },
                { "Medical", "Rescue Department" }
            };

            string inputDept = model.Department ?? "";
            model.Department = deptMap.ContainsKey(inputDept) ? deptMap[inputDept] : "Unassigned";

            if (IsCitizen())
            {
                model.ReportedBy = HttpContext.Session.GetString("UserId");
                model.ReportedByName = HttpContext.Session.GetString("UserName") ?? "Citizen";
            }
            else
            {
                model.ReportedBy = "anonymous";
                model.ReportedByName = "Anonymous";
            }

            model.ReportedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.Status = "Open";
            model.Comments = new List<IncidentComment>();

            try
            {
                await _context.Incidents.InsertOneAsync(model);

                string deptGroup = model.Department.Replace(" ", "");
                await NotificationService.SendAndSave(
                    _context, _hubContext,
                    "New Incident Reported",
                    $"New {model.Severity} incident: {model.Title} at {model.Location}.",
                    "info", model.Severity.ToLower(),
                    targetRole: deptGroup
                );

                if (model.Severity == "Critical")
                {
                    await NotificationService.SendAndSave(
                        _context, _hubContext,
                        "New Critical Incident",
                        $"Critical incident '{model.Title}' reported by {model.ReportedByName} in {model.Department}.",
                        "critical", "high",
                        targetRole: "Admin"
                    );
                }

                return Json(new { success = true, message = "Incident reported successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ==================== CREATE EMERGENCY SOS WITH LOCATION ====================
        [HttpPost]
        public async Task<IActionResult> CreateEmergency([FromBody] SOSRequest request)
        {
            if (!IsCitizen())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userPhone = HttpContext.Session.GetString("UserPhone");

            // Validate location
            if (string.IsNullOrWhiteSpace(request?.Location))
            {
                return Json(new { success = false, message = "Emergency location is required" });
            }

            var emergencyIncident = new Incident
            {
                Title = "🚨 EMERGENCY SOS - Immediate Assistance Required",
                Description = $"🚨 EMERGENCY ALERT 🚨\n\nCitizen: {userName}\nContact: {userPhone ?? "Not provided"}\nEmail: {userEmail ?? "Not provided"}\nLocation: {request.Location}\n\n{userName} has requested immediate emergency assistance at the above location.",
                Location = request.Location,
                Severity = "Critical",
                Status = "Open",
                ReportedBy = userId,
                ReportedByName = userName ?? "Citizen",
                ReportedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Department = "Rescue Department",   // ✅ FIXED: was "Rescue", now consistent
                Comments = new List<IncidentComment>()
            };

            await _context.Incidents.InsertOneAsync(emergencyIncident);

            // Send real-time SignalR notification to ALL departments (broadcast)
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveEmergencyAlert",
                    "🚨 EMERGENCY SOS",
                    $"CRITICAL: {userName} has sent an emergency SOS alert from {request.Location}! Immediate action required.",
                    DateTime.Now,
                    emergencyIncident.Id);
            }
            catch { }

            // Send notification to the citizen who raised the SOS
            await NotificationService.SendAndSave(
                _context, _hubContext,
                "Emergency Submitted",
                $"Your emergency SOS alert has been sent. Rescue team is responding to: {request.Location}",
                "success", "high",
                targetUserId: userId
            );

            // Notify the Rescue Department
            // NOTE: If your NotificationHub group is "RescueDepartment" (no space), keep it as is;
            // if it's "Rescue Department", change accordingly.
            await NotificationService.SendAndSave(
                _context, _hubContext,
                "🚨 EMERGENCY SOS - Rescue Required",
                $"CRITICAL: Citizen {userName} has raised an emergency SOS at: {request.Location}. Immediate rescue assistance required.",
                "critical", "high",
                targetRole: "RescueDepartment"   // Adjust if your group name differs
            );

            // Also notify Admin for oversight
            await NotificationService.SendAndSave(
                _context, _hubContext,
                "New Emergency SOS Alert",
                $"Citizen {userName} (ID: {userId}) raised an emergency SOS at {request.Location} on {DateTime.UtcNow:HH:mm}.",
                "critical", "high",
                targetRole: "Admin"
            );

            return Json(new { success = true, message = "🚨 SOS Emergency alert sent successfully! Rescue team has been notified and is responding to your location." });
        }

        // Add this request model class inside CitizenController or at the end
        public class SOSRequest
        {
            public string Location { get; set; } = string.Empty;
        }

        // ==================== NOTIFICATIONS ====================
        [HttpGet]
        public IActionResult Notifications()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsCitizen())
                return Unauthorized();
            var userId = HttpContext.Session.GetString("UserId");
            var notifications = await _context.Incidents
                .Find(i => i.ReportedBy == userId && (i.Status != "Open" || i.UpdatedAt > DateTime.UtcNow.AddDays(-1)))
                .SortByDescending(i => i.UpdatedAt).Limit(20).ToListAsync();

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
                case "In Progress": return $"Your incident '{title}' is now In Progress. The concerned department is working on it.";
                case "Resolved": return $"Great news! Your incident '{title}' has been resolved.";
                case "Assigned": return $"Your incident '{title}' has been assigned to the appropriate department.";
                default: return $"Status updated to {status} for your incident '{title}'.";
            }
        }

        // ==================== DOWNLOAD REPORTS ====================
        [HttpGet]
        public IActionResult DownloadReports()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPDF()
        {
            if (!IsCitizen())
                return Unauthorized();
            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents.Find(i => i.ReportedBy == userId).ToListAsync();

            var html = @"<html><head><style>
                body{font-family:Arial; margin:20px;} h1{color:#333;}
                table{border-collapse:collapse;width:100%;margin-top:20px;}
                th,td{border:1px solid #ddd;padding:8px;text-align:left;} th{background-color:#f2f2f2;}
                </style></head><body><h1>My Incident Reports</h1><table>
                <thead><tr><th>ID</th><th>Title</th><th>Location</th><th>Severity</th><th>Status</th><th>Date</th></tr></thead><tbody>";
            foreach (var i in incidents)
                html += $"<tr><td>{i.Id}</td><td>{i.Title}</td><td>{i.Location}</td><td>{i.Severity}</td><td>{i.Status}</td><td>{i.ReportedAt:yyyy-MM-dd}</td></tr>";
            html += @"</tbody></table></body></html>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "application/pdf", $"IncidentReports_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCSV()
        {
            if (!IsCitizen())
                return Unauthorized();
            var userId = HttpContext.Session.GetString("UserId");
            var incidents = await _context.Incidents.Find(i => i.ReportedBy == userId).ToListAsync();
            var csv = "ID,Title,Location,Severity,Status,ReportedAt\n";
            foreach (var i in incidents)
                csv += $"{i.Id},{i.Title},{i.Location},{i.Severity},{i.Status},{i.ReportedAt:yyyy-MM-dd}\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"IncidentReports_{DateTime.Now:yyyyMMdd}.csv");
        }

        // ==================== PROFILE ====================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            var userId = HttpContext.Session.GetString("UserId");
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return NotFound();
            ViewBag.UserName = user.Name;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserPhone = user.Phone;
            return View();
        }

        // ==================== SAFETY TIPS ====================
        [HttpGet]
        public IActionResult SafetyTips()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");
            return View();
        }

        // ==================== MARK NOTIFICATION AS READ ====================
        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            await _context.Notifications.UpdateOneAsync(
                n => n.Id == id,
                Builders<Notification>.Update.Set(n => n.IsRead, true));
            return Ok();
        }

        // ==================== REPORTS PAGE ====================
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            if (!IsCitizen())
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetString("UserId");

            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            return View(incidents);
        }

        // ==================== AJAX HELPERS ====================
        [HttpGet]
        public async Task<IActionResult> GetIncidentJson(string id)
        {
            if (!IsCitizen())
                return Unauthorized();
            var userId = HttpContext.Session.GetString("UserId");
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();
            if (incident.ReportedBy != userId) return Unauthorized();
            return Json(new
            {
                id = incident.Id,
                title = incident.Title,
                description = incident.Description,
                location = incident.Location,
                severity = incident.Severity,
                status = incident.Status,
                reportedAt = incident.ReportedAt,
                updatedAt = incident.UpdatedAt,
                reportedByName = incident.ReportedByName
            });
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

            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountDocumentsAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);
            }

            var criticalCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Critical");
            var highCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "High");
            var mediumCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Medium");
            var lowCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Severity == "Low");

            var openCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Open");
            var inProgressCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "In Progress");
            var resolvedCount = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Resolved");

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

        // ==================== GET ALL INCIDENTS FOR REPORTS (SINGLE METHOD - FIXED) ====================
        [HttpGet]
        public async Task<IActionResult> GetAllIncidentsForReports()
        {
            if (!IsCitizen())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");

            var incidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .ToListAsync();

            var incidentList = incidents.Select(i => new
            {
                id = i.Id,
                title = i.Title,
                description = i.Description,
                location = i.Location,
                severity = i.Severity,
                status = i.Status,
                reportedAt = i.ReportedAt,
                updatedAt = i.UpdatedAt
            });

            return Json(new { success = true, incidents = incidentList });
        }

        // ==================== GET PROFILE STATISTICS ====================
        [HttpGet]
        public async Task<IActionResult> GetProfileStatistics()
        {
            if (!IsCitizen())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");

            var totalIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId);

            var resolvedIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status == "Resolved");

            var activeIncidents = await _context.Incidents
                .CountDocumentsAsync(i => i.ReportedBy == userId && i.Status != "Resolved");

            return Json(new
            {
                success = true,
                totalIncidents = (int)totalIncidents,
                resolvedIncidents = (int)resolvedIncidents,
                activeIncidents = (int)activeIncidents
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            if (!IsCitizen())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetString("UserId");
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null)
                return Json(new { success = false, message = "User not found" });

            // Update only name and phone (email stays same)
            user.Name = model.Name;
            user.Phone = model.Phone;

            await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

            // Update session
            HttpContext.Session.SetString("UserName", model.Name);

            return Json(new { success = true, message = "Profile updated successfully!" });
        }

        // Add this model class
        public class UpdateProfileModel
        {
            public string Name { get; set; }
            public string Phone { get; set; }
        }
    }
}