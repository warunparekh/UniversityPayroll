using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class Employee
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string UserId { get; set; }
        public ObjectId DesignationId { get; set; }
    }
}
