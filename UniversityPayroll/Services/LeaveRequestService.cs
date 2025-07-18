using MongoDB.Driver;
using MongoDB.Bson;
using UniversityPayroll.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversityPayroll.Services
{
    public class LeaveRequestService
    {
        private readonly IMongoCollection<LeaveRequest> _col;
        public LeaveRequestService(DatabaseSettings s)
        {
            var client = new MongoClient(s.ConnectionString);
            var db = client.GetDatabase(s.DatabaseName);
            _col = db.GetCollection<LeaveRequest>("LeaveRequests");
        }
        public Task<List<LeaveRequest>> GetAll() =>
            _col.Find(_ => true).ToListAsync();
        public Task<List<LeaveRequest>> GetByEmployee(string empId) =>
            _col.Find(x => x.EmployeeId == ObjectId.Parse(empId)).ToListAsync();
        public Task<LeaveRequest> Get(string id) =>
            _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        public Task Create(LeaveRequest r)
        {
            r.AppliedOn = DateTime.UtcNow;
            r.Status = LeaveStatus.Pending;
            return _col.InsertOneAsync(r);
        }
        public Task Update(string id, LeaveRequest r) =>
            _col.ReplaceOneAsync(x => x.Id == ObjectId.Parse(id), r);
        public Task Delete(string id) =>
            _col.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));
    }
}
