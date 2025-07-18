using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversityPayroll.Models;

namespace UniversityPayroll.Services
{
    public class LeaveTypeService
    {
        private readonly IMongoCollection<LeaveType> _col;
        public LeaveTypeService(DatabaseSettings s)
        {
            var db = new MongoClient(s.ConnectionString).GetDatabase(s.DatabaseName);
            _col = db.GetCollection<LeaveType>("LeaveTypes");
        }

        public Task<List<LeaveType>> GetAll() =>
            _col.Find(_ => true).ToListAsync();

        public Task<LeaveType> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();

        public Task Create(LeaveType m) =>
            _col.InsertOneAsync(m);

        public Task Update(string id, LeaveType m) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), m);

        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
