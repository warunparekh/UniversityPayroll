using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Employee> Employees => _database.GetCollection<Employee>("employees");
        public IMongoCollection<SalaryStructure> SalaryStructures => _database.GetCollection<SalaryStructure>("salaryStructures");
        public IMongoCollection<LeaveType> LeaveTypes => _database.GetCollection<LeaveType>("leaveTypes");
        public IMongoCollection<LeaveApplication> LeaveApplications => _database.GetCollection<LeaveApplication>("leaveApplications");
        public IMongoCollection<LeaveBalance> LeaveBalances => _database.GetCollection<LeaveBalance>("leaveBalances");
        public IMongoCollection<LeaveEntitlement> LeaveEntitlements => _database.GetCollection<LeaveEntitlement>("leaveEntitlements");
        public IMongoCollection<TaxSlab> TaxSlabs => _database.GetCollection<TaxSlab>("taxSlabs");
        public IMongoCollection<SalarySlip> SalarySlips => _database.GetCollection<SalarySlip>("salarySlips");
        public IMongoCollection<Designation> Designations => _database.GetCollection<Designation>("designations");
        public IMongoCollection<Notification> Notifications => _database.GetCollection<Notification>("Notifications");
    }
}