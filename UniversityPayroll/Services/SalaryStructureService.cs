using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class SalaryStructureService
    {
        private readonly IMongoCollection<SalaryStructure> _col;
        public SalaryStructureService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<SalaryStructure>("SalaryStructures");
        }

        public Task<List<SalaryStructure>> GetAll() =>
            _col.Find(_ => true).ToListAsync();

        public Task<SalaryStructure> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();

        public Task Create(SalaryStructure m) =>
            _col.InsertOneAsync(m);

        public Task Update(string id, SalaryStructure m) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), m);

        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
