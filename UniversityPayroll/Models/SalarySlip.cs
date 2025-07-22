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
        public decimal Da { get; set; }
        public decimal Hra { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal GrossEarnings { get; set; }
        public decimal PfEmployee { get; set; }
        public decimal PfEmployer { get; set; }
        public decimal Edli { get; set; }
        public decimal Tax { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal UnpaidLeaveDeduction { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedOn { get; set; }
        public string PdfUrl { get; set; }
    }
}