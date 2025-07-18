using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class TaxSlabService
    {
        private readonly IMongoCollection<TaxSlab> _col;
        public TaxSlabService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<TaxSlab>("TaxSlabs");
        }
        public Task<List<TaxSlab>> GetAll() =>
            _col.Find(_ => true).ToListAsync();
        public Task<TaxSlab> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        public Task Create(TaxSlab m) =>
            _col.InsertOneAsync(m);
        public Task Update(string id, TaxSlab m) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), m);
        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
