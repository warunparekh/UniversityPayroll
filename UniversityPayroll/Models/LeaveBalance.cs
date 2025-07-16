using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class LeaveBalance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; }

        public int Year { get; set; }
        public Dictionary<string, int> Entitlements { get; set; }
        public Dictionary<string, int> Used { get; set; }
        public Dictionary<string, int> Balance { get; set; }
        public DateTime LastAccrualDate { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
