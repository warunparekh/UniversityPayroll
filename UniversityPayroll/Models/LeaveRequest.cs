using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace UniversityPayroll.Models
{
    public enum LeaveStatus { Pending, Approved, Rejected }

    public class LeaveRequest
    {
        [BsonId] public ObjectId Id { get; set; }
        public ObjectId EmployeeId { get; set; }
        public ObjectId LeaveTypeId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Days { get; set; }
        public bool IsHalfDay { get; set; }
        public string Reason { get; set; }
        public LeaveStatus Status { get; set; }
        public DateTime AppliedOn { get; set; }
        public DateTime? ActionedOn { get; set; }
        public string ActionedBy { get; set; }
    }
}
