using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class PayRun
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime RunDate { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Slips { get; set; }

        public string CreatedBy { get; set; }
        public string Status { get; set; }
    }
}
