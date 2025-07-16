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

        public IMongoCollection<Employee> Employees =>
            _database.GetCollection<Employee>("Employees");

        public IMongoCollection<LeaveApplication> LeaveApplications =>
            _database.GetCollection<LeaveApplication>("LeaveApplications");

        public IMongoCollection<SalaryStructure> SalaryStructures =>
            _database.GetCollection<SalaryStructure>("SalaryStructures");

        public IMongoCollection<TaxSlab> TaxSlabs =>
            _database.GetCollection<TaxSlab>("TaxSlabs");

        public IMongoCollection<LeaveBalance> LeaveBalances =>
            _database.GetCollection<LeaveBalance>("LeaveBalances");

        public IMongoCollection<SalarySlip> SalarySlips =>
            _database.GetCollection<SalarySlip>("SalarySlips");

        public IMongoCollection<PayRun> PayRuns =>
            _database.GetCollection<PayRun>("PayRuns");
    }
}
