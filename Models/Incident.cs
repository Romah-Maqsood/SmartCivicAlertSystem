using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Incident
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("severity")]
        public string Severity { get; set; } = "Medium";

        [BsonElement("status")]
        public string Status { get; set; } = "Open";

        [BsonElement("department")]
        public string Department { get; set; } = string.Empty;

        [BsonElement("reportedBy")]
        public string ReportedBy { get; set; } = string.Empty;

        [BsonElement("reportedAt")]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("comments")]
        public List<IncidentComment> Comments { get; set; } = new();

        // Police Specific Fields
        [BsonElement("firNumber")]
        public string FIRNumber { get; set; } = string.Empty;

        [BsonElement("caseType")]
        public string CaseType { get; set; } = string.Empty;

        [BsonElement("complainantName")]
        public string ComplainantName { get; set; } = string.Empty;

        [BsonElement("complainantPhone")]
        public string ComplainantPhone { get; set; } = string.Empty;

        [BsonElement("complainantCNIC")]
        public string ComplainantCNIC { get; set; } = string.Empty;

        [BsonElement("suspectName")]
        public string SuspectName { get; set; } = string.Empty;

        [BsonElement("suspectDescription")]
        public string SuspectDescription { get; set; } = string.Empty;

        [BsonElement("vehicleNumber")]
        public string VehicleNumber { get; set; } = string.Empty;

        [BsonElement("investigationStatus")]
        public string InvestigationStatus { get; set; } = "FIR Registered";

        [BsonElement("assignedOfficer")]
        public string AssignedOfficer { get; set; } = string.Empty;
    }

    public class IncidentComment
    {
        public string Text { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}