using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using SmartCityPulse.Hubs;
using SmartCityPulse.Services;                // ✅ added

namespace SmartCityPulse.Controllers
{
    public class OperatorController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OperatorController(MongoDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private bool IsOperatorLoggedIn() =>
            HttpContext.Session.GetString("UserRole") == "Operator";

        private string GetOperatorDepartment() =>
            HttpContext.Session.GetString("UserDepartment") ?? "";

        private string GetOperatorName() =>
            HttpContext.Session.GetString("UserName") ?? "Operator";

        private string GenerateFIRNumber()
        {
            var random = new Random().Next(1000, 9999);
            return $"FIR/{DateTime.Now:yyyyMMdd}/{random}";
        }

        // ---------- (old SendNotification helper removed) ----------

        public async Task<IActionResult> Dashboard()
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var operatorName = GetOperatorName();
            var filter = Builders<Incident>.Filter.Eq(i => i.Department, department);
            var incidents = await _context.Incidents.Find(filter).ToListAsync();

            var today = DateTime.UtcNow.Date;
            var newToday = incidents.Count(i => i.ReportedAt.Date == today && i.Status == "Open");
            var inProgress = incidents.Count(i => i.Status == "In Progress");
            var resolved = incidents.Count(i => i.Status == "Resolved");
            var total = incidents.Count;
            var criticalCount = incidents.Count(i => i.Severity == "Critical");
            var highCount = incidents.Count(i => i.Severity == "High");
            var mediumCount = incidents.Count(i => i.Severity == "Medium");
            var lowCount = incidents.Count(i => i.Severity == "Low");
            var openCount = incidents.Count(i => i.Status == "Open");
            var inProgressCount = incidents.Count(i => i.Status == "In Progress");
            var resolvedCount = incidents.Count(i => i.Status == "Resolved");

            var last7Days = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                last7Days.Add(incidents.Count(inc => inc.ReportedAt.Date == date));
            }

            var monthlyData = new Dictionary<string, int>();
            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthName = month.ToString("MMM yyyy");
                monthlyData[monthName] = incidents.Count(inc =>
                    inc.ReportedAt.Year == month.Year && inc.ReportedAt.Month == month.Month);
            }

            ViewBag.OperatorName = operatorName;
            ViewBag.Department = department;
            ViewBag.NewToday = newToday;
            ViewBag.InProgress = inProgress;
            ViewBag.Resolved = resolved;
            ViewBag.Total = total;
            ViewBag.CriticalCount = criticalCount;
            ViewBag.HighCount = highCount;
            ViewBag.MediumCount = mediumCount;
            ViewBag.LowCount = lowCount;
            ViewBag.OpenCount = openCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.ResolvedCount = resolvedCount;
            ViewBag.Last7Days = JsonConvert.SerializeObject(last7Days);
            ViewBag.MonthlyData = JsonConvert.SerializeObject(monthlyData);

            var viewModel = new OperatorDashboardViewModel
            {
                OperatorName = operatorName,
                Department = department,
                NewToday = newToday,
                InProgress = inProgress,
                Resolved = resolved,
                TotalIncidents = total,
                RecentIncidents = incidents.OrderByDescending(i => i.ReportedAt).Take(10).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Incidents(string? status, string? severity, string? caseType, string? fireType, string? emergencyType)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var filterBuilder = Builders<Incident>.Filter;
            var filter = filterBuilder.Eq(i => i.Department, department);

            if (!string.IsNullOrEmpty(status)) filter &= filterBuilder.Eq(i => i.Status, status);
            if (!string.IsNullOrEmpty(severity)) filter &= filterBuilder.Eq(i => i.Severity, severity);
            if (!string.IsNullOrEmpty(caseType) && department == "Police") filter &= filterBuilder.Eq(i => i.CaseType, caseType);
            if (!string.IsNullOrEmpty(fireType) && department == "Fire") filter &= filterBuilder.Eq(i => i.FireType, fireType);
            if (!string.IsNullOrEmpty(emergencyType) && department == "Rescue") filter &= filterBuilder.Eq(i => i.EmergencyType, emergencyType);

            var incidents = await _context.Incidents.Find(filter)
                .SortByDescending(i => i.ReportedAt).ToListAsync();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = department;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSeverity = severity;
            ViewBag.SelectedCaseType = caseType;
            ViewBag.SelectedFireType = fireType;
            ViewBag.SelectedEmergencyType = emergencyType;

            return View(incidents);
        }

        public async Task<IActionResult> IncidentDetail(string id)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = department;
            return View(incident);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status, string comment)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            var update = Builders<Incident>.Update.Set(i => i.Status, status).Set(i => i.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(comment))
            {
                incident.Comments.Add(new IncidentComment
                {
                    Text = comment,
                    Author = GetOperatorName(),
                    CreatedAt = DateTime.UtcNow
                });
                update = update.Set(i => i.Comments, incident.Comments);
            }

            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);

            // ✅ Notify the citizen who reported the incident
            if (!string.IsNullOrEmpty(incident.ReportedBy))
            {
                await NotificationService.SendAndSave(
                    _context, _hubContext,
                    "Incident Status Updated",
                    $"Your incident '{incident.Title}' is now {status}.",
                    "info", "medium",
                    targetUserId: incident.ReportedBy
                );
            }

            // ✅ If a critical incident was resolved, notify admins
            if (incident.Severity == "Critical" && status == "Resolved")
            {
                await NotificationService.SendAndSave(
                    _context, _hubContext,
                    "Critical Incident Resolved",
                    $"The critical incident '{incident.Title}' has been resolved by {GetOperatorName()} ({GetOperatorDepartment()}).",
                    "success", "high",
                    targetRole: "Admin"
                );
            }

            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInvestigationStatus(string id, string investigationStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            await _context.Incidents.UpdateOneAsync(i => i.Id == id,
                Builders<Incident>.Update.Set(i => i.InvestigationStatus, investigationStatus).Set(i => i.UpdatedAt, DateTime.UtcNow));
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFireStatus(string id, string fireStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            await _context.Incidents.UpdateOneAsync(i => i.Id == id,
                Builders<Incident>.Update.Set(i => i.FireStatus, fireStatus).Set(i => i.UpdatedAt, DateTime.UtcNow));
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRescueStatus(string id, string rescueStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            await _context.Incidents.UpdateOneAsync(i => i.Id == id,
                Builders<Incident>.Update.Set(i => i.RescueStatus, rescueStatus).Set(i => i.UpdatedAt, DateTime.UtcNow));
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult CreateIncident()
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");
            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = GetOperatorDepartment();
            return View(new Incident());
        }

        [HttpPost]
        public async Task<IActionResult> CreateIncident(Incident model)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");

            model.Department = GetOperatorDepartment();
            model.ReportedBy = GetOperatorName();
            model.ReportedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.Status = "Open";
            model.Comments = new List<IncidentComment>
            {
                new IncidentComment
                {
                    Text = $"Incident reported by {GetOperatorName()}",
                    Author = "System",
                    CreatedAt = DateTime.UtcNow
                }
            };

            if (model.Department == "Police") { model.FIRNumber = GenerateFIRNumber(); model.InvestigationStatus = "FIR Registered"; }
            else if (model.Department == "Fire") { model.FireStatus = "Dispatched"; }
            else if (model.Department == "Rescue") { model.RescueStatus = "Ambulance Dispatched"; }

            try
            {
                await _context.Incidents.InsertOneAsync(model);

                // ✅ Notify department operators (group name without spaces)
                string deptGroup = model.Department.Replace(" ", "");
                await NotificationService.SendAndSave(
                    _context, _hubContext,
                    "New Incident in Department",
                    $"New {model.Severity} incident: {model.Title} at {model.Location}.",
                    "info", model.Severity.ToLower(),
                    targetRole: deptGroup
                );

                // ✅ If critical, also notify admins
                if (model.Severity == "Critical")
                {
                    await NotificationService.SendAndSave(
                        _context, _hubContext,
                        "New Critical Incident",
                        $"A new critical incident '{model.Title}' has been reported in {model.Department}.",
                        "critical", "high",
                        targetRole: "Admin"
                    );
                }

                TempData["Success"] = "Incident created successfully!";
                return RedirectToAction("Incidents");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.OperatorName = GetOperatorName();
                ViewBag.Department = GetOperatorDepartment();
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(string id, string commentText)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            incident.Comments.Add(new IncidentComment { Text = commentText, Author = GetOperatorName(), CreatedAt = DateTime.UtcNow });
            await _context.Incidents.UpdateOneAsync(i => i.Id == id,
                Builders<Incident>.Update.Set(i => i.Comments, incident.Comments).Set(i => i.UpdatedAt, DateTime.UtcNow));

            // ✅ Notify citizen that a comment was added
            if (!string.IsNullOrEmpty(incident.ReportedBy))
            {
                await NotificationService.SendAndSave(
                    _context, _hubContext,
                    "New Comment on Your Incident",
                    $"A new comment was added to your incident '{incident.Title}' by {GetOperatorName()}.",
                    "info", "low",
                    targetUserId: incident.ReportedBy
                );
            }

            TempData["Success"] = "Comment added successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        // ==================== GET NOTIFICATIONS (NEW) ====================
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsOperatorLoggedIn()) return Unauthorized();
            var department = GetOperatorDepartment().Replace(" ", ""); // e.g., "FireDepartment"
            var notifications = await _context.Notifications
                .Find(n => n.TargetRole == department || n.TargetRole == "Operator" || n.TargetRole == "")
                .SortByDescending(n => n.CreatedAt)
                .Limit(50)
                .ToListAsync();
            return Json(notifications);
        }

        // ==================== (Ajax helpers kept as is) ====================
        [HttpGet]
        public async Task<IActionResult> GetNewIncidentsCount()
        {
            if (!IsOperatorLoggedIn()) return Json(new { count = 0 });
            var dept = GetOperatorDepartment();
            var today = DateTime.UtcNow.Date;
            var count = await _context.Incidents.CountDocumentsAsync(i => i.Department == dept && i.ReportedAt.Date == today && i.Status == "Open");
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            if (!IsOperatorLoggedIn()) return Json(new { count = 0 });
            var dept = GetOperatorDepartment();
            var today = DateTime.UtcNow.Date;
            var count = await _context.Incidents.CountDocumentsAsync(i => i.Department == dept && i.ReportedAt.Date == today && i.Status == "Open");
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetNewIncidentsSince(DateTime lastCheck)
        {
            if (!IsOperatorLoggedIn()) return Json(new { incidents = new List<object>() });
            var dept = GetOperatorDepartment();
            var newIncidents = await _context.Incidents
                .Find(i => i.Department == dept && i.ReportedAt > lastCheck)
                .SortByDescending(i => i.ReportedAt).ToListAsync();
            var result = newIncidents.Select(i => new { i.Id, i.Title, i.Location, i.Severity, i.Department, i.ReportedAt });
            return Json(new { incidents = result });
        }
    }
}