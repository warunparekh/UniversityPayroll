using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class EmployeeRepository
    {
        private readonly IMongoCollection<Employee> _col;

        public EmployeeRepository(MongoDbContext context) => _col = context.Employees;

        public Task<List<Employee>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<Employee?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task<Employee?> GetByCodeAsync(string code) => _col.Find(x => x.EmployeeCode == code).FirstOrDefaultAsync()!;
        public Task<Employee?> GetByUserIdAsync(string userId) => _col.Find(x => x.IdentityUserId == userId).FirstOrDefaultAsync()!;
        public Task CreateAsync(Employee item) => _col.InsertOneAsync(item);
        public Task UpdateAsync(Employee item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);
    }
}
