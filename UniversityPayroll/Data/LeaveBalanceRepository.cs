using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveBalanceRepository
    {
        private readonly IMongoCollection<LeaveBalance> _col;

        public LeaveBalanceRepository(MongoDbContext context) => _col = context.LeaveBalances;

        public Task<List<LeaveBalance>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<LeaveBalance?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task<LeaveBalance?> GetByEmployeeYearAsync(string empId, int year) =>
            _col.Find(x => x.EmployeeId == empId && x.Year == year).FirstOrDefaultAsync()!;
        public Task CreateAsync(LeaveBalance item) => _col.InsertOneAsync(item);
        public Task UpdateAsync(LeaveBalance item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);

        public Task RemoveLeaveTypeFromAll(string leaveType)
        {
            var filter = Builders<LeaveBalance>.Filter.Exists($"Entitlements.{leaveType}");
            var update = Builders<LeaveBalance>.Update
                .Unset($"Entitlements.{leaveType}")
                .Unset($"Used.{leaveType}")
                .Unset($"Balance.{leaveType}");
            return _col.UpdateManyAsync(filter, update);
        }
    }
}