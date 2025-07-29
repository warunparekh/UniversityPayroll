using System.Collections.Generic;
using UniversityPayroll.Models;

namespace UniversityPayroll.ViewModels
{
    public class EmployeeProfileViewModel
    {
        public Employee? Employee { get; set; }
        public SalaryStructure? SalaryStructure { get; set; }
        public TaxSlab? TaxSlab { get; set; }
        public LeaveBalance? LeaveBalance { get; set; }
        public List<SalarySlip>? SalarySlips { get; set; }
        public List<LeaveApplicationViewModel>? LeaveApplications { get; set; }
        public List<LeaveType>? LeaveTypes { get; set; }
    }
}