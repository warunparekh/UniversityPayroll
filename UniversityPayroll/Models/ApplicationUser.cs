using AspNetCore.Identity.Mongo.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UniversityPayroll.Models
{
    public class ApplicationUser : MongoUser<ObjectId>
    {
        
    }
}
