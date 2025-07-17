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

        public Task<List<LeaveEntitlement>> GetAllAsync() =>
            _col.Find(_ => true).ToListAsync();

        public Task<LeaveEntitlement?> GetByDesignationAsync(string designation) =>
            _col.Find(x => x.Designation == designation).FirstOrDefaultAsync();

        public Task CreateAsync(LeaveEntitlement item) =>
            _col.InsertOneAsync(item);

        public Task UpdateAsync(LeaveEntitlement item) =>
            _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public Task DeleteAsync(string id) =>
            _col.DeleteOneAsync(x => x.Id == id);
    }
}
