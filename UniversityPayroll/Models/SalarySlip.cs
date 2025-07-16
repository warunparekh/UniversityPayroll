using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class SalarySlip
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string StructureRefId { get; set; }

        public decimal Basic { get; set; }
        public Earnings Earnings { get; set; }
        public decimal GrossEarnings { get; set; }
        public Deductions Deductions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedOn { get; set; }
        public string PdfUrl { get; set; }
    }

    public class Earnings
    {
        public decimal Da { get; set; }
        public decimal Hra { get; set; }
        public List<OtherEarning> Others { get; set; }
    }

    public class OtherEarning
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }

    public class Deductions
    {
        public decimal Lwp { get; set; }
        public decimal PfEmployee { get; set; }
        public decimal PfEmployer { get; set; }
        public decimal Edli { get; set; }
        public decimal Tds { get; set; }
        public decimal SalaryAdvance { get; set; }
    }
}
