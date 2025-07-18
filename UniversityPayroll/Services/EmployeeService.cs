using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class EmployeeService
    {
        private readonly IMongoCollection<Employee> _col;
        public EmployeeService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<Employee>("Employees");
        }
        public Task<List<Employee>> GetAll() =>
            _col.Find(_ => true).ToListAsync();
        public Task<Employee> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        public Task<Employee> GetByUserId(string userId) =>
            _col.Find(x => x.UserId == userId).FirstOrDefaultAsync();
        public Task Create(Employee e) =>
            _col.InsertOneAsync(e);
        public Task Update(string id, Employee e) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), e);
        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
