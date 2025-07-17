using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class SalaryStructureRepository
    {
        private readonly IMongoCollection<SalaryStructure> _col;

        public SalaryStructureRepository(MongoDbContext context)
        {
            _col = context.SalaryStructures;
        }

        public async Task<List<SalaryStructure>> GetAllAsync() =>
            await _col.Find(_ => true).ToListAsync();

        public async Task<SalaryStructure?> GetByIdAsync(string id) =>
            await _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(SalaryStructure item) =>
            await _col.InsertOneAsync(item);

        public async Task UpdateAsync(SalaryStructure item) =>
            await _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _col.DeleteOneAsync(x => x.Id == id);
        public async Task<SalaryStructure?> GetByDesignationAsync(string designation) =>
            await _col.Find(x => x.Designation == designation)
                      .FirstOrDefaultAsync();
    }
}
