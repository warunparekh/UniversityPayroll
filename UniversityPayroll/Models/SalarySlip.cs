using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace UniversityPayroll.Models
{
    public class SalarySlip
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public ObjectId EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Gross { get; set; }
        public decimal Tax { get; set; }
        public decimal Net { get; set; }
        public string FilePath { get; set; }
        public DateTime GeneratedOn { get; set; }
    }
}
