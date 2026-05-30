using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // ----- existing fields -----
        public string? UserId { get; set; }          // Citizen’s user ID
        public string? IncidentId { get; set; }      // Related incident ID
        public string? Title { get; set; }
        public string? Status { get; set; }          // e.g., "Read", "Unread" (can be used instead of IsRead)
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        // ----- new fields for role‑based targeting -----
        public string TargetRole { get; set; } = string.Empty;   // "Admin", "Operator", "FireDepartment", etc.
        public string? TargetUserId { get; set; }                // individual citizen
        public string Type { get; set; } = "info";               // info, success, warning, critical
        public string Severity { get; set; } = "low";            // low, medium, high, critical
    }
}