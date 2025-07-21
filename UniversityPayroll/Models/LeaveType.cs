using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveType
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
