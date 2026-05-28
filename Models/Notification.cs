using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
<<<<<<< HEAD
        public string Id { get; set; }

        public string UserId { get; set; }
        public string IncidentId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
=======
        public string? Id { get; set; }          // Auto-generated, nullable

        public string? UserId { get; set; }      // Nullable
        public string? IncidentId { get; set; }  // Nullable
        public string? Title { get; set; }       // Nullable
        public string? Status { get; set; }      // Nullable
        public string? Message { get; set; }     // Nullable

>>>>>>> 331e1f62fcb331caaab5a32e0aefa4e34ba620ab
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}