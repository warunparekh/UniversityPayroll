using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class Designation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}