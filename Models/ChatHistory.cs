using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SmartCityPulse.Models
{
    public class ChatHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null;

        public string UserId { get; set; } = null;     // Admin user ID
        public string Role { get; set; } = null;            // "user" or "bot"
        public string Message { get; set; } = null;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}