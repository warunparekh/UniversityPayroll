using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class LeaveRepository
    {
        private readonly IMongoCollection<LeaveApplication> _col;

        public LeaveRepository(MongoDbContext context)
        {
            _col = context.LeaveApplications;
        }

        public Task<List<LeaveApplication>> GetAllAsync() =>
            _col.Find(_ => true)
                .SortByDescending(x => x.AppliedOn)
                .ToListAsync();

        public Task<LeaveApplication?> GetByIdAsync(string id) =>
            _col.Find(x => x.Id == id).FirstOrDefaultAsync();

        public Task<List<LeaveApplication>> GetByEmployeeAsync(string employeeId) =>
            _col.Find(x => x.EmployeeId == employeeId)
                .SortByDescending(x => x.AppliedOn)
                .ToListAsync();

        public Task CreateAsync(LeaveApplication item) =>
            _col.InsertOneAsync(item);

        public Task UpdateAsync(LeaveApplication item) =>
            _col.ReplaceOneAsync(x => x.Id == item.Id, item);

        public Task DeleteAsync(string id) =>
            _col.DeleteOneAsync(x => x.Id == id);
    }
}
