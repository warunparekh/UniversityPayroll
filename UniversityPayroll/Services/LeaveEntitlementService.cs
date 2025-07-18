using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversityPayroll.Models;

namespace UniversityPayroll.Services
{
    public class LeaveEntitlementService
    {
        private readonly IMongoCollection<LeaveEntitlement> _col;
        public LeaveEntitlementService(DatabaseSettings s)
        {
            var db = new MongoClient(s.ConnectionString).GetDatabase(s.DatabaseName);
            _col = db.GetCollection<LeaveEntitlement>("LeaveEntitlements");
        }

        public Task<List<LeaveEntitlement>> GetAll() =>
            _col.Find(_ => true).ToListAsync();

        public Task<LeaveEntitlement> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();

        public Task<List<LeaveEntitlement>> GetByDesignation(string did) =>
            _col.Find(x => x.DesignationId == ObjectId.Parse(did)).ToListAsync();

        public Task Create(LeaveEntitlement m) =>
            _col.InsertOneAsync(m);

        public Task Update(string id, LeaveEntitlement m) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), m);

        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));

        public Task AdjustEntitlement(string designationId, string leaveTypeId, int delta) =>
            _col.UpdateOneAsync(
              Builders<LeaveEntitlement>.Filter.And(
                Builders<LeaveEntitlement>.Filter.Eq(x => x.DesignationId, ObjectId.Parse(designationId)),
                Builders<LeaveEntitlement>.Filter.Eq(x => x.LeaveTypeId, ObjectId.Parse(leaveTypeId))
              ),
              Builders<LeaveEntitlement>.Update.Inc(x => x.AnnualQuota, delta)
            );
    }
}
