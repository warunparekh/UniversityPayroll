using AspNetCore.Identity.Mongo.Model;

namespace UniversityPayroll.Models
{
    public class ApplicationUser : MongoUser
    {
        public string FullName { get; set; }
    }
}
