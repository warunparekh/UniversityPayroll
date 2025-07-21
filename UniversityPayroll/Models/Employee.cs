using AspNetCore.Identity.Mongo.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? TaxSlabId { get; set; }
        public string EmployeeCode { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public decimal BaseSalary { get; set; }
        public string Status { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string IdentityUserId { get; set; }

        
    }
}
