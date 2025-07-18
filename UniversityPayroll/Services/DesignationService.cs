using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class DesignationService
    {
        private readonly IMongoCollection<Designation> _col;
        public DesignationService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<Designation>("Designations");
        }
        public Task<List<Designation>> GetAll() =>
            _col.Find(_ => true).ToListAsync();
        public Task<Designation> Get(string id) =>
            _col.Find(d => d.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        public Task Create(Designation d) =>
            _col.InsertOneAsync(d);
        public Task Update(string id, Designation d) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), d);
        public Task Delete(string id) =>
            _col.DeleteOneAsync(d => d.Id == ObjectId.Parse(id));
    }
}
