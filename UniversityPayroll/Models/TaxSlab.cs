using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class TaxSlab
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FinancialYear { get; set; }
        public List<Slab> Slabs { get; set; }
        public double CessPercent { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class Slab
    {
        public decimal From { get; set; }
        public decimal? To { get; set; }
        public double Rate { get; set; }
    }
}
