using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveBalance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? EmployeeId { get; set; }

        public int Year { get; set; }
        public Dictionary<string, decimal> Entitlements { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> Used { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> Balance { get; set; } = new Dictionary<string, decimal>();
        public DateTime LastAccrualDate { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}