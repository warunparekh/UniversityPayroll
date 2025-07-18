using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveEntitlement
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId DesignationId { get; set; }
        public ObjectId LeaveTypeId { get; set; }
        public int AnnualQuota { get; set; }
    }
}
