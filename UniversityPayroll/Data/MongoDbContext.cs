using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _db;
        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _db = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Employee> Employees =>
            _db.GetCollection<Employee>("Employees");
        public IMongoCollection<LeaveApplication> LeaveApplications =>
            _db.GetCollection<LeaveApplication>("LeaveApplications");
        public IMongoCollection<SalaryStructure> SalaryStructures =>
            _db.GetCollection<SalaryStructure>("SalaryStructures");
        public IMongoCollection<TaxSlab> TaxSlabs =>
            _db.GetCollection<TaxSlab>("TaxSlabs");
        public IMongoCollection<LeaveBalance> LeaveBalances =>
            _db.GetCollection<LeaveBalance>("LeaveBalances");
        public IMongoCollection<SalarySlip> SalarySlips =>
            _db.GetCollection<SalarySlip>("SalarySlips");
        public IMongoCollection<PayRun> PayRuns =>
            _db.GetCollection<PayRun>("PayRuns");
        // new collection for entitlements
        public IMongoCollection<LeaveEntitlement> LeaveEntitlements =>
            _db.GetCollection<LeaveEntitlement>("LeaveEntitlements");
    }
}
