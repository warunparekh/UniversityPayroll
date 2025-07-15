using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public ObjectId IdentityUserId { get; set; } 


        public string EmployeeCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public BankAccount BankAccount { get; set; } = new BankAccount();
        public DateTime DateOfJoining { get; set; } = DateTime.Today;
        public decimal BasicPay { get; set; }
        public double HraPercent { get; set; }
        public double DaPercent { get; set; }
        public string Status { get; set; }
        public string Designation { get; set; }


    }

    public class BankAccount
    {
        public string IFSC { get; set; }
        public string AccountNumber { get; set; }
    }
}
