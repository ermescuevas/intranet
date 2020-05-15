using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class UserRequest
    {
        public string RequestId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string DaysWork { get; set; }
        public string Resources { get; set; }
        public string EmailAccount { get; set; }
        public string InternetAccess { get; set; }
        public string Department { get; set; }
        public string Comments { get; set; }
        public bool IsPolicy { get; set; }
        public int Status { get; set; }
        public int RowId { get; set; }
    }
}
