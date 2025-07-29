using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class SalaryStructure
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Designation { get; set; }
        public Allowances Allowances { get; set; }
        public double AnnualIncrementPercent { get; set; }
        public PfRules Pf { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class Allowances
    {
        public double DaPercent { get; set; }
        public double HraPercent { get; set; }
        public List<OtherAllowance> OtherAllowances { get; set; }
    }

    public class OtherAllowance
    {
        public string Name { get; set; }
        public double Percent { get; set; }
    }

    public class PfRules
    {
        public double EmployeePercent { get; set; }
        public double EmployerPercent { get; set; }
        public double EdliPercent { get; set; }
        public decimal PfWageCeiling { get; set; }
    }
}
