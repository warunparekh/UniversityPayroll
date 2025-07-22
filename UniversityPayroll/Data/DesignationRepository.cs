using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class DesignationRepository
    {
        private readonly IMongoCollection<Designation> _col;

        public DesignationRepository(MongoDbContext context)
        {
            _col = context.Designations;
        }

        public Task<List<Designation>> GetAllAsync() =>
            _col.Find(_ => true).SortBy(x => x.Name).ToListAsync();

        public Task<List<Designation>> GetActiveAsync() =>
            _col.Find(x => x.IsActive).SortBy(x => x.Name).ToListAsync();

        public Task<Designation?> GetByIdAsync(string id) =>
            _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task<Designation?> GetByNameAsync(string name) =>
            _col.Find(x => x.Name == name).FirstOrDefaultAsync();

        public Task CreateAsync(Designation item) =>
            _col.InsertOneAsync(item);

        public Task UpdateAsync(Designation item) =>
            _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public Task DeleteAsync(string id) =>
            _col.DeleteOneAsync(x => x.Id == id);
    }
}