using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class TaxSlabRepository
    {
        private readonly IMongoCollection<TaxSlab> _col;

        public TaxSlabRepository(MongoDbContext context)
        {
            _col = context.TaxSlabs;
        }

        public async Task<List<TaxSlab>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<TaxSlab?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(TaxSlab item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(TaxSlab item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
    }
}
