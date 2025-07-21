using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class SalarySlipRepository
    {
        private readonly IMongoCollection<SalarySlip> _col;

        public SalarySlipRepository(MongoDbContext context)
        {
            _col = context.SalarySlips;
        }

        public async Task<List<SalarySlip>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<SalarySlip?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<List<SalarySlip>> GetByEmployeeAsync(string empId) =>
            await _col.Find(x => x.EmployeeId == empId)
                      .SortByDescending(x => x.Year).ThenByDescending(x => x.Month)
                      .ToListAsync();

        public async Task CreateAsync(SalarySlip item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(SalarySlip item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
    }
}