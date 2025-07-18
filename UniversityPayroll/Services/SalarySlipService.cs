using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class SalarySlipService
    {
        private readonly IMongoCollection<SalarySlip> _col;
        public SalarySlipService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<SalarySlip>("SalarySlips");
        }
        public Task<List<SalarySlip>> GetAll() =>
            _col.Find(_ => true).ToListAsync();
        public Task<List<SalarySlip>> GetByEmployee(string empId) =>
            _col.Find(x => x.EmployeeId == ObjectId.Parse(empId)).ToListAsync();
        public Task<SalarySlip> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        public Task Create(SalarySlip s) =>
            _col.InsertOneAsync(s);
        public Task Update(string id, SalarySlip s) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), s);
        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
