using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveType
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Name { get; set; }
    }
}
