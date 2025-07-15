using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveApplication
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public string EmployeeId { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public int TotalDays { get; set; }
        public string Reason { get; set; }
        public DateTime AppliedOn { get; set; }
        public DateTime? DecidedOn { get; set; }
        public string Status { get; set; } = "Pending";
        public string Comment { get; set; } = "None";
        public string DecidedBy { get; set; } = "N/A";
    }
}
