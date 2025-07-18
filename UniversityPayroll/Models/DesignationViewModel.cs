using System.Collections.Generic;

namespace UniversityPayroll.Models
{
    public class EntitlementInput
    {
        public string LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public int Quota { get; set; }
    }

    public class DesignationViewModel
    {
        public string Name { get; set; }
        public List<EntitlementInput> Entitlements { get; set; }
    }
}
