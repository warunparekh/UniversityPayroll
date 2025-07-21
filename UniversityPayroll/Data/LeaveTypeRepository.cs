using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Data;

public class LeaveTypeRepository
{
    private readonly IMongoCollection<LeaveType> _col;
    public LeaveTypeRepository(MongoDbContext ctx)
    {
        _col = ctx.LeaveTypes;
    }

    public async Task<List<LeaveType>> GetAllAsync() => await _col.Find(_ => true).ToListAsync();
    public async Task<LeaveType> GetByIdAsync(string id) => await _col.Find(x => x.Id == id).FirstOrDefaultAsync();
    public async Task CreateAsync(LeaveType item) => await _col.InsertOneAsync(item);
    public async Task UpdateAsync(LeaveType item) => await _col.ReplaceOneAsync(x => x.Id == item.Id, item);
    public async Task DeleteAsync(string id) => await _col.DeleteOneAsync(x => x.Id == id);
}