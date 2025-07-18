using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class Setting
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
