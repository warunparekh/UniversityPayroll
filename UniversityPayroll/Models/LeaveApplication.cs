using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System;

namespace UniversityPayroll.Models
{
    [BsonIgnoreExtraElements]
    public class LeaveApplication
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; }

        [Required]
        public string LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required]
        [DataType(DataType.Date)]
        [EndDateAfterStartDate(ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;

        public int TotalDays { get; set; }

        [Required]
        public string Reason { get; set; }

        public string Status { get; set; }

        public DateTime AppliedOn { get; set; }

        public string? Comment { get; set; }

        public string? DecidedBy { get; set; }

        public DateTime? DecidedOn { get; set; }
    }

    public class EndDateAfterStartDateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = (LeaveApplication)validationContext.ObjectInstance;
            if (model.EndDate < model.StartDate)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}