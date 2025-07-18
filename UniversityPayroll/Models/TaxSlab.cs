using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class TaxSlab
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public decimal MinAnnualIncome { get; set; }
        public decimal MaxAnnualIncome { get; set; }
        public float Rate { get; set; }
    }
}
