using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace UniversityPayroll.Models
{
    public class EndDateAfterStartDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var model = value as LeaveApplication;
            if (model == null) return true;
            return model.EndDate >= model.StartDate;
        }
    }

    [BsonIgnoreExtraElements]
    public class LeaveApplication
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? EmployeeId { get; set; }

        [Required]
        public string LeaveType { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required]
        [DataType(DataType.Date)]
        [EndDateAfterStartDate(ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;

        public decimal TotalDays { get; set; }

        public bool IsHalfDay { get; set; } = false;

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime AppliedOn { get; set; } = DateTime.UtcNow;

        public string? Comment { get; set; }

        public string? DecidedBy { get; set; }

        public DateTime? DecidedOn { get; set; }
    }
}