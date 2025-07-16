using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class EmployeeRepository
    {
        private readonly IMongoCollection<Employee> _col;

        public EmployeeRepository(MongoDbContext context)
        {
            _col = context.Employees;
        }

        public async Task<List<Employee>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<Employee?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Employee?> GetByCodeAsync(string code) =>
            await _col.Find(x => x.EmployeeCode == code).FirstOrDefaultAsync();

        

        public async Task CreateAsync(Employee item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(Employee item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
    }
}
