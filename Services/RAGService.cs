using MongoDB.Driver;
using SmartCityPulse.Data;
using SmartCityPulse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCityPulse.Services
{
    public class RAGService
    {
        private readonly MongoDbContext _context;

        public RAGService(MongoDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gathers key metrics from the database to build the prompt context.
        /// </summary>
        public async Task<string> BuildContextAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // Critical open incidents
            var criticalOpen = await _context.Incidents
                .CountDocumentsAsync(i => i.Severity == "Critical" && i.Status != "Resolved");

            // Pending incidents (not resolved)
            var pending = await _context.Incidents
                .CountDocumentsAsync(i => i.Status != "Resolved");

            // Resolved today
            var resolvedToday = await _context.Incidents
                .CountDocumentsAsync(i => i.Status == "Resolved" && i.UpdatedAt >= today);

            // Total incidents
            var totalIncidents = await _context.Incidents
                .CountDocumentsAsync(FilterDefinition<Incident>.Empty);

            // Counts by department
            var deptCounts = await _context.Incidents
                .Aggregate()
                .Group(i => i.Department, g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            // Recent unread notifications (limit 3)
            var unreadNotifications = await _context.Notifications
                .Find(n => !n.IsRead)
                .SortByDescending(n => n.CreatedAt)
                .Limit(3)
                .Project(n => new { n.Title, n.Message })
                .ToListAsync();

            var context = $@"
Current system state as of {now:yyyy-MM-dd HH:mm} UTC:
- Critical open incidents: {criticalOpen}
- Pending incidents (not resolved): {pending}
- Resolved today: {resolvedToday}
- Total incidents ever: {totalIncidents}
- Department breakdown: {string.Join(", ", deptCounts.Select(d => $"{d.Department}: {d.Count}"))}
- Latest unread notifications: {string.Join(" | ", unreadNotifications.Select(n => $"[{n.Title}] {n.Message}"))}
";

            return context;
        }
    }
}