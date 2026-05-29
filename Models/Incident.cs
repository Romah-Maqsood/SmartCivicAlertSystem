using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartCityPulse.Models
{
    public class Incident
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Basic Fields
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

        [BsonElement("reportedByName")]
        public string ReportedByName { get; set; } = string.Empty;

        [BsonElement("reportedAt")]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("comments")]
        public List<IncidentComment> Comments { get; set; } = new();

        // ========== POLICE DEPARTMENT FIELDS ==========
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

        // ========== FIRE DEPARTMENT FIELDS ==========
        [BsonElement("fireType")]
        public string FireType { get; set; } = string.Empty;

        [BsonElement("fireSource")]
        public string FireSource { get; set; } = string.Empty;

        [BsonElement("fireTrucksDispatched")]
        public int FireTrucksDispatched { get; set; } = 1;

        [BsonElement("firefightersCount")]
        public int FirefightersCount { get; set; } = 5;

        [BsonElement("isEvacuationNeeded")]
        public bool IsEvacuationNeeded { get; set; }

        [BsonElement("fireStatus")]
        public string FireStatus { get; set; } = "Dispatched";

        [BsonElement("estimatedFireSize")]
        public string EstimatedFireSize { get; set; } = "Small";

        // ========== RESCUE DEPARTMENT FIELDS ==========
        [BsonElement("emergencyType")]
        public string EmergencyType { get; set; } = string.Empty;

        [BsonElement("patientCondition")]
        public string PatientCondition { get; set; } = "Stable";

        [BsonElement("ambulancesDispatched")]
        public int AmbulancesDispatched { get; set; } = 1;

        [BsonElement("paramedicsCount")]
        public int ParamedicsCount { get; set; } = 2;

        [BsonElement("destinationHospital")]
        public string DestinationHospital { get; set; } = string.Empty;

        [BsonElement("isOxygenNeeded")]
        public bool IsOxygenNeeded { get; set; }

        [BsonElement("rescueStatus")]
        public string RescueStatus { get; set; } = "Ambulance Dispatched";

        [BsonElement("estimatedArrivalMinutes")]
        public int EstimatedArrivalMinutes { get; set; } = 10;
    }

    public class IncidentComment
    {
        [BsonElement("text")]
        public string Text { get; set; } = string.Empty;

        [BsonElement("author")]
        public string Author { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}