using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class SalaryStructureRepository
    {
        private readonly IMongoCollection<SalaryStructure> _col;

        public SalaryStructureRepository(MongoDbContext context) => _col = context.SalaryStructures;

        public Task<List<SalaryStructure>> GetAllAsync() => _col.Find(_ => true).ToListAsync();
        public Task<SalaryStructure?> GetByIdAsync(string id) => _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;
        public Task CreateAsync(SalaryStructure item) => _col.InsertOneAsync(item);
        public Task UpdateAsync(SalaryStructure item) => _col.ReplaceOneAsync(x => x.Id == item.Id, item);
        public Task DeleteAsync(string id) => _col.DeleteOneAsync(x => x.Id == id);
        public Task<SalaryStructure?> GetByDesignationAsync(string designation) => 
            _col.Find(x => x.Designation == designation).FirstOrDefaultAsync()!;
    }
}
