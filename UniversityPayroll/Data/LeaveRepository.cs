using MongoDB.Bson;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveRepository
    {
        private readonly IMongoCollection<LeaveApplication> _leaves;

        public LeaveRepository(MongoDbContext context)
        {
            _leaves = context.LeaveApplications;
        }

        public List<LeaveApplication> GetAll() => _leaves.Find(l => true).ToList();

        public List<LeaveApplication> GetByEmployeeId(string employeeId) =>
            _leaves.Find(l => l.EmployeeId == employeeId).ToList();

        public LeaveApplication GetById( ObjectId id) =>
            _leaves.Find(l => l.Id == id).FirstOrDefault();

        public void Create(LeaveApplication leave) =>
            _leaves.InsertOne(leave);

        public void Update(ObjectId id, LeaveApplication leaveIn) =>
            _leaves.ReplaceOne(l => l.Id == id, leaveIn);

        public void Remove(ObjectId id) =>
            _leaves.DeleteOne(l => l.Id == id);
        
    }
}
