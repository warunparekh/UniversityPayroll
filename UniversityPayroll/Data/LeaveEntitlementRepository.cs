using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveEntitlementRepository
    {
        private readonly IMongoCollection<LeaveEntitlement> _col;
        
        public LeaveEntitlementRepository(MongoDbContext ctx) => _col = ctx.LeaveEntitlements;

        public Task<LeaveEntitlement?> GetByDesignationAsync(string designation) =>
            _col.Find(x => x.Designation == designation).FirstOrDefaultAsync()!;
        public Task CreateAsync(LeaveEntitlement item) => _col.InsertOneAsync(item);
        public Task<List<LeaveEntitlement>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<LeaveEntitlement?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task UpdateAsync(LeaveEntitlement item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);

        public Task RemoveLeaveTypeFromAll(string leaveType)
        {
            var filter = Builders<LeaveEntitlement>.Filter.Exists($"Entitlements.{leaveType}");
            var update = Builders<LeaveEntitlement>.Update.Unset($"Entitlements.{leaveType}");
            return _col.UpdateManyAsync(filter, update);
        }
    }
}