using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveRepository
    {
        private readonly IMongoCollection<LeaveApplication> _col;

        public LeaveRepository(MongoDbContext context) => _col = context.LeaveApplications;

        public Task<List<LeaveApplication>> GetAllAsync() =>
            _col.Find(_ => true).SortByDescending(x => x.AppliedOn).ToListAsync();

        public Task<LeaveApplication?> GetByIdAsync(string id) =>
            _col.Find(x => x.Id == id).FirstOrDefaultAsync()!;

        public Task<List<LeaveApplication>> GetByEmployeeAsync(string employeeId) =>
            _col.Find(x => x.EmployeeId == employeeId).SortByDescending(x => x.AppliedOn).ToListAsync();

        public async Task<bool> HasOverlappingLeaveAsync(string employeeId, DateTime startDate, DateTime endDate, string? excludeId = null)
        {
            var filter = Builders<LeaveApplication>.Filter.And(
                Builders<LeaveApplication>.Filter.Eq(x => x.EmployeeId, employeeId),
                Builders<LeaveApplication>.Filter.Ne(x => x.Status, "Rejected"),
                Builders<LeaveApplication>.Filter.Or(
                    Builders<LeaveApplication>.Filter.And(
                        Builders<LeaveApplication>.Filter.Lte(x => x.StartDate, endDate),
                        Builders<LeaveApplication>.Filter.Gte(x => x.EndDate, startDate)
                    )
                )
            );

            if (!string.IsNullOrEmpty(excludeId))
            {
                filter = Builders<LeaveApplication>.Filter.And(filter,
                    Builders<LeaveApplication>.Filter.Ne(x => x.Id, excludeId));
            }

            var overlapping = await _col.Find(filter).FirstOrDefaultAsync();
            return overlapping != null;
        }

        public Task CreateAsync(LeaveApplication item) =>
            _col.InsertOneAsync(item);

        public Task UpdateAsync(LeaveApplication item) =>
            _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public Task DeleteAsync(string id) =>
            _col.DeleteOneAsync(x => x.Id == id);
    }
}
