using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;
using SmartCityPulse.Models;
using SmartCityPulse.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCityPulse.Controllers
{
    public class IncidentController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AIVisionService _aiVisionService;

        public IncidentController(MongoDbContext context, IHubContext<NotificationHub> hubContext, AIVisionService aiVisionService)
        {
            _context = context;
            _hubContext = hubContext;
            _aiVisionService = aiVisionService;
        }

        // ==================== PUBLIC: Report Incident (Citizen/Public) ====================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Incident());
        }

        // ==================== CREATE INCIDENT WITH OPTIONAL IMAGE UPLOAD ====================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Incident incident, IFormFile? ImageFile)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userName = HttpContext.Session.GetString("UserName");

                incident.Id = null;
                incident.ReportedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.Status = "Open";
                incident.Comments = new List<IncidentComment>();

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "incidents");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    incident.ImagePath = $"/uploads/incidents/{uniqueFileName}";
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    incident.ReportedBy = userId;
                    incident.ReportedByName = userName ?? "Citizen";
                }
                else
                {
                    incident.ReportedByName = "Anonymous User";
                }

                if (string.IsNullOrEmpty(incident.Department))
                {
                    incident.Department = GetDepartmentBySeverity(incident.Severity);
                }

                await _context.Incidents.InsertOneAsync(incident);

                // Send notification to operators
                await SendNotificationToOperators(incident);

                return Json(new
                {
                    success = true,
                    message = "Incident reported successfully!",
                    incidentId = incident.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

        // ========== Send Notification to Operators ==========
        private async Task SendNotificationToOperators(Incident incident)
        {
            try
            {
                string deptGroup = "";

                if (incident.Department.Contains("Police"))
                {
                    deptGroup = "Police";
                }
                else if (incident.Department.Contains("Fire"))
                {
                    deptGroup = "Fire";
                }
                else if (incident.Department.Contains("Rescue"))
                {
                    deptGroup = "Rescue";
                }
                else
                {
                    switch (incident.Severity?.ToLower())
                    {
                        case "critical":
                            deptGroup = "EmergencyResponse";
                            break;
                        case "high":
                            deptGroup = "Police";
                            break;
                        case "medium":
                            deptGroup = "Fire";
                            break;
                        default:
                            deptGroup = "GeneralServices";
                            break;
                    }
                }

                // Save notification to database
                var notification = new Notification
                {
                    Id = null,
                    Title = $"New {incident.Severity} Incident: {incident.Title}",
                    Message = $"{incident.Title} at {incident.Location}. Reported by {incident.ReportedByName}.",
                    Type = "incident",
                    Severity = incident.Severity?.ToLower() ?? "medium",
                    TargetRole = deptGroup,
                    TargetUserId = null,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    IncidentId = incident.Id
                };

                await _context.Notifications.InsertOneAsync(notification);

                // Send real-time via SignalR
                await _hubContext.Clients.Group(deptGroup).SendAsync("ReceiveNotification",
                    notification.Title,
                    notification.Message,
                    DateTime.UtcNow,
                    notification.Id);

                await _hubContext.Clients.Group(deptGroup).SendAsync("NewIncident", new
                {
                    id = incident.Id,
                    title = incident.Title,
                    location = incident.Location,
                    severity = incident.Severity,
                    status = incident.Status
                });

                await _hubContext.Clients.Group("Operator").SendAsync("ReceiveNotification",
                    notification.Title,
                    notification.Message,
                    DateTime.UtcNow,
                    notification.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification error: {ex.Message}");
            }
        }

        private string GetDepartmentBySeverity(string severity)
        {
            switch (severity?.ToLower())
            {
                case "critical": return "Emergency Response";
                case "high": return "Police Department";
                case "medium": return "Fire Department";
                case "low": return "General Services";
                default: return "General Services";
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

        // ==================== GET: Incident Details JSON ====================
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
                longitude = incident.Longitude,
                imagePath = incident.ImagePath
            });
        }

        // ==================== GET: My Incidents ====================
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
            await SendNotificationToOperators(emergencyIncident);

            TempData["SuccessMessage"] = "🚨 Emergency alert sent! Authorities have been notified.";

            if (userRole == "Citizen")
                return RedirectToAction("Index", "Citizen");

            return RedirectToAction("Index", "Home");
        }

        // ==================== UPDATE INCIDENT STATUS ====================
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

        // ==================== ADD COMMENT ====================
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

        // ==================== AI IMAGE ANALYSIS ====================
        [HttpPost]
        public async Task<IActionResult> AnalyzeImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return Json(new { success = false, message = "No image uploaded" });
                }

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return Json(new { success = false, message = "Only JPG, PNG, and GIF images are allowed" });
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Image size must be less than 5MB" });
                }

                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var analysis = await _aiVisionService.AnalyzeIncidentImage(imageBytes, image.ContentType);

                return Json(new
                {
                    success = true,
                    title = analysis.Title,
                    description = analysis.Description,
                    severity = analysis.Severity,
                    department = analysis.Department
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Analysis failed: {ex.Message}" });
            }
        }

        // ==================== GET ALL INCIDENTS FOR REPORTS ====================
        [HttpGet]
        public async Task<IActionResult> GetAllIncidentsForReports()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Unauthorized" });

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
                updatedAt = i.UpdatedAt,
                imagePath = i.ImagePath
            });

            return Json(new { success = true, incidents = incidentList });
        }
    }
}