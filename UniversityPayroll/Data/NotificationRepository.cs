using MongoDB.Driver;
using UniversityPayroll.Models;

namespace UniversityPayroll.Data
{
    public class NotificationRepository
    {
        private readonly IMongoCollection<Notification> _notifications;

        public NotificationRepository(MongoDbContext context) => _notifications = context.Notifications;

        public Task CreateAsync(Notification notification) => _notifications.InsertOneAsync(notification);

        public Task<List<Notification>> GetByUserIdAsync(string userId) =>
            _notifications.Find(n => n.UserId == userId).SortByDescending(n => n.CreatedAt).ToListAsync();
    }
}