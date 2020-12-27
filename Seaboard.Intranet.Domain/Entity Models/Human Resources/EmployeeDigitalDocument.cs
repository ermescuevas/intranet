using System;

namespace Seaboard.Intranet.Domain
{
    public class EmployeeDigitalDocument
    {
        public string DocumentId { get; set; }
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PersonalizedContent { get; set; }
        public DateTime SendDate { get; set; }
        public DateTime SignDate { get; set; }
        public bool Status { get; set; }
    }
}
