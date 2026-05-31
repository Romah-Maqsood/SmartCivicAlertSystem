using Microsoft.AspNetCore.Mvc;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using SmartCityPulse.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        private string GetOperatorEmail() =>
            HttpContext.Session.GetString("UserEmail") ?? "";

        private string GenerateFIRNumber()
        {
            var random = new Random().Next(1000, 9999);
            return $"FIR/{DateTime.Now:yyyyMMdd}/{random}";
        }

        private FilterDefinition<Incident> GetDepartmentFilter()
        {
            var department = GetOperatorDepartment();
            var possible = new List<string> { department };

            if (department.EndsWith(" Department", StringComparison.OrdinalIgnoreCase))
            {
                string shortName = department.Substring(0, department.LastIndexOf(" Department"));
                possible.Add(shortName);
            }
            else
            {
                string fullName = department + " Department";
                possible.Add(fullName);
            }

            possible.Add("Unassigned");

            return Builders<Incident>.Filter.In(i => i.Department, possible);
        }

        // ==================== DEPARTMENT-SPECIFIC PRIORITY SCORE ====================
        private int GetPriorityScore(Incident incident)
        {
            string department = GetOperatorDepartment();
            int score = 3; // Default Medium

            // Severity based scoring
            if (incident.Severity == "Critical") score = 1;
            else if (incident.Severity == "High") score = 2;
            else if (incident.Severity == "Medium") score = 3;
            else if (incident.Severity == "Low") score = 4;

            string text = (incident.Title + " " + incident.Description).ToLower();

            // ========== POLICE DEPARTMENT PRIORITY ==========
            if (department.Contains("Police"))
            {
                // Priority 1 - Critical for Police
                if (text.Contains("murder") || text.Contains("killing") || text.Contains("shooting") ||
                    text.Contains("hostage") || text.Contains("terrorist") || text.Contains("bomb") ||
                    text.Contains("explosion") || text.Contains("kidnap") || text.Contains("emergency") ||
                    text.Contains("riot") || text.Contains("violence"))
                {
                    score = 1;
                }
                // Priority 2 - High for Police
                else if (text.Contains("robbery") || text.Contains("armed") || text.Contains("weapon") ||
                         text.Contains("assault") || text.Contains("attack") || text.Contains("fight") ||
                         text.Contains("snatching") || text.Contains("theft with weapon"))
                {
                    if (score > 2) score = 2;
                }
                // Priority 3 - Medium for Police
                else if (text.Contains("theft") || text.Contains("stolen") || text.Contains("burglary") ||
                         text.Contains("missing") || text.Contains("fraud") || text.Contains("scam") ||
                         text.Contains("accident") || text.Contains("vehicle theft") || text.Contains("mobile snatching"))
                {
                    if (score > 3) score = 3;
                }
                // Priority 4 - Low for Police
                else
                {
                    if (score > 4) score = 4;
                }
            }

            // ========== FIRE DEPARTMENT PRIORITY ==========
            else if (department.Contains("Fire"))
            {
                // Priority 1 - Critical for Fire
                if (text.Contains("building fire") || text.Contains("factory fire") || text.Contains("chemical fire") ||
                    text.Contains("gas explosion") || text.Contains("cylinder blast") || text.Contains("major fire") ||
                    text.Contains("industrial fire") || text.Contains("fire with people trapped") ||
                    incident.Severity == "Critical")
                {
                    score = 1;
                }
                // Priority 2 - High for Fire
                else if (text.Contains("house fire") || text.Contains("apartment fire") || text.Contains("electrical fire") ||
                         text.Contains("gas leak") || text.Contains("forest fire") || text.Contains("vehicle fire") ||
                         text.Contains("car fire") || text.Contains("fire spreading") || text.Contains("smoke"))
                {
                    if (score > 2) score = 2;
                }
                // Priority 3 - Medium for Fire
                else if (text.Contains("small fire") || text.Contains("kitchen fire") || text.Contains("trash fire") ||
                         text.Contains("fire alarm") || text.Contains("smoke detected"))
                {
                    if (score > 3) score = 3;
                }
                // Priority 4 - Low for Fire
                else
                {
                    if (score > 4) score = 4;
                }
            }

            // ========== RESCUE DEPARTMENT PRIORITY ==========
            else if (department.Contains("Rescue"))
            {
                // Priority 1 - Critical for Rescue
                if (text.Contains("cardiac arrest") || text.Contains("heart attack") || text.Contains("stroke") ||
                    text.Contains("unconscious") || text.Contains("not breathing") || text.Contains("severe bleeding") ||
                    text.Contains("major accident") || text.Contains("multiple injuries") || text.Contains("drowning") ||
                    text.Contains("life threatening") || incident.Severity == "Critical")
                {
                    score = 1;
                }
                // Priority 2 - High for Rescue
                else if (text.Contains("accident injury") || text.Contains("serious injury") || text.Contains("burns") ||
                         text.Contains("fracture") || text.Contains("broken bone") || text.Contains("head injury") ||
                         text.Contains("fall from height") || text.Contains("road accident") || text.Contains("car crash"))
                {
                    if (score > 2) score = 2;
                }
                // Priority 3 - Medium for Rescue
                else if (text.Contains("minor injury") || text.Contains("first aid") || text.Contains("childbirth") ||
                         text.Contains("pregnancy") || text.Contains("sick person") || text.Contains("fever") ||
                         text.Contains("difficulty breathing"))
                {
                    if (score > 3) score = 3;
                }
                // Priority 4 - Low for Rescue
                else
                {
                    if (score > 4) score = 4;
                }
            }

            return score;
        }

        // ==================== DASHBOARD ====================
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var operatorName = GetOperatorName();
            var operatorEmail = GetOperatorEmail();
            var department = GetOperatorDepartment();
            var filter = GetDepartmentFilter();

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
            ViewBag.OperatorEmail = operatorEmail;
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

        // ==================== NOTIFICATIONS PAGE ====================
        public IActionResult Notifications()
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.OperatorEmail = GetOperatorEmail();
            ViewBag.Department = GetOperatorDepartment();
            return View();
        }

        // ==================== INCIDENTS LIST ====================
        public async Task<IActionResult> Incidents(string? status, string? severity, string? caseType, string? fireType, string? emergencyType, string? priority)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var filterBuilder = Builders<Incident>.Filter;
            var filter = GetDepartmentFilter();

            if (!string.IsNullOrEmpty(status))
                filter &= filterBuilder.Eq(i => i.Status, status);
            if (!string.IsNullOrEmpty(severity))
                filter &= filterBuilder.Eq(i => i.Severity, severity);
            if (!string.IsNullOrEmpty(caseType) && department.Contains("Police"))
                filter &= filterBuilder.Eq(i => i.CaseType, caseType);
            if (!string.IsNullOrEmpty(fireType) && department.Contains("Fire"))
                filter &= filterBuilder.Eq(i => i.FireType, fireType);
            if (!string.IsNullOrEmpty(emergencyType) && department.Contains("Rescue"))
                filter &= filterBuilder.Eq(i => i.EmergencyType, emergencyType);

            var incidents = await _context.Incidents.Find(filter).ToListAsync();

            // Apply priority filter if selected
            if (!string.IsNullOrEmpty(priority))
            {
                int priorityValue = int.Parse(priority);
                incidents = incidents.Where(i => GetPriorityScore(i) == priorityValue).ToList();
            }

            // Sort by priority (1 = highest) then by date
            incidents = incidents.OrderBy(i => GetPriorityScore(i)).ThenByDescending(i => i.ReportedAt).ToList();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.OperatorEmail = GetOperatorEmail();
            ViewBag.Department = department;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSeverity = severity;
            ViewBag.SelectedPriority = priority;
            ViewBag.SelectedCaseType = caseType;
            ViewBag.SelectedFireType = fireType;
            ViewBag.SelectedEmergencyType = emergencyType;

            return View(incidents);
        }

        // ==================== INCIDENT DETAIL ====================
        public async Task<IActionResult> IncidentDetail(string id)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var department = GetOperatorDepartment();
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return NotFound();

            ViewBag.OperatorName = GetOperatorName();
            ViewBag.Department = department;
            return View(incident);
        }

        // ==================== UPDATE STATUS ====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status, string comment)
        {
            if (!IsOperatorLoggedIn())
                return RedirectToAction("Login", "Account");

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
                return NotFound();

            var update = Builders<Incident>.Update
                .Set(i => i.Status, status)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(comment))
            {
                var newComment = new IncidentComment
                {
                    Text = comment,
                    Author = GetOperatorName(),
                    CreatedAt = DateTime.UtcNow
                };
                incident.Comments.Add(newComment);
                update = update.Set(i => i.Comments, incident.Comments);
            }

            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);

            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        // ==================== DEPARTMENT-SPECIFIC UPDATES ====================
        [HttpPost]
        public async Task<IActionResult> UpdateInvestigationStatus(string id, string investigationStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            var update = Builders<Incident>.Update.Set(i => i.InvestigationStatus, investigationStatus).Set(i => i.UpdatedAt, DateTime.UtcNow);
            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFireStatus(string id, string fireStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            var update = Builders<Incident>.Update.Set(i => i.FireStatus, fireStatus).Set(i => i.UpdatedAt, DateTime.UtcNow);
            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRescueStatus(string id, string rescueStatus)
        {
            if (!IsOperatorLoggedIn()) return Json(new { success = false });
            var update = Builders<Incident>.Update.Set(i => i.RescueStatus, rescueStatus).Set(i => i.UpdatedAt, DateTime.UtcNow);
            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);
            return Json(new { success = true });
        }

        // ==================== CREATE INCIDENT ====================
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

            if (model.Department == "Police Department")
            {
                model.FIRNumber = GenerateFIRNumber();
                model.InvestigationStatus = "FIR Registered";
            }
            else if (model.Department == "Fire Department")
            {
                model.FireStatus = "Dispatched";
            }
            else if (model.Department == "Rescue Department")
            {
                model.RescueStatus = "Ambulance Dispatched";
            }

            try
            {
                await _context.Incidents.InsertOneAsync(model);
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

        // ==================== ADD COMMENT ====================
        [HttpPost]
        public async Task<IActionResult> AddComment(string id, string commentText)
        {
            if (!IsOperatorLoggedIn()) return RedirectToAction("Login", "Account");
            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null) return NotFound();

            var newComment = new IncidentComment
            {
                Text = commentText,
                Author = GetOperatorName(),
                CreatedAt = DateTime.UtcNow
            };
            incident.Comments.Add(newComment);

            var update = Builders<Incident>.Update.Set(i => i.Comments, incident.Comments).Set(i => i.UpdatedAt, DateTime.UtcNow);
            await _context.Incidents.UpdateOneAsync(i => i.Id == id, update);

            TempData["Success"] = "Comment added successfully!";
            return RedirectToAction("IncidentDetail", new { id });
        }

        // ==================== NOTIFICATION METHODS ====================

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsOperatorLoggedIn()) return Unauthorized();

            var department = GetOperatorDepartment();
            var deptGroup = department.Replace(" ", "");

            var notifications = await _context.Notifications
                .Find(n => (n.TargetRole == deptGroup || n.TargetRole == "Operator" || n.TargetRole == ""))
                .SortByDescending(n => n.CreatedAt)
                .Limit(50)
                .ToListAsync();

            return Json(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            if (!IsOperatorLoggedIn()) return Json(new { count = 0 });

            var department = GetOperatorDepartment();
            var deptGroup = department.Replace(" ", "");

            var count = await _context.Notifications
                .CountDocumentsAsync(n => (n.TargetRole == deptGroup || n.TargetRole == "Operator" || n.TargetRole == "") && !n.IsRead);

            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            await _context.Notifications.UpdateOneAsync(
                n => n.Id == id,
                Builders<Notification>.Update.Set(n => n.IsRead, true));

            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsOperatorLoggedIn()) return Unauthorized();

            var department = GetOperatorDepartment();
            var deptGroup = department.Replace(" ", "");

            await _context.Notifications.UpdateManyAsync(
                n => (n.TargetRole == deptGroup || n.TargetRole == "Operator" || n.TargetRole == "") && !n.IsRead,
                Builders<Notification>.Update.Set(n => n.IsRead, true));

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetNewIncidentsSince(DateTime lastCheck)
        {
            if (!IsOperatorLoggedIn()) return Json(new { incidents = new List<object>() });

            var filter = GetDepartmentFilter() &
                         Builders<Incident>.Filter.Gt(i => i.ReportedAt, lastCheck);

            var newIncidents = await _context.Incidents.Find(filter)
                .SortByDescending(i => i.ReportedAt).ToListAsync();

            var result = newIncidents.Select(i => new {
                id = i.Id,
                title = i.Title,
                location = i.Location,
                severity = i.Severity,
                department = i.Department,
                reportedAt = i.ReportedAt
            });
            return Json(new { incidents = result });
        }

        // ==================== DELETE NOTIFICATION ====================
        [HttpPost]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            if (!IsOperatorLoggedIn()) return Unauthorized();

            if (string.IsNullOrEmpty(id)) return BadRequest();

            var result = await _context.Notifications.DeleteOneAsync(n => n.Id == id);

            if (result.DeletedCount > 0)
            {
                return Ok(new { success = true });
            }

            return Ok(new { success = false, message = "Notification not found" });
        }

        // ==================== DELETE ALL READ NOTIFICATIONS ====================
        [HttpPost]
        public async Task<IActionResult> DeleteAllReadNotifications()
        {
            if (!IsOperatorLoggedIn()) return Unauthorized();

            var department = GetOperatorDepartment();
            var deptGroup = department.Replace(" ", "");

            var result = await _context.Notifications.DeleteManyAsync(n =>
                (n.TargetRole == deptGroup || n.TargetRole == "Operator" || n.TargetRole == "") && n.IsRead == true);

            return Ok(new { success = true, deletedCount = result.DeletedCount });
        }
    }
}