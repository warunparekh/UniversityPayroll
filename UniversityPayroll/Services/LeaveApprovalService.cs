using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class LeaveApprovalService
    {
        private readonly IMongoCollection<LeaveRequest> _col;
        public LeaveApprovalService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<LeaveRequest>("LeaveRequests");
        }
        public Task<List<LeaveRequest>> GetPending() =>
            _col.Find(x => x.Status == LeaveStatus.Pending).ToListAsync();
        public Task Approve(string id, string by)
        {
            var update = Builders<LeaveRequest>.Update
              .Set(x => x.Status, LeaveStatus.Approved)
              .Set(x => x.ActionedOn, DateTime.UtcNow)
              .Set(x => x.ActionedBy, by);
            return _col.UpdateOneAsync(x => x.Id == ObjectId.Parse(id), update);
        }
        public Task Reject(string id, string by)
        {
            var update = Builders<LeaveRequest>.Update
              .Set(x => x.Status, LeaveStatus.Rejected)
              .Set(x => x.ActionedOn, DateTime.UtcNow)
              .Set(x => x.ActionedBy, by);
            return _col.UpdateOneAsync(x => x.Id == ObjectId.Parse(id), update);
        }
    }
}
