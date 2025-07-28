using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class SalarySlipRepository
    {
        private readonly IMongoCollection<SalarySlip> _col;

        public SalarySlipRepository(MongoDbContext context) => _col = context.SalarySlips;

        public Task<List<SalarySlip>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<SalarySlip?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task<List<SalarySlip>> GetByEmployeeAsync(string empId) =>
            _col.Find(x => x.EmployeeId == empId).SortByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync();
        public Task CreateAsync(SalarySlip item) => _col.InsertOneAsync(item);
        public Task UpdateAsync(SalarySlip item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);
    }
}