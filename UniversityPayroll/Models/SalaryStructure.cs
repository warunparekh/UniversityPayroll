using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace UniversityPayroll.Models
{
    public class Allowance
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }

    public class Deduction
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }

    public class SalaryStructure
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public ObjectId DesignationId { get; set; }

        public decimal Basic { get; set; }

        public List<Allowance> Allowances { get; set; } = new();

        public List<Deduction> Deductions { get; set; } = new();
    }
}
