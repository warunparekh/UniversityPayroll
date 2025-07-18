using System.Collections.Generic;

namespace UniversityPayroll.Models
{
    public class DesignationIndexItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Entitlements { get; set; }
    }
}
