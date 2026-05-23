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

            // Get citizen's own incidents
            var myIncidents = await _context.Incidents
                .Find(i => i.ReportedBy == userId)
                .SortByDescending(i => i.ReportedAt)
                .Limit(5)
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

            // ✅ FIXED: Weekly data for chart (explicit int conversion)
            var weeklyData = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _context.Incidents
                    .CountAsync(inc => inc.ReportedBy == userId && inc.ReportedAt.Date == date);
                weeklyData.Add((int)count);  // ✅ Explicit cast to int
            }

            ViewBag.UserName = userName;
            ViewBag.TotalIncidents = totalIncidents;
            ViewBag.ResolvedIncidents = resolvedIncidents;
            ViewBag.PendingIncidents = pendingIncidents;
            ViewBag.CriticalIncidents = criticalIncidents;
            ViewBag.MyIncidents = myIncidents;
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

        // ==================== INCIDENT DETAILS ====================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (!IsCitizen())
            {
                return RedirectToAction("Login", "Account");
            }

            var incident = await _context.Incidents.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (incident == null)
            {
                return NotFound();
            }

            return View(incident);
        }
    }
}