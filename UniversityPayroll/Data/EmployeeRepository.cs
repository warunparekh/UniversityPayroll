using MongoDB.Bson;
using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class EmployeeRepository
    {
        private readonly IMongoCollection<Employee> _employees;

        public EmployeeRepository(MongoDbContext context)
        {
            _employees = context.Employees;
        }

        public List<Employee> GetAll() => _employees.Find(emp => true).ToList();

        public Employee GetById(ObjectId id) =>
            _employees.Find(emp => emp.Id == id).FirstOrDefault();
        public Employee FindByIdentityUserId(ObjectId identityUserId) =>
            _employees.Find(e => e.IdentityUserId == identityUserId).FirstOrDefault();


        public void Create(Employee employee) =>
            _employees.InsertOne(employee);

        public void Update(ObjectId id, Employee employeeIn) =>
            _employees.ReplaceOne(emp => emp.Id == id, employeeIn);

        public void Remove(ObjectId id) =>
            _employees.DeleteOne(emp => emp.Id == id);
    }
}
