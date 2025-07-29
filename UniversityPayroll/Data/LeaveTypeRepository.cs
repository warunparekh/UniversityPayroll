using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveTypeRepository
    {
        private readonly IMongoCollection<LeaveType> _col;
        
        public LeaveTypeRepository(MongoDbContext ctx) => _col = ctx.LeaveTypes;

        public Task<List<LeaveType>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<LeaveType?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task CreateAsync(LeaveType item) => _col.InsertOneAsync(item);
        public Task UpdateAsync(LeaveType item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);
    }
}