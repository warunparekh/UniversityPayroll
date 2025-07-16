using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveBalanceRepository
    {
        private readonly IMongoCollection<LeaveBalance> _col;

        public LeaveBalanceRepository(MongoDbContext context)
        {
            _col = context.LeaveBalances;
        }

        public async Task<List<LeaveBalance>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<LeaveBalance?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<LeaveBalance?> GetByEmployeeYearAsync(string empId, int year) =>
            await _col.Find(x => x.EmployeeId == empId && x.Year == year)
                      .FirstOrDefaultAsync();

        public async Task CreateAsync(LeaveBalance item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(LeaveBalance item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);

        public async Task IncrementUsedAsync(string empId, string leaveType, int days)
        {
            var filter = Builders<LeaveBalance>.Filter.Where(x =>
                x.EmployeeId == empId && x.Year == x.Year);
            var update = Builders<LeaveBalance>.Update.Inc($"Used.{leaveType}", days)
                                                    .Inc($"Balance.{leaveType}", -days)
                                                    .Set("UpdatedOn", System.DateTime.UtcNow);
            await _col.UpdateOneAsync(filter, update);
        }
    }
}
