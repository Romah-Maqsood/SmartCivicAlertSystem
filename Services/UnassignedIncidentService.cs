using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;                              // ← required for Find extension
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;

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
                    var criticalUnassigned = await db.Incidents
                        .Find(i => (i.Severity == "Critical" || i.Title.Contains("SOS")) &&
                                    i.Status == "Open" &&
                                    i.ReportedAt < threshold)
                        .ToListAsync();

                    foreach (var inc in criticalUnassigned)
                    {
                        await NotificationService.SendAndSave(
                            db, hubContext,
                            "Unassigned Critical Incident",
                            $"Critical incident '{inc.Title}' (ID: {inc.Id}) has been open for more than 10 minutes and is not yet assigned.",
                            "critical", "high",
                            targetRole: "Admin"
                        );
                    }
                }
                catch (Exception)
                {
                    // Optionally log the error
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}