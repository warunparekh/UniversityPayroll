using System;

namespace UniversityPayroll.ViewModels
{
    public class LeaveApplicationViewModel
    {
        public string LeaveId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string AdminComments { get; set; }
    }
}