using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveEntitlementRepository
    {
        private readonly IMongoCollection<LeaveEntitlement> _col;
        public LeaveEntitlementRepository(MongoDbContext ctx)
            => _col = ctx.LeaveEntitlements;

        public Task<LeaveEntitlement?> GetByDesignationAsync(string designation) =>
            _col.Find(x => x.Designation == designation).FirstOrDefaultAsync();

        public Task CreateAsync(LeaveEntitlement item) =>
            _col.InsertOneAsync(item);
        public async Task<List<LeaveEntitlement>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<LeaveEntitlement?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task UpdateAsync(LeaveEntitlement item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
    }
}
