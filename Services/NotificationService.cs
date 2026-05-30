using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using SmartCityPulse.Data;
using SmartCityPulse.Hubs;
using SmartCityPulse.Models;

namespace SmartCityPulse.Services
{
    public static class NotificationService
    {
        public static async Task SendAndSave(
            MongoDbContext db,
            IHubContext<NotificationHub> hubContext,
            string title,
            string message,
            string type = "info",
            string severity = "low",
            string? targetRole = null,
            string? targetUserId = null,
            string? incidentId = null)
        {
            // 1. Save to database
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Severity = severity,
                TargetRole = targetRole ?? "",
                TargetUserId = targetUserId,
                IncidentId = incidentId,
                CreatedAt = DateTime.UtcNow
            };

            await db.Notifications.InsertOneAsync(notification);

            // 2. Send via SignalR
            var payload = new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                severity = notification.Severity,
                time = "Just now",
                read = false
            };

            if (!string.IsNullOrEmpty(targetRole))
                await hubContext.Clients.Group(targetRole).SendAsync("ReceiveNotification", payload);
            else if (!string.IsNullOrEmpty(targetUserId))
                await hubContext.Clients.Group(targetUserId).SendAsync("ReceiveNotification", payload);
        }
    }
}