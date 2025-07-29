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

        public Task<List<TaxSlab>> GetAllAsync() =>
            _col.Find(_ => true).ToListAsync();

        public Task<TaxSlab?> GetByIdAsync(string id) =>
            _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;

        public Task CreateAsync(TaxSlab item) =>
            _col.InsertOneAsync(item);

        public Task UpdateAsync(TaxSlab item) =>
            _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public Task DeleteAsync(string id) =>
            _col.DeleteOneAsync(x => x.Id == id);
    }
}
