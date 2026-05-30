using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;
using SmartCityPulse.Models;

namespace SmartCityPulse.Services
{
    public class UnassignedIncidentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UnassignedIncidentService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                    var threshold = DateTime.UtcNow.AddMinutes(-10);

                    // Sirf unassigned critical ya SOS incidents jo 10 min se zyada open hain
                    var criticalUnassigned = await db.Incidents
                        .Find(i => (i.Severity == "Critical" || i.Title.Contains("SOS")) &&
                                    i.Status == "Open" &&
                                    i.ReportedAt < threshold)
                        .ToListAsync();

                    foreach (var inc in criticalUnassigned)
                    {
                        // 🔁 CHECK: Kya is incident ke liye pehle se notification hai?
                        var alreadyExists = await db.Notifications
                            .Find(n => n.IncidentId == inc.Id &&
                                       n.Title == "Unassigned Critical Incident")
                            .AnyAsync();

                        if (!alreadyExists)
                        {
                            // 📝 Clear message with incident details
                            string message = $"⚠️ Critical incident **'{inc.Title}'** (ID: {inc.Id})" +
                                             (string.IsNullOrEmpty(inc.Location) ? "" : $" at **{inc.Location}**") +
                                             " has been **unassigned for more than 10 minutes**.\n" +
                                             "Please assign an operator immediately.";

                            await NotificationService.SendAndSave(
                                db, hubContext,
                                "Unassigned Critical Incident",      // Title
                                message,                              // Detailed message
                                "critical",                          // Type
                                "high",                              // Severity
                                targetRole: "Admin",
                                incidentId: inc.Id
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"UnassignedIncidentService error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}