using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class PayRunRepository
    {
        private readonly IMongoCollection<PayRun> _col;

        public PayRunRepository(MongoDbContext context)
        {
            _col = context.PayRuns;
        }

        public async Task<List<PayRun>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<PayRun?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(PayRun item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(PayRun item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
    }
}
