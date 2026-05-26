using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }          // Auto-generated, nullable

        public string? UserId { get; set; }      // Nullable
        public string? IncidentId { get; set; }  // Nullable
        public string? Title { get; set; }       // Nullable
        public string? Status { get; set; }      // Nullable
        public string? Message { get; set; }     // Nullable

        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}